using System;
using System.Collections.Generic;
using System.Numerics;
using Promete.Graphics.Rendering.Commands;
using Promete.Internal;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan.Runners;

/// <summary>
/// <see cref="DrawTextureBatchedCommand"/> をインスタンシングで描画するランナーです。
/// </summary>
internal sealed unsafe class VulkanDrawTextureBatchedCommandRunner
    : CommandRunner<DrawTextureBatchedCommand>,
        IDisposable
{
    private const int InitialInstanceCapacity = 512;

    // per-instance: mat4(16) + vec4 tintColor(4) + vec4 uvRect(4) = 24 floats
    private const int InstanceStride = 24;

    private readonly VulkanContext _ctx;
    private readonly VulkanResourceManager _resources;
    private readonly VulkanPipelineProvider _pipelines;
    private readonly VulkanShaderManager _shaders;
    private readonly VulkanMaterialSystem _materials;

    private float[] _instanceData = new float[InitialInstanceCapacity * InstanceStride];
    private Buffer _quadVbo;
    private DeviceMemory _quadVboMemory;
    private Buffer _quadEbo;
    private DeviceMemory _quadEboMemory;
    private bool _initialized;
    private bool _materialWarned;
    private bool _disposed;

    public VulkanDrawTextureBatchedCommandRunner(
        VulkanContext ctx,
        VulkanResourceManager resources,
        VulkanPipelineProvider pipelines,
        VulkanShaderManager shaders,
        VulkanMaterialSystem materials
    )
    {
        _ctx = ctx;
        _resources = resources;
        _pipelines = pipelines;
        _shaders = shaders;
        _materials = materials;
    }

    public override void Execute(DrawTextureBatchedCommand command)
    {
        DrawInstanced(command.Items, command.Material);
    }

    public void Dispose()
    {
        if (_disposed || !_initialized)
            return;
        _disposed = true;
        var vk = _ctx.Vk;
        vk.DestroyBuffer(_ctx.Device, _quadVbo, null);
        vk.FreeMemory(_ctx.Device, _quadVboMemory, null);
        vk.DestroyBuffer(_ctx.Device, _quadEbo, null);
        vk.FreeMemory(_ctx.Device, _quadEboMemory, null);
    }

    private void DrawInstanced(List<DrawTextureCommand> items, Material? material)
    {
        if (items.Count == 0 || !_ctx.IsFrameActive)
            return;
        PrometeApp.Current.ThrowIfNotMainThread();

        var textureId = items[0].Texture.Handle;
        if (!_resources.Contains(textureId))
            return;

        // カスタムマテリアル: シェーダーがコンパイル済みならカスタムパイプラインを使用
        var useCustom = material is not null && _shaders.Contains(material.Shader.Handle);
        if (material is not null && !useCustom && !_materialWarned)
        {
            _materialWarned = true;
            LogHelper.Bug(
                "Material のシェーダーがコンパイルされていないため、デフォルトシェーダーで描画します。"
            );
        }

        EnsureInitialized();

        var count = items.Count;
        EnsureInstanceCapacity(count);

        // per-instanceデータを構築
        for (var i = 0; i < count; i++)
        {
            var cmd = items[i];
            var model =
                Matrix4x4.CreateScale(cmd.Width, cmd.Height, 1)
                * Matrix4x4.CreateTranslation(cmd.Pivot.X, cmd.Pivot.Y, 0)
                * cmd.ModelMatrix;

            var offset = i * InstanceStride;
            _instanceData[offset + 0] = model.M11;
            _instanceData[offset + 1] = model.M12;
            _instanceData[offset + 2] = model.M13;
            _instanceData[offset + 3] = model.M14;
            _instanceData[offset + 4] = model.M21;
            _instanceData[offset + 5] = model.M22;
            _instanceData[offset + 6] = model.M23;
            _instanceData[offset + 7] = model.M24;
            _instanceData[offset + 8] = model.M31;
            _instanceData[offset + 9] = model.M32;
            _instanceData[offset + 10] = model.M33;
            _instanceData[offset + 11] = model.M34;
            _instanceData[offset + 12] = model.M41;
            _instanceData[offset + 13] = model.M42;
            _instanceData[offset + 14] = model.M43;
            _instanceData[offset + 15] = model.M44;
            var c = cmd.TintColor;
            _instanceData[offset + 16] = c.R / 255f;
            _instanceData[offset + 17] = c.G / 255f;
            _instanceData[offset + 18] = c.B / 255f;
            _instanceData[offset + 19] = c.A / 255f;

            var uvStart = cmd.Texture.UvStart;
            var uvEnd = cmd.Texture.UvEnd;
            _instanceData[offset + 20] = uvStart.X;
            _instanceData[offset + 21] = uvStart.Y;
            _instanceData[offset + 22] = uvEnd.X;
            _instanceData[offset + 23] = uvEnd.Y;
        }

        var (instanceBuffer, instanceOffset) = _ctx.CurrentArena.Push<float>(
            new ReadOnlySpan<float>(_instanceData, 0, count * InstanceStride)
        );

        var vk = _ctx.Vk;
        var cmdBuffer = _ctx.CurrentCommandBuffer;

        var pipeline = useCustom
            ? _pipelines.GetCustomSpritePipeline(
                material!.Shader.Handle,
                VulkanPipelineProvider.PassClass.Offscreen
            )
            : _pipelines.GetTexturePipeline(VulkanPipelineProvider.PassClass.Offscreen);
        var layout = useCustom ? _pipelines.CustomSpriteLayout : _pipelines.TextureLayout;
        vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Graphics, pipeline);

        // プロジェクション行列 (Vulkan は NDC が Y 下向きなので bottom=0, top=height)
        var extent = _ctx.CurrentTargetExtent;
        var projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            extent.Width,
            0,
            extent.Height,
            -1f,
            1f
        );
        vk.CmdPushConstants(cmdBuffer, layout, ShaderStageFlags.VertexBit, 0, 64, &projection);

        var descriptorSet = _resources.GetDescriptorSet(textureId);
        vk.CmdBindDescriptorSets(
            cmdBuffer,
            PipelineBindPoint.Graphics,
            layout,
            0,
            1,
            in descriptorSet,
            0,
            null
        );

        if (useCustom)
            _materials.Apply(cmdBuffer, material!, layout);

        var vertexBuffers = stackalloc Buffer[2] { _quadVbo, instanceBuffer };
        var offsets = stackalloc ulong[2] { 0, instanceOffset };
        vk.CmdBindVertexBuffers(cmdBuffer, 0, 2, vertexBuffers, offsets);
        vk.CmdBindIndexBuffer(cmdBuffer, _quadEbo, 0, IndexType.Uint32);
        vk.CmdDrawIndexed(cmdBuffer, 6, (uint)count, 0, 0, 0);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        // 単位クワッドの頂点データ（位置 + UV）
        Span<float> vertices =
        [
            1.0f, 0.0f, 1.0f, 0.0f, // 右下
            1.0f, 1.0f, 1.0f, 1.0f, // 右上
            0.0f, 1.0f, 0.0f, 1.0f, // 左上
            0.0f, 0.0f, 0.0f, 0.0f, // 左下
        ];
        Span<uint> indices = [0, 1, 3, 1, 2, 3];

        (_quadVbo, _quadVboMemory) = CreateStaticBuffer<float>(
            vertices,
            BufferUsageFlags.VertexBufferBit
        );
        (_quadEbo, _quadEboMemory) = CreateStaticBuffer<uint>(
            indices,
            BufferUsageFlags.IndexBufferBit
        );

        _initialized = true;
    }

    private (Buffer Buffer, DeviceMemory Memory) CreateStaticBuffer<T>(
        ReadOnlySpan<T> data,
        BufferUsageFlags usage
    )
        where T : unmanaged
    {
        var size = (ulong)(data.Length * sizeof(T));
        var (buffer, memory) = _ctx.CreateBuffer(
            size,
            usage,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* mapped;
        _ctx.Vk.MapMemory(_ctx.Device, memory, 0, size, 0, &mapped);
        fixed (T* src = data)
        {
            System.Buffer.MemoryCopy(src, mapped, size, size);
        }

        _ctx.Vk.UnmapMemory(_ctx.Device, memory);
        return (buffer, memory);
    }

    private void EnsureInstanceCapacity(int count)
    {
        var required = count * InstanceStride;
        if (_instanceData.Length >= required)
            return;

        var newSize = _instanceData.Length;
        while (newSize < required)
            newSize *= 2;
        _instanceData = new float[newSize];
    }
}
