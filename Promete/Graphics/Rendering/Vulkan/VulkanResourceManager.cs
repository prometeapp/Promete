using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// テクスチャリソース (VkImage / ImageView / DescriptorSet) を int の ID で管理するテーブルです。
/// <see cref="Texture2D.Handle"/> はこのテーブルの ID を指します。
/// </summary>
internal sealed unsafe class VulkanResourceManager : IDisposable
{
    private const uint MaxDescriptorSets = 4096;

    private readonly VulkanContext _ctx;
    private readonly Dictionary<int, VulkanTextureEntry> _textures = [];

    private DescriptorPool _descriptorPool;
    private DescriptorSetLayout _textureSetLayout;
    private Sampler _nearestSampler;
    private int _nextId = 1;
    private bool _initialized;
    private bool _disposed;

    public VulkanResourceManager(VulkanContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>テクスチャ 1 枚 (combined image sampler) 用のディスクリプタセットレイアウトを取得します。</summary>
    public DescriptorSetLayout TextureSetLayout
    {
        get
        {
            EnsureInitialized();
            return _textureSetLayout;
        }
    }

    /// <summary>
    /// RGBA8 のピクセルデータからテクスチャを作成し、ID を返します。
    /// </summary>
    public int CreateTexture(ReadOnlySpan<byte> rgba, uint width, uint height)
    {
        EnsureInitialized();

        var (image, memory) = _ctx.CreateImage2D(
            width,
            height,
            VulkanContext.OffscreenFormat,
            ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit
        );

        UploadPixels(image, rgba, width, height);

        var view = _ctx.CreateImageView2D(image, VulkanContext.OffscreenFormat);
        return Register(image, memory, view, ownsImage: true);
    }

    /// <summary>
    /// 既存のイメージ (RenderTexture 等) をテーブルに登録し、ID を返します。
    /// </summary>
    public int Register(Image image, DeviceMemory memory, ImageView view, bool ownsImage)
    {
        EnsureInitialized();

        var descriptorSet = AllocateTextureDescriptorSet(view);
        var id = _nextId++;
        _textures[id] = new VulkanTextureEntry
        {
            Image = image,
            Memory = memory,
            View = view,
            DescriptorSet = descriptorSet,
            OwnsImage = ownsImage,
        };
        return id;
    }

    /// <summary>
    /// 登録済みイメージの差し替え（RenderTexture のリサイズ用）。ディスクリプタセットも更新します。
    /// 呼び出し前にデバイスがアイドルであることを保証してください。
    /// </summary>
    public void Replace(int id, Image image, DeviceMemory memory, ImageView view)
    {
        var entry = _textures[id];
        entry.Image = image;
        entry.Memory = memory;
        entry.View = view;
        UpdateTextureDescriptorSet(entry.DescriptorSet, view);
    }

    /// <summary>
    /// テクスチャのディスクリプタセットを取得します。
    /// </summary>
    public DescriptorSet GetDescriptorSet(int id) => _textures[id].DescriptorSet;

    /// <summary>
    /// テクスチャのイメージを取得します。
    /// </summary>
    public Image GetImage(int id) => _textures[id].Image;

    /// <summary>
    /// テクスチャが登録されているかを取得します。
    /// </summary>
    public bool Contains(int id) => _textures.ContainsKey(id);

    /// <summary>
    /// テクスチャを破棄します。GPU が使用中の可能性があるため、実際の破棄は遅延されます。
    /// </summary>
    public void Destroy(int id)
    {
        if (!_textures.Remove(id, out var entry))
            return;

        var vk = _ctx.Vk;
        var device = _ctx.Device;
        _ctx.DeferDestroy(() =>
        {
            var set = entry.DescriptorSet;
            vk.FreeDescriptorSets(device, _descriptorPool, 1, in set);
            if (entry.OwnsImage)
            {
                vk.DestroyImageView(device, entry.View, null);
                vk.DestroyImage(device, entry.Image, null);
                vk.FreeMemory(device, entry.Memory, null);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var vk = _ctx.Vk;
        var device = _ctx.Device;

        foreach (var entry in _textures.Values)
        {
            if (!entry.OwnsImage)
                continue;
            vk.DestroyImageView(device, entry.View, null);
            vk.DestroyImage(device, entry.Image, null);
            vk.FreeMemory(device, entry.Memory, null);
        }

        _textures.Clear();

        if (_initialized)
        {
            vk.DestroySampler(device, _nearestSampler, null);
            vk.DestroyDescriptorPool(device, _descriptorPool, null);
            vk.DestroyDescriptorSetLayout(device, _textureSetLayout, null);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;
        Initialize();
        _initialized = true;
    }

    private void Initialize()
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;

        // ディスクリプタプール
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.CombinedImageSampler,
            DescriptorCount = MaxDescriptorSets,
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
            MaxSets = MaxDescriptorSets,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
        };
        vk.CreateDescriptorPool(device, in poolInfo, null, out _descriptorPool);

        // セットレイアウト: binding 0 = combined image sampler (fragment)
        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit,
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding,
        };
        vk.CreateDescriptorSetLayout(device, in layoutInfo, null, out _textureSetLayout);

        // Nearest サンプラー (ピクセルパーフェクト描画用、ClampToEdge)
        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            MipmapMode = SamplerMipmapMode.Nearest,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxLod = 0,
        };
        vk.CreateSampler(device, in samplerInfo, null, out _nearestSampler);
    }

    private DescriptorSet AllocateTextureDescriptorSet(ImageView view)
    {
        var layout = _textureSetLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout,
        };
        var result = _ctx.Vk.AllocateDescriptorSets(_ctx.Device, in allocInfo, out var set);
        if (result != Result.Success)
            throw new InvalidOperationException($"ディスクリプタセットの確保に失敗しました: {result}");

        UpdateTextureDescriptorSet(set, view);
        return set;
    }

