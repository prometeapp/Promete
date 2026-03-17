using System;
using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="EndScissorCommand"/> でシザーテスト状態を復元するランナーです。
/// </summary>
public class GLEndScissorCommandRunner(IWindow window) : CommandRunner<EndScissorCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow
        ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

    public override void Execute(EndScissorCommand command)
    {
        var gl = _window.GL;
        gl.Scissor(command.X, command.Y, (uint)command.Width, (uint)command.Height);
        if (command.WasEnabled)
            gl.Enable(GLEnum.ScissorTest);
        else
            gl.Disable(GLEnum.ScissorTest);
    }
}
