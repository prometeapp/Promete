using System;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="EndTrimCommand"/> でシザーテスト状態を復元するランナーです。
/// </summary>
public class GLEndTrimCommandRunner(IGameView view) : CommandRunner<EndTrimCommand>
{
    private readonly OpenGLDesktopGameView _view = (OpenGLDesktopGameView)view;

    public override void Execute(EndTrimCommand command)
    {
        var gl = _view.GL;
        gl.Scissor(command.X, command.Y, (uint)command.Width, (uint)command.Height);
        if (command.WasEnabled)
            gl.Enable(GLEnum.ScissorTest);
        else
            gl.Disable(GLEnum.ScissorTest);
    }
}
