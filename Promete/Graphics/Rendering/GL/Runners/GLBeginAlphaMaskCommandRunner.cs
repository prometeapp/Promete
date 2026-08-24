using Promete.Graphics.Rendering.Commands;

namespace Promete.Graphics.Rendering.GL.Runners;

/// <summary>
/// <see cref="BeginAlphaMaskCommand"/> でアルファマスク合成を行うランナーです。
/// </summary>
public class GLBeginAlphaMaskCommandRunner(GLMaskedContainerHelper maskHelper)
    : CommandRunner<BeginAlphaMaskCommand>
{
    public override void Execute(BeginAlphaMaskCommand command)
    {
        var contentTexture = maskHelper.RenderToTexture(command.Container, command.Context);
        maskHelper.DrawMasked(contentTexture, command.MaskTexture, command.Container);
    }
}
