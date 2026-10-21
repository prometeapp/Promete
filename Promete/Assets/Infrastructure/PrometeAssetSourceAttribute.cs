using System;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// アセンブリが持つアセットの読み込み元を表す属性です。MSBuild 統合 (<c>PrometeAsset</c>) がビルド時に書き込みます。
/// </summary>
/// <remarks>
/// <para>
/// <b>バックエンド実装者向けの型です。</b> アプリのコードで直接書く必要はありません。
/// 引数なしの <c>UseAssets()</c> や <c>UseAssets(Assembly)</c> は、この属性を読み、<see cref="IAssetManifestLoader"/> でソースを作ってマウントします。
/// </para>
/// <para>
/// 1 つのアセンブリに複数付きます (パッケージを複数に分けた場合など)。
/// <see cref="Location"/> は、出力先 (<see cref="AppContext.BaseDirectory"/>。Web では仮想ファイルシステムのルート) からの相対ファイル名か、
/// <see cref="KindEmbeddedPackage"/> のときは埋め込みリソースの名前です。絶対パスは書かれません。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class PrometeAssetSourceAttribute : Attribute
{
    /// <summary>
    /// <see cref="Kind"/> の値: Folder モードのマニフェスト。<see cref="Location"/> はマニフェストのファイル名です。
    /// </summary>
    public const string KindManifest = "Manifest";

    /// <summary>
    /// <see cref="Kind"/> の値: 出力先に置いたアセットパッケージ。<see cref="Location"/> はパッケージのファイル名です。
    /// </summary>
    public const string KindPackage = "Package";

    /// <summary>
    /// <see cref="Kind"/> の値: アセンブリに埋め込んだアセットパッケージ。<see cref="Location"/> は埋め込みリソースの名前 (<c>LogicalName</c>) です。
    /// </summary>
    public const string KindEmbeddedPackage = "EmbeddedPackage";

    /// <summary>
    /// <see cref="Environment"/> の値: デスクトップ向けのビルド。
    /// </summary>
    public const string EnvironmentDesktop = "Desktop";

    /// <summary>
    /// <see cref="Environment"/> の値: Web (WebAssembly) 向けのビルド。
    /// </summary>
    public const string EnvironmentWeb = "Web";

    /// <summary>
    /// <see cref="PrometeAssetSourceAttribute"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="kind">読み込み元の種類 (<see cref="KindManifest"/> など)。</param>
    /// <param name="location">出力先からの相対ファイル名、または埋め込みリソースの名前。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public PrometeAssetSourceAttribute(string kind, string location)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(location);

        Kind = kind;
        Location = location;
    }

    /// <summary>
    /// 読み込み元の種類を取得します (<see cref="KindManifest"/>、<see cref="KindPackage"/>、<see cref="KindEmbeddedPackage"/>)。
    /// </summary>
    /// <remarks>新しい版の Promete でビルドされたアセンブリは、これら以外の値を持つことがあります。</remarks>
    public string Kind { get; }

    /// <summary>
    /// 出力先からの相対ファイル名、または埋め込みリソースの名前を取得します。
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// ビルドした環境 (<see cref="EnvironmentDesktop"/> または <see cref="EnvironmentWeb"/>) を取得または設定します。
    /// </summary>
    public string Environment { get; set; } = EnvironmentDesktop;

    /// <summary>
    /// パッケージ名 (<c>PrometeAsset</c> の <c>Package</c> メタデータ) を取得または設定します。マニフェストの場合は <see langword="null"/> です。
    /// </summary>
    public string? Package { get; set; }
}
