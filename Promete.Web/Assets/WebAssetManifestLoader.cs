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
        throw new NotImplementedException();
    }
}
