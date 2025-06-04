using System;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL;

public class GLContainbleNodeRenderer(PrometeApp app, IWindow window) : NodeRendererBase
{
    public override void Render(Node node)
    {
        if (node.IsDestroyed) return;
        var container = (ContainableNode)node;
        var w = window as OpenGLDesktopWindow ??
                throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

        if (container.isTrimmable) TrimStart(container, w.GL);
        var sorted = container.sortedChildren.AsSpan();
        foreach (var t in sorted) app.RenderNode(t);
        if (container.isTrimmable) TrimEnd(w.GL);
    }

    private void TrimStart(ContainableNode node, Silk.NET.OpenGL.GL gl)
    {
        app.ThrowIfNotMainThread();
        gl.Enable(GLEnum.ScissorTest);
        var left = (VectorInt)node.AbsoluteLocation;
        var size = (VectorInt)(node.Size * node.AbsoluteScale);

        if (left.X < 0) left.X = 0;
        if (left.Y < 0) left.Y = 0;

        if (left.X + size.X > window.ActualWidth)
            size.X = left.X + size.X - window.ActualWidth;

        if (left.Y + size.Y > window.ActualHeight)
            size.Y = left.Y + size.Y - window.ActualHeight;

        left.Y = window.ActualHeight - left.Y - size.Y;

        gl.Scissor(left.X, left.Y, (uint)size.X, (uint)size.Y);
    }

    private void TrimEnd(Silk.NET.OpenGL.GL gl)
    {
        app.ThrowIfNotMainThread();
        gl.Scissor(0, 0, (uint)window.ActualWidth, (uint)window.ActualHeight);
        gl.Disable(GLEnum.ScissorTest);
    }
}
