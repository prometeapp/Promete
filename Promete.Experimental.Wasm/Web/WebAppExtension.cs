using Promete.Backends.GL;
using Promete.Windowing;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// Promete をブラウザ (WebGL2) 上で動かすための拡張です。
/// </summary>
public static class WebAppExtension
{
    /// <summary>
    /// <see cref="PrometeApp"/> をブラウザ向けに構築します。
    /// </summary>
    /// <param name="builder">PrometeApp のビルダー。</param>
    /// <param name="opts">ウィンドウオプション。</param>
    /// <returns>構築された <see cref="PrometeApp"/>。</returns>
    public static PrometeApp BuildWithWeb(
        this PrometeApp.PrometeAppBuilder builder,
        WindowOptions? opts = null
    )
    {
        return builder.BuildWithGLBackend<WebBackend>(opts);
    }
}
