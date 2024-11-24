using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLShapeRenderer(GLPrimitiveRendererHelper helper) : NodeRendererBase
{
    public override void Render(Node node)
    {
        var shape = (Shape)node;
        helper.Draw(shape, shape.Vertices, shape.Type, shape.Color, shape.LineWidth, shape.LineColor);
    }
}
