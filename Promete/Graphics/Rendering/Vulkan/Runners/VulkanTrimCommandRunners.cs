using System;
using Promete.Graphics.Rendering.Commands;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="BeginTrimCommand"/> でシザー領域を設定するランナーです。
/// コマンドの座標は左上原点のため、Vulkan ではそのまま使用できます。
/// </summary>
internal sealed class VulkanBeginTrimCommandRunner(VulkanContext ctx)
    : CommandRunner<BeginTrimCommand>
{
    public override void Execute(BeginTrimCommand command)
    {
        if (!ctx.IsFrameActive)
            return;
        ctx.SetTrimScissor(ToScissor(command.X, command.Y, command.Width, command.Height));
    }

    internal static Rect2D ToScissor(int x, int y, int width, int height)
    {
        return new Rect2D(
            new Offset2D(Math.Max(0, x), Math.Max(0, y)),
            new Extent2D((uint)Math.Max(0, width), (uint)Math.Max(0, height))
        );
    }
}

/// <summary>
/// <see cref="EndTrimCommand"/> でシザー領域を復元するランナーです。
/// </summary>
internal sealed class VulkanEndTrimCommandRunner(VulkanContext ctx) : CommandRunner<EndTrimCommand>
{
    public override void Execute(EndTrimCommand command)
    {
        if (!ctx.IsFrameActive)
            return;

        ctx.SetTrimScissor(
            command.WasEnabled
                ? VulkanBeginTrimCommandRunner.ToScissor(
                    command.X,
                    command.Y,
                    command.Width,
                    command.Height
                )
                : null
        );
    }
}
