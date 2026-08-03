using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="DrawPrimitiveCommand"/> でプリミティブ図形を描画するランナーです。
/// </summary>
internal sealed unsafe class VulkanDrawPrimitiveCommandRunner(
    VulkanContext ctx,
    VulkanPipelineProvider pipelines,
    VulkanShaderManager shaders,
    VulkanMaterialSystem materials
) : CommandRunner<DrawPrimitiveCommand>
{
    public override void Execute(DrawPrimitiveCommand command)
    {
        Draw(
            command.WorldVertices,
            command.ShapeType,
            command.Color,
            command.LineWidth,
            command.LineColor,
            command.Material
        );
    }

    private void Draw(
        Span<Vector> worldVertices,
        ShapeType type,
        Color color,
        int lineWidth,
        Color? lineColor,
        Material? material
    )
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (worldVertices.Length == 0 || !ctx.IsFrameActive)
            return;

        var extent = ctx.CurrentTargetExtent;
        var halfWidth = extent.Width / 2f;
        var halfHeight = extent.Height / 2f;

        // ワールド座標 → Vulkan NDC (Y 下向きのため、GL 向け変換の Y を反転)
        Span<float> vertices = stackalloc float[worldVertices.Length * 2];
        for (var i = 0; i < worldVertices.Length; i++)
        {
            var (x, y) = worldVertices[i].ToViewportPoint(halfWidth, halfHeight);
            vertices[(i * 2) + 0] = x;
            vertices[(i * 2) + 1] = -y;
        }

        DrawFill(vertices, type, color, lineWidth, material);
        DrawStroke(vertices, lineWidth, lineColor, material);
    }

    private void DrawFill(
        Span<float> vertices,
        ShapeType type,
        Color color,
        int lineWidth,
        Material? material
    )
    {
        // 透明度が0の場合は、塗りつぶし領域の描画をスキップする
        if (color.A <= 0)
            return;

        _ = lineWidth; // Vulkan では線幅 1 のみサポート (wideLines 未使用)

        var vk = ctx.Vk;
        var cmd = ctx.CurrentCommandBuffer;
        var vertexCount = (uint)(vertices.Length / 2);

        var (buffer, offset) = ctx.CurrentArena.Push<float>(vertices);
        BindPipeline(cmd, ToVulkanTopology(type), color, material);

        vk.CmdBindVertexBuffers(cmd, 0, 1, in buffer, in offset);

        // 矩形はインデックスを利用して 2 トライアングルで描画する
        if (type == ShapeType.Rect)
        {
            Span<uint> indices = [0, 1, 2, 0, 2, 3];
            var (indexBuffer, indexOffset) = ctx.CurrentArena.Push<uint>(indices);
            vk.CmdBindIndexBuffer(cmd, indexBuffer, indexOffset, IndexType.Uint32);
            vk.CmdDrawIndexed(cmd, 6, 1, 0, 0, 0);
            return;
        }

        vk.CmdDraw(cmd, vertexCount, 1, 0, 0);
    }

    private void DrawStroke(Span<float> vertices, int lineWidth, Color? lineColor, Material? material)
    {
        if (lineWidth <= 0 || lineColor is not { } lc)
            return;

        var vk = ctx.Vk;
        var cmd = ctx.CurrentCommandBuffer;

        // LineLoop は Vulkan に無いため、先頭頂点を末尾に足して LineStrip で閉じる
        Span<float> looped = stackalloc float[vertices.Length + 2];
        vertices.CopyTo(looped);
        looped[^2] = vertices[0];
        looped[^1] = vertices[1];

        var (buffer, offset) = ctx.CurrentArena.Push<float>(looped);
        BindPipeline(cmd, PrimitiveTopology.LineStrip, lc, material);

        vk.CmdBindVertexBuffers(cmd, 0, 1, in buffer, in offset);
        vk.CmdDraw(cmd, (uint)(looped.Length / 2), 1, 0, 0);
    }

    /// <summary>
    /// パイプラインをバインドし、色の push constant とマテリアルを適用します。
    /// </summary>
    private void BindPipeline(
        CommandBuffer cmd,
        PrimitiveTopology topology,
        Color color,
        Material? material
    )
    {
        var vk = ctx.Vk;
        var stencil = CurrentStencilMode();
        var useCustom = material is not null && shaders.Contains(material.Shader.Handle);

        var pipeline = useCustom
            ? pipelines.GetCustomPrimitivePipeline(
                material!.Shader.Handle,
                VulkanPipelineProvider.PassClass.Offscreen,
                topology,
                stencil
            )
            : pipelines.GetPrimitivePipeline(
                VulkanPipelineProvider.PassClass.Offscreen,
                topology,
                stencil
            );
        var layout = useCustom
            ? pipelines.GetCustomLayout(
                material!.Shader.Handle,
                VulkanPipelineProvider.CustomKind.Primitive
            )
            : pipelines.PrimitiveLayout;

        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);

        var value = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        vk.CmdPushConstants(cmd, layout, ShaderStageFlags.FragmentBit, 0, 16, &value);

        if (useCustom)
            materials.Apply(cmd, material!, layout);
    }

    private VulkanPipelineProvider.StencilMode CurrentStencilMode() =>
        ctx.StencilMaskActive
            ? VulkanPipelineProvider.StencilMode.TestEqual
            : VulkanPipelineProvider.StencilMode.None;

    /// <summary>
    /// Prometeの<see cref="ShapeType"/>を、Vulkanの<see cref="PrimitiveTopology"/>に変換します。
    /// </summary>
    private static PrimitiveTopology ToVulkanTopology(ShapeType type)
    {
        return type switch
        {
            ShapeType.Pixel => PrimitiveTopology.PointList,
            ShapeType.Line => PrimitiveTopology.LineList,
            ShapeType.Rect => PrimitiveTopology.TriangleList,
            ShapeType.Triangle => PrimitiveTopology.TriangleList,
            ShapeType.Polygon => PrimitiveTopology.TriangleStrip,
            _ => throw new ArgumentException(null, nameof(type)),
        };
    }
}
