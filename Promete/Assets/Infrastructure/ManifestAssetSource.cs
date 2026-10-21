using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// マニフェストに載った「論理パス → 実ファイル」の対応だけを読める <see cref="IAssetSource"/> です。
/// MSBuild 統合 (<c>PrometeAsset</c>) の Folder モードで使います。
/// </summary>
/// <remarks>
/// <para>
/// <b>バックエンド実装者向けの型です。</b> <see cref="IAssetManifestLoader"/> の実装が、マニフェストを読んで作ります。
/// </para>
/// <para>
/// マニフェストに載っていないファイルは、実ファイルがあっても読めません。開発中 (Folder モード) にだけ読めてリリース (Package モード) で
/// 読めなくなる事故を防ぐためです。論理パスは大文字・小文字を区別し、マニフェストのキーと序数比較で照合します。
/// 大文字・小文字だけが違うパスで開こうとすると <see cref="AssetPathCaseMismatchException"/> をスローします。
/// </para>
/// <para>
/// <see cref="Enumerate"/> はマニフェストに載ったパスを返します (実ファイルの有無は確かめません)。
/// <see cref="Exists"/> は、マニフェストに載っていて、かつ実ファイルがある場合に <see langword="true"/> を返します。
/// </para>
/// <para>このクラスは不変で、どのスレッドから呼んでも安全です。</para>
/// </remarks>
public sealed class ManifestAssetSource : IFileBackedAssetSource
{
    private readonly Dictionary<string, string> _files;
    private readonly string[] _sortedKeys;
    private readonly Dictionary<string, string> _caseHints;
    private readonly string _description;

    /// <summary>
    /// <see cref="ManifestAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="files">論理パスと、実ファイルの絶対パスの組。論理パスは <see cref="AssetPath.Normalize"/> で正規化します。</param>
    /// <param name="description">例外のメッセージに使う、マニフェストの説明 (ファイル名など)。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// 論理パスが正しくない場合、正規化すると同じになる論理パスが複数ある場合、実ファイルのパスが <see langword="null"/> または絶対パスでない場合。
    /// </exception>
    public ManifestAssetSource(IEnumerable<KeyValuePair<string, string>> files, string description)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(description);

        _description = description;
        _files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in files)
        {
            var key = AssetPath.Normalize(pair.Key);
            if (pair.Value is null || !Path.IsPathRooted(pair.Value))
            {
                throw new ArgumentException(
                    $"論理パス '{key}' の実ファイルのパスは絶対パスである必要があります。",
                    nameof(files)
                );
            }

            if (!_files.TryAdd(key, pair.Value))
            {
                throw new ArgumentException($"論理パス '{key}' が重複しています。", nameof(files));
            }
        }

        _sortedKeys = _files.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();

        // 同じ綴り (大文字・小文字だけが違う) が複数あれば、序数順で最初のものをヒントにする
        _caseHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _sortedKeys)
        {
            _caseHints.TryAdd(key, key);
        }
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        var normalized = AssetPath.Normalize(path);

        return _files.TryGetValue(normalized, out var physicalPath) && File.Exists(physicalPath);
    }

    /// <inheritdoc />
    /// <exception cref="AssetPathCaseMismatchException">マニフェストに無く、大文字・小文字だけが違う論理パスがある場合。</exception>
    /// <exception cref="FileNotFoundException">
    /// マニフェストに載っていない場合 (メッセージで <c>PrometeAsset</c> での宣言を案内します)。載っているが実ファイルが無い場合 (メッセージに実ファイルのパスを含みます)。
    /// </exception>
    public Stream OpenRead(string path)
    {
        var normalized = AssetPath.Normalize(path);
        if (!_files.TryGetValue(normalized, out var physicalPath))
        {
            if (_caseHints.TryGetValue(normalized, out var actual))
            {
                throw new AssetPathCaseMismatchException(normalized, actual);
            }

            throw new FileNotFoundException(
                $"アセット '{normalized}' が {_description} に見つかりません。csproj の PrometeAsset で宣言してください。",
                normalized
            );
        }

        try
        {
            // 開発中に実ファイルを作り直せるよう、削除の共有を許可する
            return new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete
            );
        }
        catch (FileNotFoundException e)
        {
            throw MissingPhysicalFile(normalized, physicalPath, e);
        }
        catch (DirectoryNotFoundException e)
        {
            throw MissingPhysicalFile(normalized, physicalPath, e);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        var normalizedDirectory = AssetPath.NormalizeDirectory(directory);

        // パターンの検証 (null と区切り文字を弾く)
        AssetPath.IsMatch(string.Empty, pattern);

        var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";

        return _sortedKeys
            .Where(path =>
            {
                if (!path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }

                var rest = path[prefix.Length..];
                if (!recursive && rest.Contains('/'))
                {
                    return false;
                }

                var fileName = rest[(rest.LastIndexOf('/') + 1)..];
                return AssetPath.IsMatch(fileName, pattern);
            })
            .ToArray();
    }

    /// <inheritdoc />
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        var normalized = AssetPath.Normalize(path);
        if (_files.TryGetValue(normalized, out var physicalPath))
        {
            fullPath = physicalPath;
            return true;
        }

        fullPath = null;
        return false;
    }

    /// <summary>
    /// マニフェストに載っているのに実ファイルが無いときの例外を作ります。
    /// </summary>
    private FileNotFoundException MissingPhysicalFile(
        string normalized,
        string physicalPath,
        Exception inner
    ) =>
        new(
            $"アセット '{normalized}' の実ファイル '{physicalPath}' が見つかりません ({_description})。ビルドし直してください。",
            physicalPath,
            inner
        );
}