    private void UpdateTextureDescriptorSet(DescriptorSet set, ImageView view)
    {
        var imageInfo = new DescriptorImageInfo
        {
            Sampler = _nearestSampler,
            ImageView = view,
            ImageLayout = ImageLayout.General,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImageInfo = &imageInfo,
        };
        _ctx.Vk.UpdateDescriptorSets(_ctx.Device, 1, in write, 0, null);
    }

    private void UploadPixels(Image image, ReadOnlySpan<byte> rgba, uint width, uint height)
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;
        var size = (ulong)(width * height * 4);

        var (staging, stagingMemory) = _ctx.CreateBuffer(
            size,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        void* mapped;
        vk.MapMemory(device, stagingMemory, 0, size, 0, &mapped);
        fixed (byte* src = rgba)
        {
            System.Buffer.MemoryCopy(src, mapped, size, size);
        }

        vk.UnmapMemory(device, stagingMemory);

        _ctx.ExecuteOneTime(cmd =>
        {
            _ctx.TransitionImageLayout(
                cmd,
                image,
                ImageLayout.Undefined,
                ImageLayout.TransferDstOptimal,
                PipelineStageFlags.TopOfPipeBit,
                0,
                PipelineStageFlags.TransferBit,
                AccessFlags.TransferWriteBit
            );

            var region = new BufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(width, height, 1),
            };
            vk.CmdCopyBufferToImage(cmd, staging, image, ImageLayout.TransferDstOptimal, 1, in region);

            // サンプリング時のレイアウト管理を単純化するため General に統一する
            _ctx.TransitionImageLayout(
                cmd,
                image,
                ImageLayout.TransferDstOptimal,
                ImageLayout.General,
                PipelineStageFlags.TransferBit,
                AccessFlags.TransferWriteBit,
                PipelineStageFlags.FragmentShaderBit,
                AccessFlags.ShaderReadBit
            );
        });

        vk.DestroyBuffer(device, staging, null);
        vk.FreeMemory(device, stagingMemory, null);
    }

    private sealed class VulkanTextureEntry
    {
        public required Image Image { get; set; }

        public required DeviceMemory Memory { get; set; }

        public required ImageView View { get; set; }

        public required DescriptorSet DescriptorSet { get; init; }

        public required bool OwnsImage { get; init; }
    }
}
