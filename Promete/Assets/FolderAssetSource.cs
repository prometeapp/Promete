using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Promete.Assets;

/// <summary>
/// 実際のフォルダの中身を、論理パスで読み出す <see cref="IAssetSource"/> です。
/// </summary>
/// <remarks>
/// <para>
/// ファイルシステムが大文字・小文字を区別しない環境 (Windows / macOS の既定) でも、論理パスの大文字・小文字を区別します。
/// パスの各要素 (ディレクトリを含む) を、ディレクトリを列挙して得た実際の名前と序数比較で照合します。
/// 大文字・小文字だけが違うファイルを開こうとすると、<see cref="AssetPathCaseMismatchException"/> をスローします。
/// </para>
/// <para>
/// ディレクトリごとの名前の一覧はキャッシュします。見つからなかったときは、そのディレクトリを列挙し直してから判断するので、
/// 実行中に追加したファイルも読めます。
/// </para>
/// </remarks>
public sealed class FolderAssetSource : IFileBackedAssetSource
{
    /// <summary>
    /// ディレクトリの物理パスから、その直下の名前の一覧へのキャッシュ。
    /// </summary>
    private readonly ConcurrentDictionary<string, Dictionary<string, DirectoryEntry>> _listings =
        new(StringComparer.Ordinal);

    /// <summary>
    /// <see cref="MountPoint"/> を <c>/</c> で分割したもの。接頭辞が無い場合は空配列です。
    /// </summary>
    private readonly string[] _mountSegments;

