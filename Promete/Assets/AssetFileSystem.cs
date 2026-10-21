using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promete.Assets;

/// <summary>
/// 複数の <see cref="IAssetSource"/> を優先度つきでマウントし、論理パスでアセットを読み出す仮想ファイルシステムです。
/// </summary>
/// <remarks>
/// <para>
/// 読むときは、優先度の高いソースから順に探します。優先度が同じ場合は、後からマウントしたソースが優先されます。
/// 後からマウントしたソースで既存のアセットを上書きできるので、DLC やパッチも同じ仕組みで扱えます。
/// </para>
/// <para>
/// すべてのメンバーは、どのスレッドから呼んでも安全です。読み込みの途中で <see cref="Mount"/> や <see cref="Unmount"/> を呼んでも構いません。
/// </para>
/// </remarks>
public sealed class AssetFileSystem : IDisposable
{
    private readonly object _gate = new();
    private MountEntry[] _mounts = [];
    private long _nextSequence;
    private bool _disposed;

    /// <summary>
    /// マウントしているソースを、優先される順 (優先度の高い順。同じ優先度では後からマウントした順) に取得します。
    /// </summary>
    /// <remarks>呼び出した時点のスナップショットを返します。</remarks>
    public IReadOnlyList<IAssetSource> Sources
    {
        get
        {
            var mounts = Snapshot();
            return Array.ConvertAll(mounts, entry => entry.Source);
        }
    }

    /// <summary>
    /// ソースが 1 つもマウントされていないときに、<see cref="OpenRead"/> の例外のメッセージに添える説明を取得または設定します。
    /// </summary>
    /// <remarks>
    /// <see cref="AssetManager"/> が、属性も明示的なマウントも無かったときに、原因 (属性が見つからないこと) と対処を伝えるために設定します。
    /// </remarks>
    internal string? EmptyHint { get; set; }

    /// <summary>
    /// ソースをマウントします。
    /// </summary>
    /// <param name="source">マウントするソース。<see cref="IDisposable"/> を実装している場合、所有権はこのファイルシステムに移ります。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="InvalidOperationException">同じソースが既にマウントされている場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public void Mount(IAssetSource source, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (Array.Exists(_mounts, entry => ReferenceEquals(entry.Source, source)))
            {
                throw new InvalidOperationException("同じソースは既にマウントされています。");
            }

