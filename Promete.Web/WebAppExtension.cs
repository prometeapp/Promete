using Promete.Backends.GL;
using Promete.Windowing;

namespace Promete.Web;

/// <summary>
/// Promete をブラウザ (WebGL2) 上で動かすための拡張です。
/// </summary>
public static class WebAppExtension
{
    /// <summary>
    /// <see cref="PrometeApp"/> をブラウザ向けに構築します。
    /// 呼び出す前に <see cref="PrometeWeb.InitializeAsync"/> を完了させてください。
    /// </summary>
    /// <param name="builder">PrometeApp のビルダー。</param>
    /// <param name="opts">ウィンドウオプション。</param>
    /// <param name="web">ブラウザ向けの設定。</param>
    /// <returns>構築された <see cref="PrometeApp"/>。</returns>
    public static PrometeApp BuildWithWeb(
        this PrometeApp.PrometeAppBuilder builder,
        WindowOptions? opts = null,
        WebOptions? web = null
    )
    {
        WebBackend.PendingOptions = web ?? WebOptions.Default;
        return builder.BuildWithGLBackend<WebBackend>(opts);
    }
}
