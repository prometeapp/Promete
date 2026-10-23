using System;
using System.IO;
using System.Reflection;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// デスクトップ向けにビルドされた <see cref="PrometeAssetSourceAttribute"/> を読む、<see cref="IAssetManifestLoader"/> の既定の実装です。
/// </summary>
/// <remarks>
/// <see cref="PrometeAssetSourceAttribute.Location"/> は、出力先 (既定では <see cref="AppContext.BaseDirectory"/>) を基準に解決します。
/// NativeAOT や単一ファイルの publish では <see cref="Assembly.Location"/> が空になるため、アセンブリの場所は使いません。
/// </remarks>
internal sealed class DesktopAssetManifestLoader : IAssetManifestLoader
{
    /// <summary>
    /// 出力先を <see cref="AppContext.BaseDirectory"/> とする、<see cref="DesktopAssetManifestLoader"/> の新しいインスタンスを初期化します。
    /// </summary>
    public DesktopAssetManifestLoader()
        : this(AppContext.BaseDirectory) { }

    /// <summary>
    /// 出力先を指定して、<see cref="DesktopAssetManifestLoader"/> の新しいインスタンスを初期化します。テスト用です。
    /// </summary>
    /// <param name="baseDirectory">ファイル名を解決する基準のディレクトリ。</param>
    internal DesktopAssetManifestLoader(string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);

        BaseDirectory = baseDirectory;
    }

    /// <summary>
    /// ファイル名を解決する基準のディレクトリを取得します。
    /// </summary>
    internal string BaseDirectory { get; }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="PrometeAssetSourceAttribute.Environment"/> が <see cref="PrometeAssetSourceAttribute.EnvironmentDesktop"/> でない場合。
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// マニフェストやパッケージのファイル、埋め込みリソースが無い場合。メッセージにファイル名と、探した場所を含みます。
    /// </exception>
    /// <exception cref="NotSupportedException"><see cref="PrometeAssetSourceAttribute.Kind"/> を知らない場合。</exception>
    public IAssetSource CreateSource(Assembly assembly, PrometeAssetSourceAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(attribute);

        if (attribute.Environment != PrometeAssetSourceAttribute.EnvironmentDesktop)
        {
            throw new InvalidOperationException(
                $"アセンブリ '{assembly.GetName().Name}' は環境 '{attribute.Environment}' 向けにビルドされたアセットを持っています。"
                    + "デスクトップの実行プロジェクトで PrometeAsset を宣言してください。"
            );
        }

        return attribute.Kind switch
        {
            PrometeAssetSourceAttribute.KindManifest => CreateManifestSource(attribute.Location),
            PrometeAssetSourceAttribute.KindPackage => CreatePackageSource(attribute.Location),
            PrometeAssetSourceAttribute.KindEmbeddedPackage =>
                PackageAssetSource.FromEmbeddedResource(assembly, attribute.Location),
            _ => throw new NotSupportedException(
                $"未対応の読み込み元の種類 '{attribute.Kind}' です ('{attribute.Location}')。"
                    + "新しい版の Promete でビルドされたアセンブリの可能性があります。"
            ),
        };
    }

    /// <summary>
    /// 出力先のマニフェストを読み、<see cref="ManifestAssetSource"/> を作ります。
    /// </summary>
    /// <param name="location">出力先からの相対ファイル名。</param>
    /// <returns>マニフェストのソース。</returns>
    /// <exception cref="FileNotFoundException">マニフェストが無い場合。</exception>
    private ManifestAssetSource CreateManifestSource(string location)
    {
        var path = Path.Combine(BaseDirectory, location);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"マニフェスト '{location}' が '{BaseDirectory}' にありません。"
                    + "Folder モードのマニフェストはビルドした出力先にだけ置かれ、publish には載りません。"
                    + "出力先から実行するか、Package モードでビルドしてください。",
                path
            );
        }

        return new ManifestAssetSource(DesktopAssetManifest.Read(path), $"'{location}'");
    }

    /// <summary>
    /// 出力先のパッケージを開き、<see cref="PackageAssetSource"/> を作ります。
    /// </summary>
    /// <param name="location">出力先からの相対ファイル名。</param>
    /// <returns>パッケージのソース。</returns>
    /// <exception cref="FileNotFoundException">パッケージが無い場合。</exception>
    private PackageAssetSource CreatePackageSource(string location)
    {
        var path = Path.Combine(BaseDirectory, location);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"パッケージ '{location}' が '{BaseDirectory}' にありません。",
                path
            );
        }

        return new PackageAssetSource(path);
    }
}