            var next = new MountEntry[_mounts.Length + 1];
            _mounts.CopyTo(next, 0);
            next[^1] = new MountEntry(source, priority, ++_nextSequence);
            Publish(next);
        }
    }

    /// <summary>
    /// ソースを非同期に作成し、作成できたらマウントします。
    /// </summary>
    /// <param name="sourceFactory">ソースを作成する処理。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <param name="cancellationToken"><paramref name="sourceFactory"/> に渡すキャンセルトークン。</param>
    /// <returns>マウントしたソース。<see cref="Unmount"/> に渡せます。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceFactory"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。作成中に破棄された場合は、作成したソースを破棄してからスローします。</exception>
    public async Task<IAssetSource> MountAsync(
        Func<CancellationToken, Task<IAssetSource>> sourceFactory,
        int priority = 0,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(sourceFactory);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);

        var source = await sourceFactory(cancellationToken).ConfigureAwait(false);
        try
        {
            Mount(source, priority);
        }
        catch (ObjectDisposedException)
        {
            // 作成中に破棄された場合、マウントされなかったソースはここで後始末する
            if (source is IDisposable disposable)
            {
                disposable.Dispose();
            }

            throw;
        }

        return source;
    }

    /// <summary>
    /// ソースをアンマウントします。ソースが <see cref="IDisposable"/> を実装していれば破棄します。
    /// </summary>
    /// <remarks>既に開いたストリームは、閉じられるまで有効のまま残ります。</remarks>
    /// <param name="source">アンマウントするソース。</param>
    /// <returns>アンマウントした場合は <see langword="true"/>。マウントされていなかった場合は <see langword="false"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public bool Unmount(IAssetSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var index = Array.FindIndex(_mounts, entry => ReferenceEquals(entry.Source, source));
            if (index < 0)
            {
                return false;
            }

            var next = new MountEntry[_mounts.Length - 1];
            Array.Copy(_mounts, 0, next, 0, index);
            Array.Copy(_mounts, index + 1, next, index, _mounts.Length - index - 1);
            Publish(next);
        }

        // ロックの外で破棄する (破棄処理が他のメンバーを待たせないように)
        if (source is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return true;
    }

    /// <summary>
    /// 指定した論理パスのファイルが、いずれかのソースに存在するかどうかを取得します。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <returns>存在する場合は <see langword="true"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合 (<see cref="AssetPath.Normalize"/>)。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public bool Exists(string path)
    {
        var mounts = Snapshot();
        var normalized = AssetPath.Normalize(path);

        foreach (var entry in mounts)
        {
            if (TryExists(entry.Source, normalized))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 指定した論理パスのファイルを、優先度で勝ったソースから読み取り用に開きます。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <returns>読み取り用のストリーム。常に seek できます。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合 (<see cref="AssetPath.Normalize"/>)。</exception>
    /// <exception cref="AssetPathCaseMismatchException">どのソースにも無く、大文字・小文字だけが違うファイルがある場合。</exception>
    /// <exception cref="FileNotFoundException">どのソースにも無い場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Stream OpenRead(string path)
    {
        var mounts = Snapshot();
        var normalized = AssetPath.Normalize(path);

        if (mounts.Length == 0)
        {
            throw new FileNotFoundException(
                $"アセット '{normalized}' が見つかりません。ソースが 1 つもマウントされていません。",
                normalized
            );
        }

        // 1. 完全一致するソースのうち、優先度の最も高いものを読む
        foreach (var entry in mounts)
        {
            if (!TryExists(entry.Source, normalized))
            {
                continue;
            }

            Stream stream;
            try
            {
                stream = entry.Source.OpenRead(normalized);
            }
            catch (AssetPathCaseMismatchException)
            {
                throw;
            }
            catch (Exception e) when (IsMissingError(e))
            {
                // Exists と OpenRead の間に消えた、または破棄されたソースは無いものとして次へ進む
                continue;
            }

            return EnsureSeekable(stream);
        }

        // 2. 無ければ、大文字・小文字だけ違うヒントを伝えるため各ソースに開かせる
        foreach (var entry in mounts)
        {
            Stream stream;
            try
            {
                stream = entry.Source.OpenRead(normalized);
            }
            catch (AssetPathCaseMismatchException)
            {
                throw;
            }
            catch (Exception e) when (IsMissingError(e))
            {
                // 他のソースを試す
                continue;
            }

            return EnsureSeekable(stream);
        }

        throw new FileNotFoundException($"アセット '{normalized}' が見つかりません。", normalized);
    }

    /// <summary>
    /// 指定したディレクトリにあるファイルの論理パスを、すべてのソースから集めて列挙します。
    /// </summary>
    /// <param name="directory">ディレクトリの論理パス。空文字列または <c>/</c> はルートを表します。</param>
    /// <param name="pattern">ファイル名に対するパターン (<see cref="AssetPath.IsMatch"/>)。</param>
    /// <param name="recursive">サブディレクトリも含めて列挙する場合は <see langword="true"/>。</param>
    /// <returns>重複を除き、序数順に並べた論理パス。</returns>
    /// <exception cref="ArgumentException">引数が正しくない場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public IEnumerable<string> Enumerate(
        string directory,
        string pattern = "*",
        bool recursive = false
    )
    {
        var mounts = Snapshot();
        var normalizedDirectory = AssetPath.NormalizeDirectory(directory);

        // パターンの不正 (区切り文字を含むなど) をここで例外にする
        _ = AssetPath.IsMatch(string.Empty, pattern);

        var results = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in mounts)
        {
            // 途中で破棄された場合に備えて、いったん全部集めてから結果に加える
            List<string> paths;
            try
            {
                paths = new List<string>(
                    entry.Source.Enumerate(normalizedDirectory, pattern, recursive)
                );
            }
            catch (ObjectDisposedException)
            {
                // 破棄されたソースの結果は使わない
                continue;
            }

            results.UnionWith(paths);
        }

        var array = new string[results.Count];
        results.CopyTo(array);
        return array;
    }

    /// <summary>
    /// 優先度で勝ったソースが <see cref="IFileBackedAssetSource"/> であれば、そのファイルの実際のフルパスを取得します。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="fullPath">実ファイルのフルパス。</param>
    /// <returns>
    /// 取得できた場合は <see langword="true"/>。どのソースにも無い場合や、優先度で勝ったソースが <see cref="IFileBackedAssetSource"/> でない場合は <see langword="false"/>。
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が論理パスとして正しくない場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        var mounts = Snapshot();
        var normalized = AssetPath.Normalize(path);

        foreach (var entry in mounts)
        {
            if (!TryExists(entry.Source, normalized))
            {
                continue;
            }

            // 優先度で勝ったソースだけを見る。下のソースには落ちない
            if (entry.Source is IFileBackedAssetSource fileBacked)
            {
                try
                {
                    return fileBacked.TryGetFilePath(normalized, out fullPath);
                }
                catch (ObjectDisposedException)
                {
                    // Exists の後に破棄されたソースは無いものとして次へ進む
                    continue;
                }
            }

            fullPath = null;
            return false;
        }

        fullPath = null;
        return false;
    }

    /// <summary>
    /// すべてのソースをアンマウントし、<see cref="IDisposable"/> を実装したソースを破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    /// <remarks>既に開いたストリームは、閉じられるまで有効のまま残ります。</remarks>
    /// <exception cref="AggregateException">いずれかのソースの破棄が例外を投げた場合。残りのソースは全て破棄したうえで、集めた例外をまとめてスローします。</exception>
    public void Dispose()
    {
        MountEntry[] previous;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            Volatile.Write(ref _disposed, true);
            previous = _mounts;
            Publish([]);
        }

        // 1 つが失敗しても残りを破棄し、最後にまとめて投げる
        List<Exception>? errors = null;
        foreach (var entry in previous)
        {
            if (entry.Source is not IDisposable disposable)
            {
                continue;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception e)
            {
                errors ??= [];
                errors.Add(e);
            }
        }

        if (errors is not null)
        {
            throw new AggregateException(errors);
        }
    }

    /// <summary>
    /// 優先度の高い順 (同じ優先度では後からマウントした順) に並べる比較です。
    /// </summary>
    private static int CompareEntries(MountEntry a, MountEntry b)
    {
        var byPriority = b.Priority.CompareTo(a.Priority);
        return byPriority != 0 ? byPriority : b.Sequence.CompareTo(a.Sequence);
    }

    /// <summary>
    /// ソースに存在を尋ねます。ソースが破棄済みで <see cref="ObjectDisposedException"/> を投げた場合は、そのソースには無いものとして <see langword="false"/> を返します。
    /// </summary>
    private static bool TryExists(IAssetSource source, string path)
    {
        try
        {
            return source.Exists(path);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// <see cref="IAssetSource.OpenRead"/> が投げた例外が、そのソースには無いことを示すものかどうかを判定します。
    /// </summary>
    private static bool IsMissingError(Exception e)
    {
        return e is FileNotFoundException or DirectoryNotFoundException or ObjectDisposedException;
    }

    /// <summary>
    /// 返されたストリームが seek できない場合に、内容を全部メモリにコピーして seek できるストリームにします。
    /// </summary>
    private static Stream EnsureSeekable(Stream stream)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        using (stream)
        {
            var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            return buffer;
        }
    }

    /// <summary>
    /// 現在のマウント一覧のスナップショットを返します。破棄済みなら例外をスローします。
    /// </summary>
    private MountEntry[] Snapshot()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        return Volatile.Read(ref _mounts);
    }

    /// <summary>
    /// 新しいマウント一覧を並べ替えて公開します。呼び出し側はロックを保持している必要があります。
    /// </summary>
    /// <param name="entries">公開する一覧。公開後は変更しないこと。</param>
    private void Publish(MountEntry[] entries)
    {
        Array.Sort(entries, CompareEntries);
        Volatile.Write(ref _mounts, entries);
    }

    /// <summary>
    /// マウントの 1 件。優先度と、マウント順を表す連番を持ちます。
    /// </summary>
    private sealed record MountEntry(IAssetSource Source, int Priority, long Sequence);
}
