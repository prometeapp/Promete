using System;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer;

public static class RenderingHelper
{
    public static Vector Transform(Vector vertex, Node node, Vector? additionalLocation = null)
    {
        vertex = vertex
            .Translate(additionalLocation ?? (0, 0))
            .Rotate(MathHelper.ToRadian(node.Angle))
            .Scale(node.Scale)
            .Translate(node.Location);
        var parent = node.Parent;
        while (parent != null)
        {
            vertex = vertex
                .Rotate(MathHelper.ToRadian(parent.Angle))
                .Scale(parent.Scale)
                .Translate(parent.Location);
            parent = parent.Parent;
        }

        return vertex;
    }

    public static VectorInt GetViewport(Silk.NET.OpenGL.GL gl)
    {
        Span<int> viewport = stackalloc int[4];
        gl.GetInteger(GetPName.Viewport, viewport);
        return new VectorInt(viewport[2], viewport[3]);
    }
}
