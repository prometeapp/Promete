using System;
using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="EndMaskCommand"/> でステンシルマスクの後処理を行うランナーです。
/// </summary>
public class GLEndMaskCommandRunner(IWindow window, GLRenderState state) : CommandRunner<EndMaskCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow
        ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

    public override void Execute(EndMaskCommand command)
    {
        var gl = _window.GL;
        gl.StencilMask(0xFF);
        var wasEnabled = state.StencilStateStack.TryPop(out var prev) && prev;
        if (!wasEnabled)
            gl.Disable(GLEnum.StencilTest);
        gl.ClearStencil(0);
        gl.Clear(ClearBufferMask.StencilBufferBit);
    }
}
