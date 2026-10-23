using System.Reflection;
using Promete.Assets;
using Promete.Assets.Infrastructure;

namespace Promete.Web.Assets;

/// <summary>
/// Web 向けにビルドされた <see cref="PrometeAssetSourceAttribute"/> を読む <see cref="IAssetManifestLoader"/> です。
/// <see cref="WebAppExtension.BuildWithWeb"/> が登録します。
/// </summary>
/// <remarks>
/// <para>ファイルは仮想ファイルシステム (MEMFS) のルート (<c>/</c>) を基準に探します。<c>promete.js</c> がプリロードで置いたものです。</para>
/// <list type="bullet">
/// <item><c>Manifest</c>: Web 形式のマニフェスト (<see cref="WebAssetManifest"/>) の <c>file</c> と <c>font</c> のエントリから、<see cref="ManifestAssetSource"/> を作ります。
/// 実ファイルのパスは、ルートとエントリの <c>path</c> をつないだものです。</item>
/// <item><c>Package</c>: ルートに置かれたパッケージを開き、<see cref="WebPackageAssetSource"/> で包みます。</item>
/// <item><c>EmbeddedPackage</c>: 埋め込みリソースのパッケージを開き、<see cref="WebPackageAssetSource"/> で包みます。</item>
/// </list>
/// <para>フォントの取り出し先は <see cref="WebPackageFonts.GetFontDirectory"/> (ルートと <see cref="PrometeAssetSourceAttribute.Location"/> から決まる) です。</para>
/// </remarks>
internal sealed class WebAssetManifestLoader : IAssetManifestLoader
{
    /// <summary>
    /// 仮想ファイルシステムのルート (<c>/</c>) を基準にする、<see cref="WebAssetManifestLoader"/> の新しいインスタンスを初期化します。
    /// </summary>
    public WebAssetManifestLoader()
        : this("/") { }

    /// <summary>
    /// 基準のディレクトリを指定して、<see cref="WebAssetManifestLoader"/> の新しいインスタンスを初期化します。テスト用です。
    /// </summary>
    /// <param name="rootDirectory">仮想ファイルシステムのルートの代わりにするディレクトリ。</param>
    internal WebAssetManifestLoader(string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);

        RootDirectory = rootDirectory;
    }

    /// <summary>
    /// ファイルを探す基準のディレクトリを取得します。
    /// </summary>
    internal string RootDirectory { get; }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="InvalidOperationException">
    /// デスクトップ向けにビルドされた属性の場合。メッセージで、Web の実行プロジェクトで <c>PrometeAsset</c> を宣言するよう案内します。
    /// </exception>
    /// <exception cref="FileNotFoundException">マニフェストやパッケージが仮想ファイルシステムに無い場合、埋め込みリソースが無い場合。</exception>
    /// <exception cref="NotSupportedException"><see cref="PrometeAssetSourceAttribute.Kind"/> を知らない場合。</exception>
    public IAssetSource CreateSource(Assembly assembly, PrometeAssetSourceAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(attribute);

        if (attribute.Environment != PrometeAssetSourceAttribute.EnvironmentWeb)
        {
            throw new InvalidOperationException(
                $"アセンブリ '{assembly.GetName().Name}' はデスクトップ向けにビルドされているので Web では読めません。"
                    + "Web の実行プロジェクトで PrometeAsset を宣言し、"
                    + "UseAssets(typeof(Program).Assembly) で Web の実行プロジェクトのアセンブリを指定してください。"
            );
        }

        return attribute.Kind switch
        {
            PrometeAssetSourceAttribute.KindManifest => CreateManifestSource(attribute.Location),
            PrometeAssetSourceAttribute.KindPackage => CreatePackageSource(attribute.Location),
            PrometeAssetSourceAttribute.KindEmbeddedPackage => CreateEmbeddedPackageSource(
                assembly,
                attribute.Location
            ),
            _ => throw new NotSupportedException(
                $"未対応の読み込み元の種類 '{attribute.Kind}' です ('{attribute.Location}')。"
                    + "新しい版の Promete でビルドされたアセンブリの可能性があります。"
            ),
        };
    }

    /// <summary>
    /// 仮想ファイルシステムのマニフェストを読み、<c>file</c> と <c>font</c> のエントリだけを <see cref="ManifestAssetSource"/> にします。
    /// </summary>
    /// <param name="location">ルートからのマニフェストのファイル名。</param>
    /// <returns>マニフェストのソース。</returns>
    /// <exception cref="FileNotFoundException">マニフェストが仮想ファイルシステムに無い場合。</exception>
    private ManifestAssetSource CreateManifestSource(string location)
    {
        var path = Path.Combine(RootDirectory, location);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"マニフェスト '{location}' が '{RootDirectory}' にありません。"
                    + "Promete.Web の startPromete で起動しているか確認してください。",
                path
            );
        }

        var entries = WebAssetManifest.Parse(File.ReadAllBytes(path), $"'{location}'");
        var files = new List<KeyValuePair<string, string>>();
        foreach (var entry in entries)
        {
            if (
                entry.Kind is not (WebAssetManifest.KindFile or WebAssetManifest.KindFont)
                || entry.LogicalPath is null
                || entry.Path is null
            )
            {
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(RootDirectory, entry.Path.TrimStart('/')));
            files.Add(new KeyValuePair<string, string>(entry.LogicalPath, fullPath));
        }

        return new ManifestAssetSource(files, $"'{location}'");
    }

    /// <summary>
    /// 仮想ファイルシステムに置かれたパッケージを開き、<see cref="WebPackageAssetSource"/> で包みます。
    /// </summary>
    /// <param name="location">ルートからのパッケージのファイル名。</param>
    /// <returns>パッケージのソース。</returns>
    /// <exception cref="FileNotFoundException">パッケージが仮想ファイルシステムに無い場合。</exception>
    private WebPackageAssetSource CreatePackageSource(string location)
    {
        var path = Path.Combine(RootDirectory, location);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"パッケージ '{location}' が '{RootDirectory}' にありません。",
                path
            );
        }

        return new WebPackageAssetSource(
            new PackageAssetSource(path),
            WebPackageFonts.GetFontDirectory(RootDirectory, location)
        );
    }

    /// <summary>
    /// 埋め込みリソースのパッケージを開き、<see cref="WebPackageAssetSource"/> で包みます。
    /// </summary>
    /// <param name="assembly">リソースを持つアセンブリ。</param>
    /// <param name="resourceName">埋め込みリソースの名前。</param>
    /// <returns>パッケージのソース。</returns>
    /// <exception cref="FileNotFoundException">埋め込みリソースが無い場合。</exception>
    private WebPackageAssetSource CreateEmbeddedPackageSource(
        Assembly assembly,
        string resourceName
    )
    {
        return new WebPackageAssetSource(
            PackageAssetSource.FromEmbeddedResource(assembly, resourceName),
            WebPackageFonts.GetFontDirectory(RootDirectory, resourceName)
        );
    }
}
