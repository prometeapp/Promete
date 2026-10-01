using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
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

        // コマンドの座標は左上原点。GL の Scissor は左下原点なので Y を反転する
        var viewportHeight = GLHelper.GetViewport(gl).Y;
        var flippedY = viewportHeight - command.Y - command.Height;

        gl.Enable(GLEnum.ScissorTest);
        gl.Scissor(command.X, flippedY, (uint)command.Width, (uint)command.Height);
    }
}
