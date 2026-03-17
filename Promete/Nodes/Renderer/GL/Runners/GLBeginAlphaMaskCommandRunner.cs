using Promete.Nodes.Renderer.Commands;
using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL.Runners;

/// <summary>
/// <see cref="BeginAlphaMaskCommand"/> でアルファマスク合成を行うランナーです。
/// </summary>
public class GLBeginAlphaMaskCommandRunner(GLMaskedContainerHelper maskHelper)
    : CommandRunner<BeginAlphaMaskCommand>
{
    public override void Execute(BeginAlphaMaskCommand command)
    {
        var contentTexture = maskHelper.RenderToTexture(command.Container);
        maskHelper.DrawMasked(contentTexture, command.MaskTexture, command.Container);
    }
}
