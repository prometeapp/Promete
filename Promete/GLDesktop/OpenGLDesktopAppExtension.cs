using Promete.Nodes;
using Promete.Nodes.Renderer.GL;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing.GLDesktop;

namespace Promete.GLDesktop;

public static class OpenGLDesktopAppExtension
{
	public static PrometeApp BuildWithOpenGLDesktop(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder
			.UseRenderer<ContainableNode, GLContainbleNodeRenderer>()
			.UseRenderer<Container, GLContainbleNodeRenderer>()
			.UseRenderer<NineSliceSprite, GLNineSliceSpriteRenderer>()
			.UseRenderer<Shape, GLShapeRenderer>()
			.UseRenderer<Sprite, GLSpriteRenderer>()
			.UseRenderer<Text, GLTextRenderer>()
			.UseRenderer<Tilemap, GLTilemapRenderer>()
			.Use<GLTextureRendererHelper>()
			.Use<GLPrimitiveRendererHelper>()
			.Build<OpenGLDesktopWindow>();
	}
}
