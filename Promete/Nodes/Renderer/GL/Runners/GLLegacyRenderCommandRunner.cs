using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="LegacyRenderCommand"/> で旧 <see cref="NodeRendererBase.Render"/> を呼び出すランナーです。
/// </summary>
public class GLLegacyRenderCommandRunner : CommandRunner<LegacyRenderCommand>
{
    public override void Execute(LegacyRenderCommand command)
    {
#pragma warning disable CS0618
        command.Renderer.Render(command.Node);
#pragma warning restore CS0618
    }
}
