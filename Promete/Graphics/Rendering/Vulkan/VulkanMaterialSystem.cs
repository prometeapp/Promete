using System;
using System.Collections.Generic;
using System.Numerics;
using Promete.Internal;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// <see cref="Material"/> の名前ベース Uniform 値を、リフレクション結果に基づいて
/// per-material の Uniform バッファ (set=1, binding=0) へ書き込み、バインドします。
/// </summary>
internal sealed unsafe class VulkanMaterialSystem : IDisposable
{
    private const uint MaxSets = 1024;

    private readonly VulkanContext _ctx;
    private readonly VulkanShaderManager _shaders;
    private readonly Dictionary<(Material Material, int Slot), MaterialSlot> _slots = [];
    private readonly HashSet<string> _warnedUniforms = [];

    private DescriptorPool _pool;
    private DescriptorSetLayout _uboSetLayout;
    private bool _initialized;
    private bool _disposed;

    public VulkanMaterialSystem(VulkanContext ctx, VulkanShaderManager shaders)
    {
        _ctx = ctx;
        _shaders = shaders;
    }

    /// <summary>Uniform ブロック (set=1) 用のディスクリプタセットレイアウトを取得します。</summary>
    public DescriptorSetLayout UboSetLayout
    {
        get
        {
            EnsureInitialized();
            return _uboSetLayout;
        }
    }

    /// <summary>
    /// マテリアルの Uniform 値を UBO へ書き込み、set=1 としてバインドします。
    /// シェーダーに Uniform ブロックが無い場合は何もしません。
    /// </summary>
    public void Apply(CommandBuffer cmd, Material material, PipelineLayout pipelineLayout)
    {
        var entry = _shaders.Get(material.Shader.Handle);
        if (entry.UniformBlock is not { } block)
            return;

        EnsureInitialized();

        var slotKey = (material, _ctx.FrameIndex);
        if (!_slots.TryGetValue(slotKey, out var slot))
        {
            slot = CreateSlot(block.Size);
            _slots[slotKey] = slot;
        }

        // Uniform 値をオフセットに従って書き込む
        foreach (var (name, value) in material.Uniforms)
        {
            if (!block.MemberOffsets.TryGetValue(name, out var offset))
                continue;
            WriteValue(slot.Mapped + offset, value, name);
        }

        var set = slot.Set;
        _ctx.Vk.CmdBindDescriptorSets(
            cmd,
            PipelineBindPoint.Graphics,
            pipelineLayout,
            1,
            1,
            in set,
            0,
            null
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var vk = _ctx.Vk;
        var device = _ctx.Device;

        foreach (var slot in _slots.Values)
        {
            vk.DestroyBuffer(device, slot.Buffer, null);
            vk.FreeMemory(device, slot.Memory, null);
        }

        _slots.Clear();

        if (_initialized)
        {
            vk.DestroyDescriptorPool(device, _pool, null);
            vk.DestroyDescriptorSetLayout(device, _uboSetLayout, null);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        var vk = _ctx.Vk;
        var device = _ctx.Device;

        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = MaxSets,
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = MaxSets,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
        };
        vk.CreateDescriptorPool(device, in poolInfo, null, out _pool);

        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding,
        };
        vk.CreateDescriptorSetLayout(device, in layoutInfo, null, out _uboSetLayout);

        _initialized = true;
    }

    private MaterialSlot CreateSlot(uint size)
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;

        var (buffer, memory) = _ctx.CreateBuffer(
            size,
            BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* mapped;
        vk.MapMemory(device, memory, 0, size, 0, &mapped);

        var layout = _uboSetLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout,
        };
        var result = vk.AllocateDescriptorSets(device, in allocInfo, out var set);
        if (result != Result.Success)
            throw new InvalidOperationException($"ディスクリプタセットの確保に失敗しました: {result}");

        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = buffer,
            Offset = 0,
            Range = size,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PBufferInfo = &bufferInfo,
        };
        vk.UpdateDescriptorSets(device, 1, in write, 0, null);

        return new MaterialSlot
        {
            Buffer = buffer,
            Memory = memory,
            Mapped = (byte*)mapped,
            Set = set,
        };
    }

    private void WriteValue(byte* dst, object value, string name)
    {
        switch (value)
        {
            case float f:
                *(float*)dst = f;
                break;
            case int i:
                *(int*)dst = i;
                break;
            case Vector v:
                *(Vector2*)dst = new Vector2(v.X, v.Y);
                break;
            case VectorInt vi:
                *(Vector2*)dst = new Vector2(vi.X, vi.Y);
                break;
            case Vector2 v2:
                *(Vector2*)dst = v2;
                break;
            case Vector3 v3:
                *(Vector3*)dst = v3;
                break;
            case Vector4 v4:
                *(Vector4*)dst = v4;
                break;
            case Matrix4x4 m:
                *(Matrix4x4*)dst = m;
                break;
            case Texture2D:
                if (_warnedUniforms.Add(name))
                    LogHelper.Bug(
                        $"Vulkan バックエンドはまだ Material の Texture2D Uniform ({name}) をサポートしていません。"
                    );
                break;
        }
    }

    private sealed class MaterialSlot
    {
        public required Buffer Buffer { get; init; }

        public required DeviceMemory Memory { get; init; }

        public required byte* Mapped { get; init; }

        public required DescriptorSet Set { get; init; }
    }
}
