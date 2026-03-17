using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer.GL;

public class GLShapeRenderer : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        var shape = (Shape)node;

        queue.Enqueue(new DrawPrimitiveCommand
        {
            Node = shape,
            WorldVertices = shape.Vertices,
            ShapeType = shape.Type,
            Color = shape.Color,
            LineWidth = shape.LineWidth,
            LineColor = shape.LineColor,
        });
    }
}
