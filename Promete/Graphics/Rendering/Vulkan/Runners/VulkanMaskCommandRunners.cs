using Promete.Graphics.Rendering.Commands;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="BeginStencilMaskCommand"/> でステンシルマスクの書き込みフェーズを開始するランナーです。
/// </summary>
internal sealed class VulkanBeginStencilMaskCommandRunner(
    VulkanContext ctx,
    VulkanMaskedContainerHelper maskHelper
) : CommandRunner<BeginStencilMaskCommand>
{
    public override void Execute(BeginStencilMaskCommand command)
    {
        if (!ctx.IsFrameActive)
            return;

        // ステンシルをクリアし、マスク形状を書き込む
        ctx.ClearStencil();
        maskHelper.DrawMaskToStencil(command.MaskTexture, command.Container);

        // 以降の描画はステンシルテスト (Equal, ref=1) 付きパイプラインを使用する
        ctx.StencilMaskActive = true;
    }
}

/// <summary>
/// <see cref="BeginAlphaMaskCommand"/> でアルファマスク合成を行うランナーです。
/// </summary>
internal sealed class VulkanBeginAlphaMaskCommandRunner(VulkanMaskedContainerHelper maskHelper)
    : CommandRunner<BeginAlphaMaskCommand>
{
    public override void Execute(BeginAlphaMaskCommand command)
    {
        var contentTexture = maskHelper.RenderToTexture(command.Container, command.Context);
        maskHelper.DrawMasked(contentTexture, command.MaskTexture, command.Container);
    }
}

/// <summary>
/// <see cref="EndMaskCommand"/> でステンシルマスクを終了するランナーです。
/// </summary>
internal sealed class VulkanEndMaskCommandRunner(VulkanContext ctx) : CommandRunner<EndMaskCommand>
{
    public override void Execute(EndMaskCommand command)
    {
        if (!ctx.IsFrameActive)
            return;

        ctx.StencilMaskActive = false;
        ctx.ClearStencil();
    }
}
