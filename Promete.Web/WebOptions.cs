namespace Promete.Web;

/// <summary>
/// ブラウザ向けの設定です。
/// </summary>
public sealed record WebOptions
{
    /// <summary>
    /// 既定の設定を取得します。
    /// </summary>
    public static WebOptions Default { get; } = new();

    /// <summary>
    /// 描画先の canvas を指す CSS セレクタを取得します。既定は <c>#canvas</c> です。
    /// </summary>
    public string CanvasSelector { get; init; } = "#canvas";
}
