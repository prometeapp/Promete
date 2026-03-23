using System;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="EndMaskCommand"/> でステンシルマスクの後処理を行うランナーです。
/// </summary>
public class GLEndMaskCommandRunner(IGameView view, GLRenderState state) : CommandRunner<EndMaskCommand>
{
    private readonly OpenGLDesktopGameView _view = (OpenGLDesktopGameView)view;

    public override void Execute(EndMaskCommand command)
    {
        var gl = _view.GL;
        gl.StencilMask(0xFF);
        var wasEnabled = state.StencilStateStack.TryPop(out var prev) && prev;
        if (!wasEnabled)
            gl.Disable(GLEnum.StencilTest);
        gl.ClearStencil(0);
        gl.Clear(ClearBufferMask.StencilBufferBit);
    }
}
