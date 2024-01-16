using Promete.Elements.Renderer.GL.Helper;

namespace Promete.Elements.Renderer.GL;

public class GLShapeRenderer(GLPrimitiveRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var el = (Shape)element;
		helper.Draw(el.AbsoluteLocation, el.AbsoluteScale, el.Vertices, el.Type, el.Color, el.LineWidth, el.LineColor);
	}
}
