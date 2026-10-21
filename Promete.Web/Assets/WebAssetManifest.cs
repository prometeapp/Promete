namespace Promete.Web.Assets;

/// <summary>
/// Web 形式のマニフェスト (<c>promete-assets.json</c>) を読みます。
/// </summary>
/// <remarks>
/// <para>
/// 形式 (UTF-8 の JSON) は <c>Promete.Web.targets</c> が書き出し、<c>promete.js</c> が取得して仮想ファイルシステムのルートに置きます。
/// 3 者の契約は <c>PrometeWebAssetsContract</c> (今は 1) の中に閉じます。
/// </para>
/// <code>
/// {
///   "version": 1,
///   "entries": [
///     { "kind": "file", "path": "/assets/x.png", "url": "assets/x.png", "logicalPath": "assets/x.png" },
///     { "kind": "font", "path": "/assets/a.ttf", "url": "assets/a.ttf", "logicalPath": "assets/a.ttf" },
///     { "kind": "package", "path": "/MyGame.main.prmtpak", "url": "MyGame.main.prmtpak", "package": "main" },
///     { "kind": "embedded-package", "assembly": "MyGame", "resource": "MyGame.main.prmtpak", "package": "main" }
///   ]
/// }
/// </code>
/// <para>
/// <c>version</c> (整数) と <c>entries</c> (配列) は必須。各エントリの <c>kind</c> (文字列) は必須で、それ以外のプロパティは任意です。
/// 知らないプロパティ (<c>url</c> など、JavaScript だけが使うもの) は読み飛ばします。<c>version</c> が 1 以外なら <see cref="NotSupportedException"/>。
/// </para>
/// </remarks>
internal static class WebAssetManifest
{
    /// <summary>
    /// 読める形式のバージョン。
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// ばらのファイル (フォント以外)。
    /// </summary>
    public const string KindFile = "file";

    /// <summary>
    /// ばらのフォントのファイル。<c>promete.js</c> が <c>FontFace</c> にも登録します。
    /// </summary>
    public const string KindFont = "font";

    /// <summary>
    /// 出力先に置いたアセットパッケージ。<c>promete.js</c> が仮想ファイルシステムに置き、中のフォントを登録します。
    /// </summary>
    public const string KindPackage = "package";

    /// <summary>
    /// アセンブリに埋め込んだアセットパッケージ。取得はしませんが、<c>promete.js</c> が中のフォントを登録します。
    /// </summary>
    public const string KindEmbeddedPackage = "embedded-package";

    /// <summary>
    /// マニフェストの中身を解析します。
    /// </summary>
    /// <param name="json">マニフェストの中身 (UTF-8)。先頭の BOM は読み飛ばします。</param>
    /// <param name="description">例外のメッセージに使う、マニフェストの説明。</param>
    /// <returns>エントリの一覧 (書かれた順)。</returns>
    /// <exception cref="InvalidDataException">JSON として正しくない場合、必須のプロパティが無い場合、型が違う場合。</exception>
    /// <exception cref="NotSupportedException"><c>version</c> が 1 以外の場合。</exception>
    public static IReadOnlyList<WebAssetManifestEntry> Parse(
        ReadOnlySpan<byte> json,
        string description
    )
    {
        throw new NotImplementedException();
    }
}
