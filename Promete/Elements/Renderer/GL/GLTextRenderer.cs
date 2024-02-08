using Promete.Elements.Renderer.GL.Helper;

namespace Promete.Elements.Renderer.GL;

public class GLTextRenderer(GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var text = (Text)element;
		var texture = text.RenderedTexture;
		if (texture == null) return;
		helper.Draw(texture, text);
	}
}
