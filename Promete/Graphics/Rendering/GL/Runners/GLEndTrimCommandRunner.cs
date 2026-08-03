using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics.Rendering.Commands;
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

        // トリムが無効に戻る場合、コマンドはゼロ埋めのセンチネル。
        // 反転すると範囲外の原点になるため、Scissor は発行せず無効化のみ行う
        if (!command.WasEnabled)
        {
            gl.Disable(GLEnum.ScissorTest);
            return;
        }

        // コマンドの座標は左上原点。GL の Scissor は左下原点なので Y を反転する
        var viewportHeight = GLHelper.GetViewport(gl).Y;
        var flippedY = viewportHeight - command.Y - command.Height;

        gl.Scissor(command.X, flippedY, (uint)command.Width, (uint)command.Height);
        gl.Enable(GLEnum.ScissorTest);
    }
}
