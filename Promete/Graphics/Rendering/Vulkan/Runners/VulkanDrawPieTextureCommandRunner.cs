using System;
using System.Drawing;
using System.Numerics;
using Promete.Graphics.Rendering.Commands;
using Promete.Internal;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="DrawPieTextureCommand"/> で扇形テクスチャを描画するランナーです。
/// </summary>
internal sealed unsafe class VulkanDrawPieTextureCommandRunner(
    VulkanContext ctx,
    VulkanResourceManager resources,
    VulkanPipelineProvider pipelines
) : CommandRunner<DrawPieTextureCommand>, IDisposable
{
    // push constant: mat4 (16) + vec4 tint (4) + vec2 angles (2) + padding (2) = 24 floats
    private const int PushConstantFloats = 24;

    private Buffer _quadVbo;
    private DeviceMemory _quadVboMemory;
    private Buffer _quadEbo;
    private DeviceMemory _quadEboMemory;
    private bool _initialized;
    private bool _materialWarned;
    private bool _disposed;

    public override void Execute(DrawPieTextureCommand command)
    {
        Draw(
            command.Texture,
            command.ModelMatrix,
            command.TintColor,
            command.Width,
            command.Height,
            command.StartPercent,
            command.Percent,
            command.Material
        );
    }

    public void Dispose()
    {
        if (_disposed || !_initialized)
            return;
        _disposed = true;
        var vk = ctx.Vk;
        vk.DestroyBuffer(ctx.Device, _quadVbo, null);
        vk.FreeMemory(ctx.Device, _quadVboMemory, null);
        vk.DestroyBuffer(ctx.Device, _quadEbo, null);
        vk.FreeMemory(ctx.Device, _quadEboMemory, null);
    }

    private void Draw(
        Texture2D texture,
        Matrix4x4 modelMatrix,
        Color color,
        float width,
        float height,
        float startPercent,
        float percent,
        Material? material
    )
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (!ctx.IsFrameActive || !resources.Contains(texture.Handle))
            return;

        // TODO: カスタムマテリアルの適用に対応する
        if (material is not null && !_materialWarned)
        {
            _materialWarned = true;
            LogHelper.Bug("Vulkan バックエンドはまだ PieSprite のカスタムマテリアルをサポートしていません。");
        }

        EnsureInitialized();

        var model =
            Matrix4x4.CreateScale(width, height, 1)
            * modelMatrix;

        var extent = ctx.CurrentTargetExtent;
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            extent.Width,
            0,
            extent.Height,
            -1f,
            1f
        );
        var mvp = model * projection;

        // パーセント→ラジアン変換（12時方向を0%にするため-90度オフセット）
        var startAngle = ((startPercent / 100.0f * 360.0f) - 90.0f) * MathF.PI / 180.0f;
        var endAngle = ((percent / 100.0f * 360.0f) - 90.0f) * MathF.PI / 180.0f;

        var push = stackalloc float[PushConstantFloats];
        *(Matrix4x4*)push = mvp;
        push[16] = color.R / 255f;
        push[17] = color.G / 255f;
        push[18] = color.B / 255f;
        push[19] = color.A / 255f;
        push[20] = startAngle;
        push[21] = endAngle;

        var vk = ctx.Vk;
        var cmd = ctx.CurrentCommandBuffer;

        var pipeline = pipelines.GetPiePipeline(
            VulkanPipelineProvider.PassClass.Offscreen,
            ctx.StencilMaskActive
                ? VulkanPipelineProvider.StencilMode.TestEqual
                : VulkanPipelineProvider.StencilMode.None
        );
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);
        vk.CmdPushConstants(
            cmd,
            pipelines.PieLayout,
            ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            0,
            PushConstantFloats * sizeof(float),
            push
        );

        var descriptorSet = resources.GetDescriptorSet(texture.Handle);
        vk.CmdBindDescriptorSets(
            cmd,
            PipelineBindPoint.Graphics,
            pipelines.PieLayout,
            0,
            1,
            in descriptorSet,
            0,
            null
        );

        var offset = 0ul;
        vk.CmdBindVertexBuffers(cmd, 0, 1, in _quadVbo, in offset);
        vk.CmdBindIndexBuffer(cmd, _quadEbo, 0, IndexType.Uint32);
        vk.CmdDrawIndexed(cmd, 6, 1, 0, 0, 0);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        Span<float> vertices =
        [
            1.0f, 0.0f, 1.0f, 0.0f, // 右上
            1.0f, 1.0f, 1.0f, 1.0f, // 右下
            0.0f, 1.0f, 0.0f, 1.0f, // 左下
            0.0f, 0.0f, 0.0f, 0.0f, // 左上
        ];
        Span<uint> indices = [0, 1, 3, 1, 2, 3];

        (_quadVbo, _quadVboMemory) = CreateStaticBuffer<float>(vertices, BufferUsageFlags.VertexBufferBit);
        (_quadEbo, _quadEboMemory) = CreateStaticBuffer<uint>(indices, BufferUsageFlags.IndexBufferBit);
        _initialized = true;
    }

    private (Buffer Buffer, DeviceMemory Memory) CreateStaticBuffer<T>(
        ReadOnlySpan<T> data,
        BufferUsageFlags usage
    )
        where T : unmanaged
    {
        var size = (ulong)(data.Length * sizeof(T));
        var (buffer, memory) = ctx.CreateBuffer(
            size,
            usage,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* mapped;
        ctx.Vk.MapMemory(ctx.Device, memory, 0, size, 0, &mapped);
        fixed (T* src = data)
        {
            System.Buffer.MemoryCopy(src, mapped, size, size);
        }

        ctx.Vk.UnmapMemory(ctx.Device, memory);
        return (buffer, memory);
    }
}
