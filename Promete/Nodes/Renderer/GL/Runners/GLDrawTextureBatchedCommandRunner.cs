using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="DrawTextureBatchedCommand"/> をインスタンシングで描画するランナーです。
/// </summary>
public class GLDrawTextureBatchedCommandRunner(GLBatchTextureRenderer batchRenderer)
    : CommandRunner<DrawTextureBatchedCommand>
{
    public override void Execute(DrawTextureBatchedCommand command)
    {
        batchRenderer.DrawInstanced(command.Items);
    }
}
