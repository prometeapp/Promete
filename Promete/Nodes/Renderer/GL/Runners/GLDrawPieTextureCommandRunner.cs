using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="DrawPieTextureCommand"/> を実行するランナーです。
/// </summary>
public class GLDrawPieTextureCommandRunner(GLPieSpriteRendererHelper helper) : CommandRunner<DrawPieTextureCommand>
{
    public override void Execute(DrawPieTextureCommand command)
    {
        helper.Draw(command);
    }
}
