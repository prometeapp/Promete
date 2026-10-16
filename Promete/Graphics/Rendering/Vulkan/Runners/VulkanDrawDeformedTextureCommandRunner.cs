using System;
using System.Numerics;
using Promete.Graphics.Rendering.Commands;
using Promete.Internal;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="DrawDeformedTextureCommand"/> で変形テクスチャを描画するランナーです。
/// </summary>
internal sealed unsafe class VulkanDrawDeformedTextureCommandRunner(
    VulkanContext ctx,
    VulkanResourceManager resources,
    VulkanPipelineProvider pipelines,
    VulkanShaderManager shaders,
    VulkanMaterialSystem materials
) : CommandRunner<DrawDeformedTextureCommand>
{
    // push constant: mat4 (16) + vec4 tint (4) = 20 floats
    private const int PushConstantFloats = 20;

    private bool _materialWarned;

    public override void Execute(DrawDeformedTextureCommand command)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        var texture = command.Texture;
        if (!ctx.IsFrameActive || !resources.Contains(texture.Handle))
            return;

        // カスタムマテリアル: シェーダーがコンパイル済みならカスタムパイプラインを使用
        var material = command.Material;
        var useCustom = material is not null && shaders.Contains(material.Shader.Handle);
        if (material is not null && !useCustom && !_materialWarned)
        {
            _materialWarned = true;
            LogHelper.Bug(
                "Material のシェーダーがコンパイルされていないため、デフォルトシェーダーで描画します。"
            );
        }

        Span<float> vertices = stackalloc float[DeformedQuadHelper.VertexFloats];
        Span<uint> indices = stackalloc uint[DeformedQuadHelper.IndexCount];
        DeformedQuadHelper.Build(command, vertices, indices);

        // 頂点はワールド座標なので、投影行列だけを掛ける
        var extent = ctx.CurrentTargetExtent;
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            extent.Width,
            0,
            extent.Height,
            -1f,
            1f
        );

        var color = command.TintColor;
        var push = stackalloc float[PushConstantFloats];
        *(Matrix4x4*)push = projection;
        push[16] = color.R / 255f;
        push[17] = color.G / 255f;
        push[18] = color.B / 255f;
        push[19] = color.A / 255f;

        var vk = ctx.Vk;
        var cmd = ctx.CurrentCommandBuffer;

        var stencil = ctx.StencilMaskActive
            ? VulkanPipelineProvider.StencilMode.TestEqual
            : VulkanPipelineProvider.StencilMode.None;
        var pipeline = useCustom
            ? pipelines.GetCustomDeformedPipeline(
                material!.Shader.Handle,
                VulkanPipelineProvider.PassClass.Offscreen,
                stencil
            )
            : pipelines.GetDeformedPipeline(VulkanPipelineProvider.PassClass.Offscreen, stencil);
        var layout = useCustom
            ? pipelines.GetCustomLayout(
                material!.Shader.Handle,
                VulkanPipelineProvider.CustomKind.Deformed
            )
            : pipelines.DeformedLayout;
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);
        vk.CmdPushConstants(
            cmd,
            layout,
            ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            0,
            PushConstantFloats * sizeof(float),
            push
        );

        var descriptorSet = resources.GetDescriptorSet(texture.Handle);
        vk.CmdBindDescriptorSets(
            cmd,
            PipelineBindPoint.Graphics,
            layout,
            0,
            1,
            in descriptorSet,
            0,
            null
        );

        if (useCustom)
            materials.Apply(cmd, material!, layout);

        // 頂点は毎回変わりうるので、フレーム用のアリーナに積む
        var (vertexBuffer, vertexOffset) = ctx.CurrentArena.Push<float>(vertices);
        var (indexBuffer, indexOffset) = ctx.CurrentArena.Push<uint>(indices);
        vk.CmdBindVertexBuffers(cmd, 0, 1, in vertexBuffer, in vertexOffset);
        vk.CmdBindIndexBuffer(cmd, indexBuffer, indexOffset, IndexType.Uint32);
        vk.CmdDrawIndexed(cmd, DeformedQuadHelper.IndexCount, 1, 0, 0, 0);
    }
}
