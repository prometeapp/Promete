namespace Promete.Web.Assets;

/// <summary>
/// Web 形式のマニフェスト (<c>promete-assets.json</c>) の 1 つのエントリです。
/// </summary>
/// <param name="Kind">エントリの種類 (<see cref="WebAssetManifest.KindFile"/> など)。知らない種類もそのまま保持します。</param>
/// <param name="Path">仮想ファイルシステム (MEMFS) 上の絶対パス (<c>/</c> 始まり)。<c>embedded-package</c> では <see langword="null"/>。</param>
/// <param name="LogicalPath">論理パス。<c>file</c> と <c>font</c> だけが持ちます。</param>
/// <param name="Package">パッケージ名。<c>package</c> と <c>embedded-package</c> だけが持ちます。</param>
/// <param name="Assembly">埋め込みリソースを持つアセンブリの名前。<c>embedded-package</c> だけが持ちます。</param>
/// <param name="Resource">埋め込みリソースの名前。<c>embedded-package</c> だけが持ちます。</param>
internal sealed record WebAssetManifestEntry(
    string Kind,
    string? Path,
    string? LogicalPath,
    string? Package,
    string? Assembly,
    string? Resource
);
