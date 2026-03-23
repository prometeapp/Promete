using System;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="BeginTrimCommand"/> でシザーテストを開始するランナーです。
/// </summary>
public class GLBeginTrimCommandRunner(IGameView view) : CommandRunner<BeginTrimCommand>
{
    private readonly OpenGLDesktopGameView _view = (OpenGLDesktopGameView)view;

    public override void Execute(BeginTrimCommand command)
    {
        var gl = _view.GL;
        gl.Enable(GLEnum.ScissorTest);
        gl.Scissor(command.X, command.Y, (uint)command.Width, (uint)command.Height);
    }
}
