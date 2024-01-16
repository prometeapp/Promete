using Promete.Elements.Renderer.GL.Helper;

namespace Promete.Elements.Renderer.GL;

public class GLTextRenderer(GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var text = (Text)element;
		helper.Draw(text.RenderedTexture, text.AbsoluteLocation, text.AbsoluteScale);
	}
}
