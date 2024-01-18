using Promete.Elements.Renderer.GL.Helper;

namespace Promete.Elements.Renderer.GL;

public class GLSpriteRenderer(GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var sprite = (Sprite)element;
		if (sprite.Texture is not { } tex) return;
		helper.Draw(tex, sprite.AbsoluteLocation, sprite.AbsoluteScale, sprite.TintColor, sprite.Width, sprite.Height);
	}
}