    /// <summary>
    /// <see cref="FolderAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="rootPath">読み出すフォルダのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">このフォルダを置く論理パスの接頭辞。例えば <c>"assets"</c> を指定すると、フォルダ直下の <c>x.png</c> は <c>"assets/x.png"</c> になります。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="rootPath"/> のフォルダが存在しない場合。</exception>
    public FolderAssetSource(string rootPath, string mountPoint = "")
    {
        ArgumentNullException.ThrowIfNull(rootPath);

        MountPoint = AssetPath.NormalizeDirectory(mountPoint);
        _mountSegments = SplitSegments(MountPoint);

        var fullPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(rootPath, AppContext.BaseDirectory)
        );
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"フォルダ '{fullPath}' が見つかりません。");
        }

        RootPath = fullPath;
    }

    /// <summary>
    /// 読み出すフォルダのフルパスを取得します。
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// 正規化した論理パスの接頭辞を取得します。接頭辞が無い場合は空文字列です。
    /// </summary>
    public string MountPoint { get; }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        return TryResolve(AssetPath.Normalize(path), out _);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        var normalized = AssetPath.Normalize(path);
        if (!TryResolve(normalized, out var physicalPath))
        {
            throw new FileNotFoundException(
                $"アセット '{normalized}' が見つかりません。",
                normalized
            );
        }

        return new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete
        );
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        var normalizedDirectory = AssetPath.NormalizeDirectory(directory);
        // パターンの検証 (null と区切り文字を弾く)
        AssetPath.IsMatch(string.Empty, pattern);

        var results = new List<string>();
        var directorySegments = SplitSegments(normalizedDirectory);
        if (directorySegments.Length <= _mountSegments.Length)
        {
            // directory は MountPoint と同じか、その祖先
            if (!StartsWithSegments(_mountSegments, directorySegments))
            {
                return results;
            }

            if (recursive || directorySegments.Length == _mountSegments.Length)
            {
                CollectFiles(RootPath, MountPoint, pattern, recursive, results);
            }
        }
        else
        {
            // directory は MountPoint の下
            if (!StartsWithSegments(directorySegments, _mountSegments))
            {
                return results;
            }

            var physicalDirectory = ResolveDirectory(directorySegments[_mountSegments.Length..]);
            if (physicalDirectory is not null)
            {
                CollectFiles(physicalDirectory, normalizedDirectory, pattern, recursive, results);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        return TryResolve(AssetPath.Normalize(path), out fullPath);
    }

    /// <summary>
    /// 論理パス (正規化済み) を分割します。空の場合は空配列を返します。
    /// </summary>
    /// <param name="normalized">正規化済みの論理パス。</param>
    /// <returns>各要素の配列。</returns>
    private static string[] SplitSegments(string normalized)
    {
        return normalized.Length == 0 ? [] : normalized.Split('/');
    }

    /// <summary>
    /// <paramref name="segments"/> が <paramref name="prefix"/> で始まるかどうかを、序数比較で判定します。
    /// </summary>
    /// <param name="segments">調べる要素の配列。</param>
    /// <param name="prefix">先頭に並んでいるはずの要素の配列。</param>
    /// <returns>始まる場合は <see langword="true"/>。</returns>
    private static bool StartsWithSegments(string[] segments, string[] prefix)
    {
        return segments.Length >= prefix.Length
            && segments.AsSpan(0, prefix.Length).SequenceEqual(prefix);
    }

    /// <summary>
    /// 論理パス (正規化済み) を物理パスに解決します。ファイルだけを対象にします。
    /// </summary>
    /// <param name="normalizedPath">正規化済みの論理パス。</param>
    /// <param name="physicalPath">見つかった場合の物理フルパス。見つからない場合は <see langword="null"/>。</param>
    /// <returns>見つかった場合は <see langword="true"/>。</returns>
    private bool TryResolve(string normalizedPath, [NotNullWhen(true)] out string? physicalPath)
    {
        physicalPath = null;

        var segments = SplitSegments(normalizedPath);
        if (
            segments.Length <= _mountSegments.Length
            || !StartsWithSegments(segments, _mountSegments)
        )
        {
            return false;
        }

        var relative = segments[_mountSegments.Length..];
        var current = RootPath;
        for (var i = 0; i < relative.Length; i++)
        {
            if (!GetListing(current).TryGetValue(relative[i], out var entry))
            {
                return false;
            }

            var isLast = i == relative.Length - 1;
            if (isLast && entry.IsDirectory)
            {
                return false;
            }

            if (!isLast && !entry.IsDirectory)
            {
                return false;
            }

            current = Path.Combine(current, entry.ActualName);
        }

        physicalPath = current;
        return true;
    }

    /// <summary>
    /// 残りの要素を辿って、ディレクトリの物理パスを解決します。
    /// </summary>
    /// <param name="segments">MountPoint を除いたディレクトリの要素。</param>
    /// <returns>見つかった物理パス。無ければ <see langword="null"/>。</returns>
    private string? ResolveDirectory(string[] segments)
    {
        var current = RootPath;
        foreach (var segment in segments)
        {
            if (!GetListing(current).TryGetValue(segment, out var entry) || !entry.IsDirectory)
            {
                return null;
            }

            current = Path.Combine(current, entry.ActualName);
        }

        return current;
    }

    /// <summary>
    /// キャッシュ済みの名前の一覧を取得します。無ければ列挙して保存します。
    /// </summary>
    /// <param name="physicalDirectory">ディレクトリの物理パス。</param>
    /// <returns>NFC の名前をキーにした一覧。</returns>
    private Dictionary<string, DirectoryEntry> GetListing(string physicalDirectory)
    {
        return _listings.TryGetValue(physicalDirectory, out var cached)
            ? cached
            : ReadListing(physicalDirectory);
    }

    /// <summary>
    /// ディレクトリを列挙し直して、名前の一覧を作り直してキャッシュに保存します。
    /// </summary>
    /// <param name="physicalDirectory">ディレクトリの物理パス。</param>
    /// <returns>作り直した一覧。ディレクトリが無い場合は空。</returns>
    private Dictionary<string, DirectoryEntry> ReadListing(string physicalDirectory)
    {
        var listing = new Dictionary<string, DirectoryEntry>(StringComparer.Ordinal);
        try
        {
            foreach (var info in new DirectoryInfo(physicalDirectory).EnumerateFileSystemInfos())
            {
                listing[AssetPath.NormalizeUnicode(info.Name)] = new DirectoryEntry(
                    info.Name,
                    info is DirectoryInfo
                );
            }
        }
        catch (IOException)
        {
            // ディレクトリが無い (途中で消えた) 場合は空の一覧として扱う
            listing.Clear();
        }

        _listings[physicalDirectory] = listing;
        return listing;
    }

    /// <summary>
    /// ディレクトリ内のファイルの論理パスを集めます。
    /// </summary>
    /// <param name="physicalDirectory">列挙するディレクトリの物理パス。</param>
    /// <param name="logicalDirectory">そのディレクトリの論理パス (正規化済み)。</param>
    /// <param name="pattern">ファイル名のパターン。</param>
    /// <param name="recursive">サブディレクトリも列挙する場合は <see langword="true"/>。</param>
    /// <param name="results">結果を追加するリスト。</param>
    private void CollectFiles(
        string physicalDirectory,
        string logicalDirectory,
        string pattern,
        bool recursive,
        List<string> results
    )
    {
        // 列挙のたびに物理ディレクトリを読み直す (結果でキャッシュは更新する)
        foreach (var (name, entry) in ReadListing(physicalDirectory))
        {
            var logicalPath = logicalDirectory.Length == 0 ? name : logicalDirectory + "/" + name;
            if (entry.IsDirectory)
            {
                if (recursive)
                {
                    CollectFiles(
                        Path.Combine(physicalDirectory, entry.ActualName),
                        logicalPath,
                        pattern,
                        recursive,
                        results
                    );
                }
            }
            else if (AssetPath.IsMatch(name, pattern))
            {
                results.Add(logicalPath);
            }
        }
    }

    /// <summary>
    /// ディレクトリの直下にある 1 つの要素の情報です。
    /// </summary>
    /// <param name="ActualName">ファイルシステム上の実際の名前。</param>
    /// <param name="IsDirectory">ディレクトリの場合は <see langword="true"/>。</param>
    private readonly record struct DirectoryEntry(string ActualName, bool IsDirectory);
}
