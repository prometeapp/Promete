using Promete.Graphics;
using Promete.Nodes.Renderer;
using Promete.Nodes.Renderer.GL;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Nodes.Renderer.GL.Runners;
using Promete.Windowing.GLDesktop;

namespace Promete.GLDesktop;

/// <summary>
/// OpenGLを使用したデスクトップアプリケーション用の拡張機能を提供するクラスです。
/// </summary>
public static class OpenGLDesktopAppExtension
{
    /// <summary>
    /// PrometeAppをOpenGL デスクトップアプリケーションとして構築します。
    /// </summary>
    /// <param name="builder">PrometeAppのビルダー</param>
    /// <returns>構築されたPrometeAppインスタンス</returns>
    public static PrometeApp BuildWithOpenGLDesktop(this PrometeApp.PrometeAppBuilder builder)
    {
        var app = builder
            .Use<IFrameBufferProvider, GLFrameBufferProvider>()
            .Use<GLTextureRendererHelper>()
            .Use<GLPieSpriteRendererHelper>()
            .Use<GLPrimitiveRendererHelper>()
            .Use<GLMaskedContainerHelper>()
            .Use<GLBatchTextureRenderer>()
            .Use<GLRenderState>()
            .Use<RenderCommandQueue>()
            // GL CommandRunner 群
            .Use<GLDrawTextureBatchedCommandRunner>()
            .Use<GLDrawPrimitiveCommandRunner>()
            .Use<GLBeginTrimCommandRunner>()
            .Use<GLEndTrimCommandRunner>()
            .Use<GLBeginStencilMaskCommandRunner>()
            .Use<GLBeginAlphaMaskCommandRunner>()
            .Use<GLEndMaskCommandRunner>()
            .Use<GLDrawPieTextureCommandRunner>()
            .Build<OpenGLDesktopWindow>();

        // ビルド後にランナーをキューへ一括紐付け
        app.GetPlugin<RenderCommandQueue>().RegisterRunnerRange(
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
