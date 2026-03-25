using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="BeginStencilMaskCommand"/> でステンシルマスクの書き込みフェーズを開始するランナーです。
/// </summary>
public class GLBeginStencilMaskCommandRunner(IGameView view, GLMaskedContainerHelper maskHelper, GLRenderState state)
    : CommandRunner<BeginStencilMaskCommand>
{
    private readonly OpenGLDesktopGameView _view = (OpenGLDesktopGameView)view;

    public override void Execute(BeginStencilMaskCommand command)
    {
        var gl = _view.GL;

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
