using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.GL;
using Promete.Graphics.Rendering.GL.Runners;
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
        var app = builder
            .Use<GLMaskedContainerHelper>()
            .Use<GLRenderState>()
            .Use<GLDrawTextureBatchedCommandRunner>()
            .Use<GLDrawPrimitiveCommandRunner>()
            .Use<GLBeginTrimCommandRunner>()
            .Use<GLEndTrimCommandRunner>()
            .Use<GLBeginStencilMaskCommandRunner>()
            .Use<GLBeginAlphaMaskCommandRunner>()
            .Use<GLEndMaskCommandRunner>()
            .Use<GLDrawPieTextureCommandRunner>()
            .Build<WebBackend>(opts);

        app.GetPlugin<RenderCommandQueue>()
            .RegisterRunnerRange(
                app.GetPlugin<GLDrawTextureBatchedCommandRunner>(),
                app.GetPlugin<GLDrawPrimitiveCommandRunner>(),
                app.GetPlugin<GLBeginTrimCommandRunner>(),
                app.GetPlugin<GLEndTrimCommandRunner>(),
                app.GetPlugin<GLBeginStencilMaskCommandRunner>(),
                app.GetPlugin<GLBeginAlphaMaskCommandRunner>(),
                app.GetPlugin<GLEndMaskCommandRunner>(),
                app.GetPlugin<GLDrawPieTextureCommandRunner>()
            );

        return app;
    }
}
