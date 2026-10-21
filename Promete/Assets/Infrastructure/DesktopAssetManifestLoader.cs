using System;
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
        throw new NotImplementedException();
    }
}
