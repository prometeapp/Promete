using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.GL;
using Promete.Graphics.Rendering.GL.Runners;
using Promete.Windowing;

namespace Promete.Backends.GL;

/// <summary>
/// <see cref="GLBackendBase"/> を用いて <see cref="PrometeApp"/> を構築する拡張機能を提供するクラスです。
/// </summary>
public static class GLBackendAppExtension
{
    /// <summary>
    /// GL の描画に必要なプラグインとランナーを登録し、指定した GL バックエンドで <see cref="PrometeApp"/> を構築します。
    /// </summary>
    /// <typeparam name="TBackend">使用する GL バックエンド。</typeparam>
    /// <param name="builder">PrometeApp のビルダー。</param>
    /// <param name="opts">ウィンドウの設定。</param>
    /// <returns>構築された PrometeApp インスタンス。</returns>
    public static PrometeApp BuildWithGLBackend<TBackend>(
        this PrometeApp.PrometeAppBuilder builder,
        WindowOptions? opts = null
    )
        where TBackend : GLBackendBase, new()
    {
        var app = builder
            .Use<GLMaskedContainerHelper>()
            .Use<GLRenderState>()
            // GL CommandRunner 群
            .Use<GLDrawTextureBatchedCommandRunner>()
            .Use<GLDrawPrimitiveCommandRunner>()
            .Use<GLBeginTrimCommandRunner>()
            .Use<GLEndTrimCommandRunner>()
            .Use<GLBeginStencilMaskCommandRunner>()
            .Use<GLBeginAlphaMaskCommandRunner>()
            .Use<GLEndMaskCommandRunner>()
            .Use<GLDrawPieTextureCommandRunner>()
            .Build<TBackend>(opts);

        // ビルド後にランナーをキューへ一括紐付け
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
