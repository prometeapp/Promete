using System;
using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="BeginScissorCommand"/> でシザーテストを開始するランナーです。
/// </summary>
public class GLBeginScissorCommandRunner(IWindow window) : CommandRunner<BeginScissorCommand>
{
    private readonly OpenGLDesktopWindow _window = window as OpenGLDesktopWindow
        ?? throw new InvalidOperationException("Window is not a OpenGLDesktopWindow");

    public override void Execute(BeginScissorCommand command)
    {
        var gl = _window.GL;
        gl.Enable(GLEnum.ScissorTest);
        gl.Scissor(command.X, command.Y, (uint)command.Width, (uint)command.Height);
    }
}
