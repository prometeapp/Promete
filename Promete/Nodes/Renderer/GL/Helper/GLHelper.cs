using System;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Helper;

public static class GLHelper
{
    public static VectorInt GetViewport(Silk.NET.OpenGL.GL gl)
    {
        Span<int> viewport = stackalloc int[4];
        gl.GetInteger(GetPName.Viewport, viewport);
        return new VectorInt(viewport[2], viewport[3]);
    }
}
