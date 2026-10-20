using System;
using System.Collections.Generic;
using System.IO;
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
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        throw new NotImplementedException();
    }
}
