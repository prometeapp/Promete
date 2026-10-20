using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Promete.Assets;

/// <summary>
/// 任意のアセンブリの埋め込みリソースを、論理パスで読み出す <see cref="IAssetSource"/> です。
/// </summary>
/// <remarks>
/// <para>
/// リソース名のうち <c>resourcePrefix</c> より後ろの部分を、<c>/</c> 区切りの論理パスとして扱います。
/// 既定のリソース名 (<c>RootNamespace.assets.x.png</c> のように <c>.</c> 区切り) ではディレクトリを表せないので、
/// csproj で <c>LogicalName</c> を指定してください。
/// </para>
/// <code>
/// &lt;EmbeddedResource Include="assets\x.png" LogicalName="assets/x.png" /&gt;
/// </code>
/// <para>
/// 接頭辞はディレクトリとして扱います。<c>"pre"</c> と <c>"pre/"</c> と <c>"/pre/"</c> は同じ意味で、
/// <c>"pre"</c> は <c>"prefix2/a.txt"</c> には一致しません。
/// </para>
/// <para>
/// リソース名の先頭の <c>/</c> と <c>./</c> は取り除いてから照合します。接頭辞の後ろの部分は
/// <see cref="AssetPath"/> の規則で正規化します (NFD は NFC になります)。正規化すると空になるものは無視します。
/// 開くときは元のリソース名を使います。
/// </para>
/// <para>
/// 接頭辞に一致するリソース名に <c>\</c> が含まれる場合や、正規化すると同じ論理パスになるリソースが複数ある場合は
/// 構築時に <see cref="ArgumentException"/> を投げます。
/// </para>
/// </remarks>
public sealed class EmbeddedResourceAssetSource : IAssetSource
{
    private readonly Assembly _assembly;

    /// <summary>
    /// 論理パス (正規化済み) から、埋め込みリソースの名前への対応表。
    /// </summary>
    private readonly Dictionary<string, string> _resources = new(StringComparer.Ordinal);

    /// <summary>
    /// <see cref="EmbeddedResourceAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="assembly">リソースを読み出すアセンブリ。</param>
    /// <param name="resourcePrefix">対象にするリソース名の接頭辞 (ディレクトリとして扱います)。この接頭辞で始まるリソースだけを対象にし、接頭辞を取り除いた残りを論理パスにします。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="resourcePrefix"/> または <paramref name="mountPoint"/> が <c>\</c> を含む場合。
    /// 接頭辞に一致するリソース名に <c>\</c> が含まれる場合。正規化すると同じ論理パスになるリソースが複数ある場合。
    /// </exception>
    public EmbeddedResourceAssetSource(
        Assembly assembly,
        string resourcePrefix = "",
        string mountPoint = ""
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourcePrefix);

        _assembly = assembly;
        var prefix = ToDirectoryPrefix(resourcePrefix);
        var normalizedMountPoint = AssetPath.NormalizeDirectory(mountPoint);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var name = AssetPath.NormalizeUnicode(TrimLeadingDotSlash(resourceName));
            if (!name.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (name.Contains('\\'))
            {
                throw new ArgumentException(
                    $"リソース名 '{resourceName}' に \\ が含まれています。LogicalName では / を使ってください (Windows の %(RelativeDir) は \\ 区切りです)。"
                );
            }

            var rest = AssetPath.NormalizeDirectory(name[prefix.Length..]);
            if (rest.Length == 0)
            {
                continue;
            }

            var logicalPath =
                normalizedMountPoint.Length == 0 ? rest : normalizedMountPoint + "/" + rest;
            if (_resources.TryGetValue(logicalPath, out var existing))
            {
                throw new ArgumentException(
                    $"リソース '{existing}' と '{resourceName}' は正規化すると同じ論理パス '{logicalPath}' になります。"
                );
            }

            _resources[logicalPath] = resourceName;
        }
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        return _resources.ContainsKey(AssetPath.Normalize(path));
    }

    /// <inheritdoc />
    /// <remarks>
    /// 大文字・小文字だけが違うリソースがある場合は <see cref="AssetPathCaseMismatchException"/> を投げます。
    /// </remarks>
    public Stream OpenRead(string path)
    {
        var normalized = AssetPath.Normalize(path);
        if (_resources.TryGetValue(normalized, out var resourceName))
        {
            var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                return stream;
            }
        }

        foreach (var key in _resources.Keys)
        {
            if (string.Equals(key, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new AssetPathCaseMismatchException(normalized, key);
            }
        }

        throw new FileNotFoundException($"アセット '{normalized}' が見つかりません。", normalized);
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        var normalizedDirectory = AssetPath.NormalizeDirectory(directory);
        // パターンの検証 (null と区切り文字を弾く)
        AssetPath.IsMatch(string.Empty, pattern);

        var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";

        return _resources
            .Keys.Where(key =>
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }

                var rest = key[prefix.Length..];
                if (!recursive && rest.Contains('/'))
                {
                    return false;
                }

                var fileName = rest[(rest.LastIndexOf('/') + 1)..];
                return AssetPath.IsMatch(fileName, pattern);
            })
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 接頭辞を論理パスのディレクトリ形式 (末尾に <c>/</c> を付けたもの、空の場合は空文字列) に変換します。
    /// </summary>
    /// <param name="resourcePrefix">リソース名の接頭辞。</param>
    /// <returns>照合に使う接頭辞。</returns>
    private static string ToDirectoryPrefix(string resourcePrefix)
    {
        var normalized = AssetPath.NormalizeDirectory(resourcePrefix);
        return normalized.Length == 0 ? string.Empty : normalized + "/";
    }

    /// <summary>
    /// リソース名の先頭にある <c>/</c> と <c>./</c> を取り除きます。
    /// </summary>
    /// <param name="resourceName">リソース名。</param>
    /// <returns>取り除いた後の名前。</returns>
    private static string TrimLeadingDotSlash(string resourceName)
    {
        var start = 0;
        while (start < resourceName.Length)
        {
            if (resourceName[start] == '/')
            {
                start++;
            }
            else if (string.CompareOrdinal(resourceName, start, "./", 0, 2) == 0)
            {
                start += 2;
            }
            else
            {
                break;
            }
        }

        return resourceName[start..];
    }
}
