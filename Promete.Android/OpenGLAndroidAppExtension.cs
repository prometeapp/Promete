using Promete.Elements;
using Promete.Elements.Renderer.GL;
using Promete.Elements.Renderer.GL.Helper;
using Promete.Windowing.GLDesktop;

namespace Promete.Android;

public static class OpenGLAndroidAppExtension
{
	public static PrometeApp BuildWithAndroid(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder
			.UseRenderer<Container, GLContainbleElementRenderer>()
			.UseRenderer<NineSliceSprite, GLNineSliceSpriteRenderer>()
			.UseRenderer<Shape, GLShapeRenderer>()
			.UseRenderer<Sprite, GLSpriteRenderer>()
			.UseRenderer<Text, GLTextRenderer>()
			.UseRenderer<Tilemap, GLTilemapRenderer>()
			.Use<GLTextureRendererHelper>()
			.Use<GLPrimitiveRendererHelper>()
			.Build<OpenGLAndroidWindow>();
	}
}
