using Promete.Graphics;
using Promete.Nodes;
using Promete.Nodes.Renderer.GL;
using Promete.Nodes.Renderer.GL.Helper;
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
        return builder
            .UseRenderer<ContainableNode, GLContainbleNodeRenderer>()
            .UseRenderer<Container, GLContainbleNodeRenderer>()
            .UseRenderer<MaskedContainer, GLMaskedContainerRenderer>()
            .UseRenderer<NineSliceSprite, GLNineSliceSpriteRenderer>()
            .UseRenderer<Shape, GLShapeRenderer>()
            .UseRenderer<Sprite, GLSpriteRenderer>()
            .UseRenderer<Text, GLTextRenderer>()
            .UseRenderer<Tilemap, GLTilemapRenderer>()
            .Use<IFrameBufferProvider, GLFrameBufferProvider>()
            .Use<GLTextureRendererHelper>()
            .Use<GLPrimitiveRendererHelper>()
            .Use<GLMaskedContainerHelper>()
            .Build<OpenGLDesktopWindow>();
    }
}
