using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLTextRenderer(GLTextureRendererHelper helper) : NodeRendererBase
{
	public override void Render(Node node)
	{
		var text = (Text)node;
		var texture = text.RenderedTexture;
		if (texture == null) return;
		helper.Draw(texture.Value, text);
	}
}
