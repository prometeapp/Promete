using Promete.Elements;
using Promete.Elements.Renderer.GL;
using Promete.Elements.Renderer.GL.Helper;
using Promete.Windowing.GLDesktop;

namespace Promete.GLDesktop;

public static class OpenGLDesktopAppExtension
{
	public static PrometeApp BuildWithOpenGLDesktop(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder
			.UseRenderer<ContainableElementBase, GLContainbleElementRenderer>()
			.UseRenderer<Container, GLContainbleElementRenderer>()
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
