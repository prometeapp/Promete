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
    /// <summary>
    /// マウントしているソースを、優先される順 (優先度の高い順。同じ優先度では後からマウントした順) に取得します。
    /// </summary>
    /// <remarks>呼び出した時点のスナップショットを返します。</remarks>
    public IReadOnlyList<IAssetSource> Sources => throw new NotImplementedException();

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
        throw new NotImplementedException();
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
    public Task<IAssetSource> MountAsync(
        Func<CancellationToken, Task<IAssetSource>> sourceFactory,
        int priority = 0,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// すべてのソースをアンマウントし、<see cref="IDisposable"/> を実装したソースを破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    /// <remarks>既に開いたストリームは、閉じられるまで有効のまま残ります。</remarks>
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
