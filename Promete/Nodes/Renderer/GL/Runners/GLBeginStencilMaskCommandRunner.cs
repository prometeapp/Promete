using System;
using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="BeginStencilMaskCommand"/> でステンシルマスクの書き込みフェーズを開始するランナーです。
/// </summary>
public class GLBeginStencilMaskCommandRunner(IWindow window, GLMaskedContainerHelper maskHelper, GLRenderState state)
    : CommandRunner<BeginStencilMaskCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow
        ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

    public override void Execute(BeginStencilMaskCommand command)
    {
        var gl = _window.GL;

        state.StencilStateStack.Push(gl.IsEnabled(GLEnum.StencilTest));

        gl.Enable(GLEnum.StencilTest);
        gl.ClearStencil(0);
        gl.Clear(ClearBufferMask.StencilBufferBit);
        gl.StencilFunc(GLEnum.Always, 1, 0xFF);
        gl.StencilOp(GLEnum.Keep, GLEnum.Keep, GLEnum.Replace);
        gl.StencilMask(0xFF);
        gl.ColorMask(false, false, false, false);
        maskHelper.DrawMaskToStencil(command.MaskTexture, command.Container);
        gl.ColorMask(true, true, true, true);
        gl.StencilFunc(GLEnum.Equal, 1, 0xFF);
        gl.StencilMask(0x00);
    }
}
