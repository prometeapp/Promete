using System;
using System.Collections.Generic;
using System.Reflection;

namespace Promete.Assets;

/// <summary>
/// <see cref="AssetsAppBuilderExtension.UseAssets(PrometeApp.PrometeAppBuilder, Action{AssetsOptions}?)"/> で、明示的にマウントするソースを指定します。
/// </summary>
/// <remarks>
/// <para>
/// ここで指定したマウントは、<see cref="AssetManager"/> が作られる時点 (アプリの開始時。プラグインの <c>OnStart</c> より前) に、指定した順に行われます。
/// 優先度が同じ場合は、後に指定したものが優先されます。
/// </para>
/// <para>
/// アセンブリに付いた <c>PrometeAsset</c> の属性によるマウントは、ここで指定したマウントより先に、より低い優先度で行われます。
/// そのため、ここで指定したマウントは、属性によるマウントの上に重なり、同じ論理パスのファイルを上書きします。
/// </para>
/// </remarks>
public sealed class AssetsOptions
{
    private readonly List<(Func<IAssetSource> Factory, int Priority)> _mounts = [];

    /// <summary>
    /// 指定された順に並んだ、マウントの要求。ソースの作成は <see cref="AssetManager"/> が行います。
    /// </summary>
    internal IReadOnlyList<(Func<IAssetSource> Factory, int Priority)> Mounts => _mounts;

    /// <summary>
    /// 実際のフォルダをマウントします。
    /// </summary>
    /// <param name="physicalPath">フォルダのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">論理パスの接頭辞。例えば <c>MountFolder("assets", "assets")</c> とすると、<c>"assets/x.png"</c> で読めます。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <remarks>
    /// フォルダが存在しない場合は、マウントする時点 (アプリの開始時) で <see cref="System.IO.DirectoryNotFoundException"/> がスローされます。
    /// <paramref name="mountPoint"/> の誤りは、この呼び出し (構成時) で検出されます。
    /// </remarks>
    public AssetsOptions MountFolder(string physicalPath, string mountPoint = "", int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(physicalPath);
        ArgumentNullException.ThrowIfNull(mountPoint);
        AssetPath.NormalizeDirectory(mountPoint);

        _mounts.Add((() => new FolderAssetSource(physicalPath, mountPoint), priority));
        return this;
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
    /// <exception cref="ArgumentException"><paramref name="resourcePrefix"/> または <paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    public AssetsOptions MountEmbeddedResources(
        Assembly assembly,
        string resourcePrefix = "",
        string mountPoint = "",
        int priority = 0
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourcePrefix);
        ArgumentNullException.ThrowIfNull(mountPoint);
        AssetPath.NormalizeDirectory(resourcePrefix);
        AssetPath.NormalizeDirectory(mountPoint);

        _mounts.Add(
            (() => new EmbeddedResourceAssetSource(assembly, resourcePrefix, mountPoint), priority)
        );
        return this;
    }

    /// <summary>
    /// アセットパッケージのファイルをマウントします (<see cref="PackageAssetSource"/>)。
    /// </summary>
    /// <param name="filePath">パッケージのファイルのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">論理パスの接頭辞。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <remarks>
    /// ファイルが存在しない場合や壊れている場合は、マウントする時点 (アプリの開始時) で
    /// <see cref="System.IO.FileNotFoundException"/> や <see cref="System.IO.InvalidDataException"/> がスローされます。
    /// <paramref name="mountPoint"/> の誤りは、この呼び出し (構成時) で検出されます。
    /// </remarks>
    public AssetsOptions MountPackage(string filePath, string mountPoint = "", int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(mountPoint);
        AssetPath.NormalizeDirectory(mountPoint);

        _mounts.Add((() => new PackageAssetSource(filePath, mountPoint), priority));
        return this;
    }

    /// <summary>
    /// アセンブリに埋め込んだアセットパッケージをマウントします (<see cref="PackageAssetSource.FromEmbeddedResource"/>)。
    /// </summary>
    /// <param name="assembly">リソースを持つアセンブリ。</param>
    /// <param name="resourceName">パッケージのリソースの名前 (<c>LogicalName</c>)。</param>
    /// <param name="mountPoint">論理パスの接頭辞。</param>
    /// <param name="priority">優先度。大きいほど優先されます。</param>
    /// <returns>このインスタンス。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <remarks>
    /// リソースが見つからない場合や壊れている場合は、マウントする時点 (アプリの開始時) で
    /// <see cref="System.IO.FileNotFoundException"/> や <see cref="System.IO.InvalidDataException"/> がスローされます。
    /// </remarks>
    public AssetsOptions MountEmbeddedPackage(
        Assembly assembly,
        string resourceName,
        string mountPoint = "",
        int priority = 0
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourceName);
        ArgumentNullException.ThrowIfNull(mountPoint);
        AssetPath.NormalizeDirectory(mountPoint);

        _mounts.Add(
            (
                () => PackageAssetSource.FromEmbeddedResource(assembly, resourceName, mountPoint),
                priority
            )
        );
        return this;
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
        ArgumentNullException.ThrowIfNull(source);

        _mounts.Add((() => source, priority));
        return this;
    }
}
