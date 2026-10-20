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
/// 論理パスとして正しくない名前 (<c>\</c> を含むもの、正規化すると変わるもの) のリソースは無視します。
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
    /// <param name="resourcePrefix">対象にするリソース名の接頭辞。この接頭辞で始まるリソースだけを対象にし、接頭辞を取り除いた残りを論理パスにします。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    public EmbeddedResourceAssetSource(
        Assembly assembly,
        string resourcePrefix = "",
        string mountPoint = ""
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourcePrefix);

        _assembly = assembly;
        var normalizedMountPoint = AssetPath.NormalizeDirectory(mountPoint);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = resourceName[resourcePrefix.Length..];
            if (!TryNormalizeResourcePath(rest, out var logicalRest))
            {
                continue;
            }

            var logicalPath =
                normalizedMountPoint.Length == 0
                    ? logicalRest
                    : normalizedMountPoint + "/" + logicalRest;
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
    /// リソース名の接頭辞を取り除いた部分が、正規化しても変わらない有効な論理パスかどうかを判定します。
    /// </summary>
    /// <param name="rest">接頭辞を取り除いたリソース名。</param>
    /// <param name="normalized">有効な場合の正規化済みパス。</param>
    /// <returns>有効な場合は <see langword="true"/>。</returns>
    private static bool TryNormalizeResourcePath(string rest, out string normalized)
    {
        try
        {
            normalized = AssetPath.Normalize(rest);
        }
        catch (ArgumentException)
        {
            normalized = string.Empty;
            return false;
        }

        return normalized == rest;
    }
}
