using System;
using System.Collections.Generic;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL;

public class GLContainbleNodeRenderer(PrometeApp app, IWindow window) : NodeRendererBase
{
    private readonly record struct ScissorState(int X, int Y, int Width, int Height, bool Enabled);
    private readonly Stack<ScissorState> _scissorStack = new();
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

    protected void TrimStart(ContainableNode node, Silk.NET.OpenGL.GL gl)
    {
        app.ThrowIfNotMainThread();

        // 現在の Scissor 状態をスタックに保存
        Span<int> scissorBox = stackalloc int[4];
        gl.GetInteger(GetPName.ScissorBox, scissorBox);
        var wasEnabled = gl.IsEnabled(GLEnum.ScissorTest);
        _scissorStack.Push(new ScissorState(scissorBox[0], scissorBox[1], scissorBox[2], scissorBox[3], wasEnabled));

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

        var scissorX = left.X * window.Scale;
        var scissorY = left.Y * window.Scale;
        var scissorW = size.X * window.Scale;
        var scissorH = size.Y * window.Scale;

        // 親の Scissor が有効の場合、矩形の積集合を計算して親の範囲内に制限する
        if (wasEnabled)
        {
            var right = Math.Min(scissorX + scissorW, scissorBox[0] + scissorBox[2]);
            var top = Math.Min(scissorY + scissorH, scissorBox[1] + scissorBox[3]);
            scissorX = Math.Max(scissorX, scissorBox[0]);
            scissorY = Math.Max(scissorY, scissorBox[1]);
            scissorW = Math.Max(0, right - scissorX);
            scissorH = Math.Max(0, top - scissorY);
        }

        gl.Scissor(scissorX, scissorY, (uint)scissorW, (uint)scissorH);
    }

    protected void TrimEnd(Silk.NET.OpenGL.GL gl)
    {
        app.ThrowIfNotMainThread();

        // スタックから直前の Scissor 状態を復元
        var prev = _scissorStack.Pop();
        gl.Scissor(prev.X, prev.Y, (uint)prev.Width, (uint)prev.Height);
        if (prev.Enabled)
            gl.Enable(GLEnum.ScissorTest);
        else
            gl.Disable(GLEnum.ScissorTest);
    }
}
