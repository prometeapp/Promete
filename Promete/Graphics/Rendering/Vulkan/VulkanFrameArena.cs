using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// フレームごとの動的頂点データ用リングバッファです。
/// ホスト可視メモリを持続的にマップし、バンプアロケートで割り当てます。
/// 容量不足時は新しいバッファに切り替え、旧バッファはフレーム完了後に破棄します。
/// </summary>
internal sealed unsafe class VulkanFrameArena : IDisposable
{
    private const ulong InitialCapacity = 256 * 1024;

    private readonly VulkanContext _ctx;
    private readonly List<(Buffer Buffer, DeviceMemory Memory)> _retired = [];

    private Buffer _buffer;
    private DeviceMemory _memory;
    private byte* _mapped;
    private ulong _capacity;
    private ulong _offset;
    private bool _disposed;

    public VulkanFrameArena(VulkanContext ctx)
    {
        _ctx = ctx;
        AllocateBuffer(InitialCapacity);
    }

    /// <summary>
    /// データをアリーナへ書き込み、バッファとオフセットを返します。
    /// </summary>
    public (Buffer Buffer, ulong Offset) Push<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var size = (ulong)(data.Length * sizeof(T));
        var aligned = (_offset + 15) & ~15ul;

        if (aligned + size > _capacity)
        {
            // 旧バッファは記録済みコマンドから参照されているため、フレーム完了まで保持する
            _retired.Add((_buffer, _memory));
            var newCapacity = Math.Max(_capacity * 2, aligned + size);
            AllocateBuffer(newCapacity);
            aligned = 0;
        }

        fixed (T* src = data)
        {
            System.Buffer.MemoryCopy(src, _mapped + aligned, size, size);
        }

        _offset = aligned + size;
        return (_buffer, aligned);
    }

    /// <summary>
    /// フレーム開始時に呼び出し、オフセットをリセットして退役バッファを破棄します。
    /// このスロットのフェンス待機後に呼び出してください。
    /// </summary>
    public void Reset()
    {
        _offset = 0;
        if (_retired.Count == 0)
            return;

        foreach (var (buffer, memory) in _retired)
            DestroyBuffer(buffer, memory);
        _retired.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var (buffer, memory) in _retired)
            DestroyBuffer(buffer, memory);
        _retired.Clear();
        _ctx.Vk.UnmapMemory(_ctx.Device, _memory);
        DestroyBuffer(_buffer, _memory);
    }

    private void AllocateBuffer(ulong capacity)
    {
        (_buffer, _memory) = _ctx.CreateBuffer(
            capacity,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* mapped;
        _ctx.Vk.MapMemory(_ctx.Device, _memory, 0, capacity, 0, &mapped);
        _mapped = (byte*)mapped;
        _capacity = capacity;
        _offset = 0;
    }

    private void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        _ctx.Vk.DestroyBuffer(_ctx.Device, buffer, null);
        _ctx.Vk.FreeMemory(_ctx.Device, memory, null);
    }
}
