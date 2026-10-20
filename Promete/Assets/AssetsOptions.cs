using System;
using System.Reflection;

namespace Promete.Assets;

/// <summary>
/// <see cref="AssetsAppBuilderExtension.UseAssets(PrometeApp.PrometeAppBuilder, Action{AssetsOptions}?)"/> で、明示的にマウントするソースを指定します。
/// </summary>
/// <remarks>
/// ここで指定したマウントは、<see cref="AssetManager"/> が作られる時点 (アプリの開始時。プラグインの <c>OnStart</c> より前) に、指定した順に行われます。
/// 優先度が同じ場合は、後に指定したものが優先されます。
/// </remarks>
public sealed class AssetsOptions
{
    /// <summary>
    /// 実際のフォルダをマウントします。
    /// </summary>
    /// <param name="physicalPath">フォルダのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">論理パスの接頭辞。例えば <c>MountFolder("assets", "assets")</c> とすると、<c>"assets/x.png"</c> で読めます。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <remarks>フォルダが存在しない場合は、マウントする時点で <see cref="System.IO.DirectoryNotFoundException"/> がスローされます。</remarks>
    public AssetsOptions MountFolder(string physicalPath, string mountPoint = "", int priority = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// アセンブリの埋め込みリソースをマウントします。
    /// </summary>
    /// <param name="assembly">リソースを読み出すアセンブリ。</param>
    /// <param name="resourcePrefix">対象にするリソース名の接頭辞 (<see cref="EmbeddedResourceAssetSource"/>)。</param>
    /// <param name="mountPoint">論理パスの接頭辞。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public AssetsOptions MountEmbeddedResources(
        Assembly assembly,
        string resourcePrefix = "",
        string mountPoint = "",
        int priority = 0
    )
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 任意のソースをマウントします。
    /// </summary>
    /// <param name="source">マウントするソース。<see cref="IDisposable"/> を実装している場合、所有権は <see cref="AssetManager"/> に移ります。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> が <see langword="null"/> の場合。</exception>
    public AssetsOptions Mount(IAssetSource source, int priority = 0)
    {
        throw new NotImplementedException();
    }
}
