using System;
using System.Collections.Generic;
using System.Drawing;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan のインスタンス・デバイス・スワップチェーン・フレーム同期を管理するコンテキストです。
/// フレームのライフサイクル（コマンドバッファ記録・サブミット・プレゼント）と、
/// オフスクリーンレンダーターゲットのスタック管理も担います。
/// </summary>
internal sealed unsafe class VulkanContext : IDisposable
{
    /// <summary>同時進行フレーム数。</summary>
    public const int FramesInFlight = 2;

    /// <summary>オフスクリーンレンダーターゲットのカラーフォーマット。</summary>
    public const Format OffscreenFormat = Format.R8G8B8A8Unorm;

    private readonly IWindow _window;
    private readonly Stack<VulkanRenderTarget> _targetStack = new();
    private readonly List<Action>[] _deferredDestroys = new List<Action>[FramesInFlight];

    private KhrSurface _khrSurface = null!;
    private KhrSwapchain _khrSwapchain = null!;

    private Instance _instance;
    private SurfaceKHR _surface;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private uint _queueFamilyIndex;
    private Queue _graphicsQueue;
    private PhysicalDeviceMemoryProperties _memoryProperties;

    private SwapchainKHR _swapchain;
    private Format _swapchainFormat;
    private Extent2D _swapchainExtent;
    private Image[] _swapchainImages = [];
    private ImageView[] _swapchainImageViews = [];
    private Framebuffer[] _framebuffers = [];

    private RenderPass _swapchainPass;
    private RenderPass _offscreenClearPass;
    private RenderPass _offscreenLoadPass;
    private CommandPool _commandPool;
    private CommandPool _transientPool;
    private CommandBuffer[] _commandBuffers = [];
    private VulkanFrameArena[] _arenas = [];

    private Semaphore[] _imageAvailableSemaphores = [];
    private Semaphore[] _renderFinishedSemaphores = [];
    private Fence[] _inFlightFences = [];

    private int _currentFrame;
    private uint _currentImageIndex;
    private bool _frameActive;
    private bool _swapchainPassActive;
    private bool _swapchainPassDone;
    private bool _framebufferResized;
    private bool _disposed;

    public VulkanContext(IWindow window)
    {
        _window = window;
        _window.FramebufferResize += _ => _framebufferResized = true;
        for (var i = 0; i < FramesInFlight; i++)
            _deferredDestroys[i] = [];
    }

    /// <summary>Vulkan API のエントリポイントを取得します。</summary>
    public Vk Vk { get; } = Vk.GetApi();

    /// <summary>論理デバイスを取得します。</summary>
    public Device Device => _device;

    /// <summary>物理デバイスを取得します。</summary>
    public PhysicalDevice PhysicalDevice => _physicalDevice;

    /// <summary>グラフィックス兼プレゼントキューを取得します。</summary>
    public Queue GraphicsQueue => _graphicsQueue;

    /// <summary>スワップチェーンのフォーマットを取得します。</summary>
    public Format SwapchainFormat => _swapchainFormat;

    /// <summary>スワップチェーンの大きさを取得します。</summary>
    public Extent2D SwapchainExtent => _swapchainExtent;

    /// <summary>初期化済みかどうかを取得します。</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>フレームが記録中かどうかを取得します。</summary>
    public bool IsFrameActive => _frameActive;

    /// <summary>現在のフレームスロット (0..FramesInFlight-1) を取得します。</summary>
    public int FrameIndex => _currentFrame;

    /// <summary>現在記録中のコマンドバッファを取得します。</summary>
    public CommandBuffer CurrentCommandBuffer => _commandBuffers[_currentFrame];

    /// <summary>現在のフレームで使用する動的頂点データアリーナを取得します。</summary>
    public VulkanFrameArena CurrentArena => _arenas[_currentFrame];

    /// <summary>オフスクリーン描画用 (クリア) レンダーパスを取得します。</summary>
    public RenderPass OffscreenClearPass => _offscreenClearPass;

    /// <summary>オフスクリーン描画用 (ロード) レンダーパスを取得します。</summary>
    public RenderPass OffscreenLoadPass => _offscreenLoadPass;

    /// <summary>スワップチェーン描画用レンダーパスを取得します。</summary>
    public RenderPass SwapchainPass => _swapchainPass;

    /// <summary>現在のトリム (シザー) 領域。null なら全域。</summary>
    public Rect2D? TrimScissor { get; private set; }

    /// <summary>オフスクリーンターゲットのステンシルフォーマットを取得します。</summary>
    public Format StencilFormat { get; private set; }

    /// <summary>
    /// ステンシルマスクが有効かどうかを取得または設定します。
    /// 有効な間、ランナーはステンシルテスト (Equal, ref=1) 付きパイプラインを使用します。
    /// </summary>
    public bool StencilMaskActive { get; set; }

    /// <summary>現在の描画ターゲットの大きさを取得します。</summary>
    public Extent2D CurrentTargetExtent =>
        _targetStack.Count > 0 ? _targetStack.Peek().Extent : _swapchainExtent;

    /// <summary>
    /// Vulkan オブジェクトを初期化します。ウィンドウのロード後（サーフェスが取得可能になった後）に呼び出してください。
    /// </summary>
    public void Initialize(string appName)
    {
        CreateInstance(appName);
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchain();
        CreateImageViews();
        ChooseStencilFormat();
        CreateRenderPasses();
        CreateFramebuffers();
        CreateCommandPools();
        CreateCommandBuffers();
        CreateSyncObjects();

        _arenas = new VulkanFrameArena[FramesInFlight];
        for (var i = 0; i < FramesInFlight; i++)
            _arenas[i] = new VulkanFrameArena(this);

        IsInitialized = true;
    }

    // --- フレームライフサイクル ---

    /// <summary>
    /// フレームの記録を開始します。スワップチェーンイメージの取得とコマンドバッファの開始を行います。
    /// </summary>
    /// <returns>フレームを開始できた場合 true。最小化中などで描画をスキップする場合 false。</returns>
    public bool BeginFrame()
    {
        var fb = _window.FramebufferSize;
        if (fb.X <= 0 || fb.Y <= 0)
            return false;

        var vk = Vk;
        var fence = _inFlightFences[_currentFrame];
        vk.WaitForFences(_device, 1, in fence, true, ulong.MaxValue);

        // このスロットの前回フレームが完了したので、遅延破棄を実行
        FlushDeferredDestroys(_currentFrame);
        _arenas[_currentFrame].Reset();

        var result = _khrSwapchain.AcquireNextImage(
            _device,
            _swapchain,
            ulong.MaxValue,
            _imageAvailableSemaphores[_currentFrame],
            default,
            ref _currentImageIndex
        );

        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return false;
        }

        if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException($"スワップチェーンイメージの取得に失敗しました: {result}");

        vk.ResetFences(_device, 1, in fence);

        var cmd = _commandBuffers[_currentFrame];
        vk.ResetCommandBuffer(cmd, 0);
        var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        ThrowIfFailed(vk.BeginCommandBuffer(cmd, in beginInfo), "コマンドバッファの記録開始");

        _frameActive = true;
        _swapchainPassActive = false;
        _swapchainPassDone = false;
        TrimScissor = null;
        return true;
    }

    /// <summary>
    /// フレームの記録を終了し、サブミット・プレゼントします。
    /// </summary>
    public void EndFrame()
    {
        if (!_frameActive)
            return;

        var vk = Vk;
        var cmd = _commandBuffers[_currentFrame];

        // ブリットが行われなかった場合でも、スワップチェーンイメージをプレゼント可能な状態にする
        if (!_swapchainPassDone)
            BeginSwapchainPass(Color.Black);

        // スワップチェーンパスは ImGui 等のオーバーレイ描画のためフレーム終了まで開いたままにしている
        if (_swapchainPassActive)
            EndSwapchainPass();

        ThrowIfFailed(vk.EndCommandBuffer(cmd), "コマンドバッファの記録終了");
        _frameActive = false;
        _targetStack.Clear();

        var waitSemaphore = _imageAvailableSemaphores[_currentFrame];
        var signalSemaphore = _renderFinishedSemaphores[_currentFrame];
        var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore,
        };

        ThrowIfFailed(
            vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]),
            "キューの送信"
        );

        var swapchain = _swapchain;
        var imageIndex = _currentImageIndex;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
        };

        var result = _khrSwapchain.QueuePresent(_graphicsQueue, in presentInfo);

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr || _framebufferResized)
        {
            _framebufferResized = false;
            RecreateSwapchain();
        }
        else if (result != Result.Success)
        {
            throw new InvalidOperationException($"プレゼントに失敗しました: {result}");
        }

        _currentFrame = (_currentFrame + 1) % FramesInFlight;
    }

    // --- レンダーターゲットスタック ---

    /// <summary>
    /// レンダーターゲットをスタックに積み、そのターゲットへのレンダーパスを開始します。
    /// 既にパスが記録中の場合は中断し、Pop 時に再開します。
    /// </summary>
    public void PushRenderTarget(VulkanRenderTarget target, Color? clearColor)
    {
        EnsureFrameActive();
        var cmd = CurrentCommandBuffer;

        if (_targetStack.Count > 0)
            Vk.CmdEndRenderPass(cmd);

        _targetStack.Push(target);
        BeginOffscreenPass(target, clearColor);
    }

    /// <summary>
    /// レンダーターゲットをスタックから降ろし、前のターゲットへのレンダーパスを再開します。
    /// </summary>
    public void PopRenderTarget()
    {
        EnsureFrameActive();
        var cmd = CurrentCommandBuffer;

        Vk.CmdEndRenderPass(cmd);
        _targetStack.Pop();

        if (_targetStack.Count > 0)
            BeginOffscreenPass(_targetStack.Peek(), null);
    }

    /// <summary>
    /// スワップチェーンイメージへのレンダーパスを開始します。（画面ブリット用）
    /// </summary>
    public void BeginSwapchainPass(Color clearColor)
    {
        EnsureFrameActive();
        if (_targetStack.Count > 0)
            throw new InvalidOperationException(
                "レンダーターゲットのキャプチャ中はスワップチェーンパスを開始できません。"
            );

        var cmd = CurrentCommandBuffer;
        var clearValue = new ClearValue(ToClearColor(clearColor));
        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _swapchainPass,
            Framebuffer = _framebuffers[_currentImageIndex],
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchainExtent),
            ClearValueCount = 1,
            PClearValues = &clearValue,
        };

        Vk.CmdBeginRenderPass(cmd, in beginInfo, SubpassContents.Inline);
        ApplyViewportAndScissor(_swapchainExtent, ignoreTrim: true);
        _swapchainPassActive = true;
        _swapchainPassDone = true;
    }

    /// <summary>
    /// スワップチェーンイメージへのレンダーパスを終了します。
    /// </summary>
    public void EndSwapchainPass()
    {
        if (!_swapchainPassActive)
            return;
        Vk.CmdEndRenderPass(CurrentCommandBuffer);
        _swapchainPassActive = false;
    }

    /// <summary>
    /// トリム (シザー) 領域を設定します。null で全域に戻します。
    /// </summary>
    public void SetTrimScissor(Rect2D? scissor)
    {
        TrimScissor = scissor;
        ApplyCurrentScissor();
    }

    // --- リソースヘルパー ---

    /// <summary>
    /// バッファとそのメモリを作成します。
    /// </summary>
    public (Buffer Buffer, DeviceMemory Memory) CreateBuffer(
        ulong size,
        BufferUsageFlags usage,
        MemoryPropertyFlags properties
    )
    {
        var vk = Vk;
        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };
        ThrowIfFailed(vk.CreateBuffer(_device, in createInfo, null, out var buffer), "バッファの作成");

        vk.GetBufferMemoryRequirements(_device, buffer, out var requirements);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, properties),
        };
        ThrowIfFailed(vk.AllocateMemory(_device, in allocInfo, null, out var memory), "メモリの確保");
        vk.BindBufferMemory(_device, buffer, memory, 0);
        return (buffer, memory);
    }

    /// <summary>
    /// 2D イメージとそのメモリを作成します。
    /// </summary>
    public (Image Image, DeviceMemory Memory) CreateImage2D(
        uint width,
        uint height,
        Format format,
        ImageUsageFlags usage
    )
    {
        var vk = Vk;
        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = format,
            Extent = new Extent3D(width, height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
        };
        ThrowIfFailed(vk.CreateImage(_device, in createInfo, null, out var image), "イメージの作成");

        vk.GetImageMemoryRequirements(_device, image, out var requirements);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = FindMemoryType(
                requirements.MemoryTypeBits,
                MemoryPropertyFlags.DeviceLocalBit
            ),
        };
        ThrowIfFailed(vk.AllocateMemory(_device, in allocInfo, null, out var memory), "メモリの確保");
        vk.BindImageMemory(_device, image, memory, 0);
        return (image, memory);
    }

    /// <summary>
    /// 2D イメージビューを作成します。
    /// </summary>
    public ImageView CreateImageView2D(
        Image image,
        Format format,
        ImageAspectFlags aspect = ImageAspectFlags.ColorBit
    )
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(aspect, 0, 1, 0, 1),
        };
        ThrowIfFailed(
            Vk.CreateImageView(_device, in createInfo, null, out var view),
            "イメージビューの作成"
        );
        return view;
    }

    /// <summary>
    /// オフスクリーンパス用のフレームバッファを作成します。（カラー + ステンシル）
    /// </summary>
    public Framebuffer CreateOffscreenFramebuffer(
        ImageView colorView,
        ImageView stencilView,
        uint width,
        uint height
    )
    {
        var attachments = stackalloc ImageView[2] { colorView, stencilView };
        var createInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = _offscreenClearPass,
            AttachmentCount = 2,
            PAttachments = attachments,
            Width = width,
            Height = height,
            Layers = 1,
        };
        ThrowIfFailed(
            Vk.CreateFramebuffer(_device, in createInfo, null, out var framebuffer),
            "フレームバッファの作成"
        );
        return framebuffer;
    }

    /// <summary>
    /// 現在の描画ターゲットのステンシルアタッチメントを 0 でクリアします。
    /// レンダーパス記録中に呼び出してください。
    /// </summary>
    public void ClearStencil()
    {
        EnsureFrameActive();
        var attachment = new ClearAttachment
        {
            AspectMask = ImageAspectFlags.StencilBit,
            ClearValue = new ClearValue { DepthStencil = new ClearDepthStencilValue(1f, 0) },
        };
        var rect = new ClearRect
        {
            Rect = new Rect2D(new Offset2D(0, 0), CurrentTargetExtent),
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        Vk.CmdClearAttachments(CurrentCommandBuffer, 1, in attachment, 1, in rect);
    }

    /// <summary>
    /// 一時的なコマンドバッファでコマンドを実行し、完了まで待機します。（リソース転送用）
    /// </summary>
    public void ExecuteOneTime(Action<CommandBuffer> record)
    {
        var vk = Vk;
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _transientPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        vk.AllocateCommandBuffers(_device, in allocInfo, out var cmd);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        vk.BeginCommandBuffer(cmd, in beginInfo);
        record(cmd);
        vk.EndCommandBuffer(cmd);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
        };
        ThrowIfFailed(vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, default), "転送コマンドの送信");
        vk.QueueWaitIdle(_graphicsQueue);
        vk.FreeCommandBuffers(_device, _transientPool, 1, in cmd);
    }

    /// <summary>
    /// イメージのレイアウトを遷移します。
    /// </summary>
    public void TransitionImageLayout(
        CommandBuffer cmd,
        Image image,
        ImageLayout oldLayout,
        ImageLayout newLayout,
        PipelineStageFlags srcStage,
        AccessFlags srcAccess,
        PipelineStageFlags dstStage,
        AccessFlags dstAccess,
        ImageAspectFlags aspect = ImageAspectFlags.ColorBit
    )
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange(aspect, 0, 1, 0, 1),
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
        };
        Vk.CmdPipelineBarrier(
            cmd,
            srcStage,
            dstStage,
            0,
            0,
            null,
            0,
            null,
            1,
            in barrier
        );
    }

    /// <summary>
    /// イメージのピクセルを RGBA8 のバイト列として読み出します。（スクリーンショット用）
    /// 完了まで待機するため低速です。
    /// </summary>
    public byte[] ReadImagePixels(Image image, uint width, uint height)
    {
        var vk = Vk;
        var size = (ulong)(width * height * 4);

        var (staging, stagingMemory) = CreateBuffer(
            size,
            BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
        );

        ExecuteOneTime(cmd =>
        {
            TransitionImageLayout(
                cmd,
                image,
                ImageLayout.General,
                ImageLayout.General,
                PipelineStageFlags.ColorAttachmentOutputBit,
                AccessFlags.ColorAttachmentWriteBit,
                PipelineStageFlags.TransferBit,
                AccessFlags.TransferReadBit
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
            Vk.CmdCopyImageToBuffer(cmd, image, ImageLayout.General, staging, 1, in region);
        });

        var pixels = new byte[size];
        void* mapped;
        vk.MapMemory(_device, stagingMemory, 0, size, 0, &mapped);
        fixed (byte* dst = pixels)
        {
            System.Buffer.MemoryCopy(mapped, dst, size, size);
        }

        vk.UnmapMemory(_device, stagingMemory);
        vk.DestroyBuffer(_device, staging, null);
        vk.FreeMemory(_device, stagingMemory, null);
        return pixels;
    }

    /// <summary>
    /// 現在のフレームスロットの実行完了後にリソースを破棄するアクションを登録します。
    /// </summary>
    public void DeferDestroy(Action destroy)
    {
        if (!IsInitialized)
        {
            destroy();
            return;
        }

        _deferredDestroys[_currentFrame].Add(destroy);
    }

    /// <summary>
    /// デバイスの全処理完了を待機します。
    /// </summary>
    public void WaitIdle()
    {
        Vk.DeviceWaitIdle(_device);
    }

    /// <summary>
    /// メモリタイプを検索します。
    /// </summary>
    public uint FindMemoryType(uint typeBits, MemoryPropertyFlags properties)
    {
        for (var i = 0u; i < _memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << (int)i)) == 0)
                continue;
            if ((_memoryProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
                return i;
        }

        throw new InvalidOperationException("適切なメモリタイプが見つかりませんでした。");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var vk = Vk;
        vk.DeviceWaitIdle(_device);

        for (var i = 0; i < FramesInFlight; i++)
            FlushDeferredDestroys(i);

        foreach (var arena in _arenas)
            arena.Dispose();

        CleanupSwapchain();

        for (var i = 0; i < FramesInFlight; i++)
        {
            vk.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
            vk.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
            vk.DestroyFence(_device, _inFlightFences[i], null);
        }

        vk.DestroyCommandPool(_device, _commandPool, null);
        vk.DestroyCommandPool(_device, _transientPool, null);
        vk.DestroyRenderPass(_device, _swapchainPass, null);
        vk.DestroyRenderPass(_device, _offscreenClearPass, null);
        vk.DestroyRenderPass(_device, _offscreenLoadPass, null);
        vk.DestroyDevice(_device, null);
        _khrSurface.DestroySurface(_instance, _surface, null);
        vk.DestroyInstance(_instance, null);

        _khrSwapchain.Dispose();
        _khrSurface.Dispose();
        vk.Dispose();
    }

    private static ClearColorValue ToClearColor(Color c) =>
        new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

    private static void ThrowIfFailed(Result result, string operation)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"{operation}に失敗しました: {result}");
    }

    // --- private: フレーム内部処理 ---
    private void EnsureFrameActive()
    {
        if (!_frameActive)
            throw new InvalidOperationException("フレームの記録が開始されていません。");
    }

    private void BeginOffscreenPass(VulkanRenderTarget target, Color? clearColor)
    {
        var cmd = CurrentCommandBuffer;
        var clearValues = stackalloc ClearValue[2]
        {
            new ClearValue(ToClearColor(clearColor ?? Color.Transparent)),
            new ClearValue { DepthStencil = new ClearDepthStencilValue(1f, 0) },
        };
        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = clearColor.HasValue ? _offscreenClearPass : _offscreenLoadPass,
            Framebuffer = target.Framebuffer,
            RenderArea = new Rect2D(new Offset2D(0, 0), target.Extent),
            ClearValueCount = 2,
            PClearValues = clearValues,
        };

        Vk.CmdBeginRenderPass(cmd, in beginInfo, SubpassContents.Inline);
        ApplyViewportAndScissor(target.Extent, ignoreTrim: false);
    }

    private void ApplyViewportAndScissor(Extent2D extent, bool ignoreTrim)
    {
        var cmd = CurrentCommandBuffer;
        var viewport = new Viewport(0, 0, extent.Width, extent.Height, 0f, 1f);
        Vk.CmdSetViewport(cmd, 0, 1, in viewport);

        if (ignoreTrim || TrimScissor is not { } trim)
        {
            var full = new Rect2D(new Offset2D(0, 0), extent);
            Vk.CmdSetScissor(cmd, 0, 1, in full);
        }
        else
        {
            Vk.CmdSetScissor(cmd, 0, 1, in trim);
        }
    }

    private void ApplyCurrentScissor()
    {
        if (!_frameActive)
            return;
        var cmd = CurrentCommandBuffer;
        if (TrimScissor is { } trim)
        {
            Vk.CmdSetScissor(cmd, 0, 1, in trim);
        }
        else
        {
            var full = new Rect2D(new Offset2D(0, 0), CurrentTargetExtent);
            Vk.CmdSetScissor(cmd, 0, 1, in full);
        }
    }

    private void FlushDeferredDestroys(int slot)
    {
        var list = _deferredDestroys[slot];
        if (list.Count == 0)
            return;
        foreach (var destroy in list)
            destroy();
        list.Clear();
    }

    // --- private: 初期化 ---
    private void CreateInstance(string appName)
    {
        var vk = Vk;

        var appNamePtr = (byte*)SilkMarshal.StringToPtr(appName);
        var engineNamePtr = (byte*)SilkMarshal.StringToPtr("Promete");

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = appNamePtr,
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = engineNamePtr,
            EngineVersion = new Version32(2, 0, 0),
            ApiVersion = Vk.Version12,
        };

        var surfaceExtensions = _window.VkSurface!.GetRequiredExtensions(out var extensionCount);

        var enabledLayers = GetAvailableValidationLayers();
        var layersPtr = enabledLayers.Length > 0
            ? (byte**)SilkMarshal.StringArrayToPtr(enabledLayers)
            : null;

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = extensionCount,
            PpEnabledExtensionNames = surfaceExtensions,
            EnabledLayerCount = (uint)enabledLayers.Length,
            PpEnabledLayerNames = layersPtr,
        };

        var result = vk.CreateInstance(in createInfo, null, out _instance);

        SilkMarshal.Free((nint)appNamePtr);
        SilkMarshal.Free((nint)engineNamePtr);
        if (layersPtr != null)
            SilkMarshal.Free((nint)layersPtr);

        ThrowIfFailed(result, "Vulkan インスタンスの作成");

        if (!vk.TryGetInstanceExtension(_instance, out _khrSurface))
            throw new InvalidOperationException("VK_KHR_surface 拡張が利用できません。");
    }

    private string[] GetAvailableValidationLayers()
    {
#if DEBUG
        const string validationLayerName = "VK_LAYER_KHRONOS_validation";

        var vk = Vk;
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var layers = new LayerProperties[layerCount];
        fixed (LayerProperties* p = layers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, p);
        }

        foreach (var layer in layers)
        {
            var name = SilkMarshal.PtrToString((nint)layer.LayerName);
            if (name == validationLayerName)
                return [validationLayerName];
        }
#endif
        return [];
    }

    private void CreateSurface()
    {
        _surface = _window
            .VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null)
            .ToSurface();
    }

    private void PickPhysicalDevice()
    {
        var vk = Vk;

        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);
        if (deviceCount == 0)
            throw new NotSupportedException("Vulkan をサポートする GPU が見つかりませんでした。");

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* p = devices)
        {
            vk.EnumeratePhysicalDevices(_instance, ref deviceCount, p);
        }

        // グラフィックスとプレゼントを両方サポートするキューファミリーを持つデバイスを選ぶ。
        // ディスクリート GPU を優先する。
        PhysicalDevice? fallback = null;
        uint fallbackQueueFamily = 0;

        foreach (var device in devices)
        {
            if (!TryFindQueueFamily(device, out var queueFamily))
                continue;
            if (!SupportsSwapchain(device))
                continue;

            vk.GetPhysicalDeviceProperties(device, out var props);
            if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
            {
                SelectPhysicalDevice(device, queueFamily);
                return;
            }

            if (fallback is null)
            {
                fallback = device;
                fallbackQueueFamily = queueFamily;
            }
        }

        if (fallback is null)
            throw new NotSupportedException("要件を満たす Vulkan デバイスが見つかりませんでした。");
        SelectPhysicalDevice(fallback.Value, fallbackQueueFamily);
    }

    private void SelectPhysicalDevice(PhysicalDevice device, uint queueFamily)
    {
        _physicalDevice = device;
        _queueFamilyIndex = queueFamily;
        Vk.GetPhysicalDeviceMemoryProperties(device, out _memoryProperties);
    }

    private bool TryFindQueueFamily(PhysicalDevice device, out uint queueFamilyIndex)
    {
        var vk = Vk;

        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* p = families)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, p);
        }

        for (uint i = 0; i < count; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.GraphicsBit) == 0)
                continue;

            _khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupported);
            if (!presentSupported)
                continue;

            queueFamilyIndex = i;
            return true;
        }

        queueFamilyIndex = 0;
        return false;
    }

    private bool SupportsSwapchain(PhysicalDevice device)
    {
        var vk = Vk;

        uint count = 0;
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, null);
        var extensions = new ExtensionProperties[count];
        fixed (ExtensionProperties* p = extensions)
        {
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref count, p);
        }

        foreach (var ext in extensions)
        {
            var name = SilkMarshal.PtrToString((nint)ext.ExtensionName);
            if (name == KhrSwapchain.ExtensionName)
                return true;
        }

        return false;
    }

    private void CreateLogicalDevice()
    {
        var vk = Vk;

        var queuePriority = 1f;
        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _queueFamilyIndex,
            QueueCount = 1,
            PQueuePriorities = &queuePriority,
        };

        var extensionsPtr = (byte**)SilkMarshal.StringArrayToPtr([KhrSwapchain.ExtensionName]);
        PhysicalDeviceFeatures features = default;

        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            EnabledExtensionCount = 1,
            PpEnabledExtensionNames = extensionsPtr,
            PEnabledFeatures = &features,
        };

        var result = vk.CreateDevice(_physicalDevice, in createInfo, null, out _device);
        SilkMarshal.Free((nint)extensionsPtr);
        ThrowIfFailed(result, "論理デバイスの作成");

        vk.GetDeviceQueue(_device, _queueFamilyIndex, 0, out _graphicsQueue);

        if (!vk.TryGetDeviceExtension(_instance, _device, out _khrSwapchain))
            throw new InvalidOperationException("VK_KHR_swapchain 拡張が利用できません。");
    }

    private void CreateSwapchain()
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out var caps);

        var format = ChooseSurfaceFormat();
        var presentMode = ChoosePresentMode();
        var extent = ChooseExtent(caps);

        var imageCount = caps.MinImageCount + 1;
        if (caps.MaxImageCount > 0 && imageCount > caps.MaxImageCount)
            imageCount = caps.MaxImageCount;

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = caps.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
        };

        ThrowIfFailed(
            _khrSwapchain.CreateSwapchain(_device, in createInfo, null, out _swapchain),
            "スワップチェーンの作成"
        );

        _swapchainFormat = format.Format;
        _swapchainExtent = extent;

        uint actualImageCount = 0;
        _khrSwapchain.GetSwapchainImages(_device, _swapchain, ref actualImageCount, null);
        _swapchainImages = new Image[actualImageCount];
        fixed (Image* p = _swapchainImages)
        {
            _khrSwapchain.GetSwapchainImages(_device, _swapchain, ref actualImageCount, p);
        }
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref count, null);
        var formats = new SurfaceFormatKHR[count];
        fixed (SurfaceFormatKHR* p = formats)
        {
            _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref count, p);
        }

        foreach (var f in formats)
        {
            if (f.Format == Format.B8G8R8A8Unorm && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                return f;
        }

        return formats[0];
    }

    private PresentModeKHR ChoosePresentMode()
    {
        // VSync 有効時、または非対応環境では FIFO（常にサポートされる）
        if (_window.VSync)
            return PresentModeKHR.FifoKhr;

        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref count, null);
        var modes = new PresentModeKHR[count];
        fixed (PresentModeKHR* p = modes)
        {
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref count, p);
        }

        foreach (var mode in modes)
        {
            if (mode == PresentModeKHR.MailboxKhr)
                return mode;
        }

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseExtent(SurfaceCapabilitiesKHR caps)
    {
        if (caps.CurrentExtent.Width != uint.MaxValue)
            return caps.CurrentExtent;

        var fb = _window.FramebufferSize;
        return new Extent2D(
            Math.Clamp((uint)fb.X, caps.MinImageExtent.Width, caps.MaxImageExtent.Width),
            Math.Clamp((uint)fb.Y, caps.MinImageExtent.Height, caps.MaxImageExtent.Height)
        );
    }

    private void CreateImageViews()
    {
        _swapchainImageViews = new ImageView[_swapchainImages.Length];
        for (var i = 0; i < _swapchainImages.Length; i++)
            _swapchainImageViews[i] = CreateImageView2D(_swapchainImages[i], _swapchainFormat);
    }

    private void ChooseStencilFormat()
    {
        // 環境によってサポートが異なるため、利用可能なステンシル付きフォーマットを選択する
        Span<Format> candidates = [Format.D24UnormS8Uint, Format.D32SfloatS8Uint];
        foreach (var format in candidates)
        {
            Vk.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out var props);
            if ((props.OptimalTilingFeatures & FormatFeatureFlags.DepthStencilAttachmentBit) != 0)
            {
                StencilFormat = format;
                return;
            }
        }

        throw new NotSupportedException("ステンシルアタッチメントに使用できるフォーマットがありません。");
    }

    private void CreateRenderPasses()
    {
        _swapchainPass = CreateSwapchainRenderPass();
        _offscreenClearPass = CreateOffscreenRenderPass(clear: true);
        _offscreenLoadPass = CreateOffscreenRenderPass(clear: false);
    }

    private RenderPass CreateSwapchainRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = _swapchainFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        var colorRef = new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal);

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorRef,
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit,
        };

        var createInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency,
        };

        ThrowIfFailed(
            Vk.CreateRenderPass(_device, in createInfo, null, out var renderPass),
            "レンダーパスの作成"
        );
        return renderPass;
    }

    private RenderPass CreateOffscreenRenderPass(bool clear)
    {
        var attachments = stackalloc AttachmentDescription[2];

        // カラーアタッチメント (サンプリングを単純化するため General レイアウトを維持)
        attachments[0] = new AttachmentDescription
        {
            Format = OffscreenFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = clear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.General,
            FinalLayout = ImageLayout.General,
        };

        // ステンシルアタッチメント (パス中断/再開をまたいで保持するため Load/Store)
        attachments[1] = new AttachmentDescription
        {
            Format = StencilFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.DontCare,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = clear ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
            StencilStoreOp = AttachmentStoreOp.Store,
            InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        var colorRef = new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal);
        var stencilRef = new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal);

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorRef,
            PDepthStencilAttachment = &stencilRef,
        };

        const PipelineStageFlags stencilStages =
            PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit;

        // 開始依存: 前段の描画/読み取り完了を待つ
        // 終了依存: パス完了後のフラグメントシェーダー/転送からの読み取りを同期する
        var dependencies = stackalloc SubpassDependency[2];
        dependencies[0] = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask =
                PipelineStageFlags.ColorAttachmentOutputBit
                | PipelineStageFlags.FragmentShaderBit
                | stencilStages,
            SrcAccessMask =
                AccessFlags.ColorAttachmentWriteBit
                | AccessFlags.ShaderReadBit
                | AccessFlags.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | stencilStages,
            DstAccessMask =
                AccessFlags.ColorAttachmentWriteBit
                | AccessFlags.ColorAttachmentReadBit
                | AccessFlags.DepthStencilAttachmentWriteBit
                | AccessFlags.DepthStencilAttachmentReadBit,
        };
        dependencies[1] = new SubpassDependency
        {
            SrcSubpass = 0,
            DstSubpass = Vk.SubpassExternal,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | stencilStages,
            SrcAccessMask =
                AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags.FragmentShaderBit | PipelineStageFlags.TransferBit,
            DstAccessMask = AccessFlags.ShaderReadBit | AccessFlags.TransferReadBit,
        };

        var createInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 2,
            PDependencies = dependencies,
        };

        ThrowIfFailed(
            Vk.CreateRenderPass(_device, in createInfo, null, out var renderPass),
            "レンダーパスの作成"
        );
        return renderPass;
    }

    private void CreateFramebuffers()
    {
        _framebuffers = new Framebuffer[_swapchainImageViews.Length];

        for (var i = 0; i < _swapchainImageViews.Length; i++)
        {
            var attachment = _swapchainImageViews[i];
            var createInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _swapchainPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = _swapchainExtent.Width,
                Height = _swapchainExtent.Height,
                Layers = 1,
            };

            ThrowIfFailed(
                Vk.CreateFramebuffer(_device, in createInfo, null, out _framebuffers[i]),
                "フレームバッファの作成"
            );
        }
    }

    private void CreateCommandPools()
    {
        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = _queueFamilyIndex,
        };
        ThrowIfFailed(
            Vk.CreateCommandPool(_device, in createInfo, null, out _commandPool),
            "コマンドプールの作成"
        );

        var transientInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.TransientBit,
            QueueFamilyIndex = _queueFamilyIndex,
        };
        ThrowIfFailed(
            Vk.CreateCommandPool(_device, in transientInfo, null, out _transientPool),
            "コマンドプールの作成"
        );
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[FramesInFlight];

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = FramesInFlight,
        };

        fixed (CommandBuffer* p = _commandBuffers)
        {
            ThrowIfFailed(Vk.AllocateCommandBuffers(_device, in allocInfo, p), "コマンドバッファの確保");
        }
    }

    private void CreateSyncObjects()
    {
        var vk = Vk;
        _imageAvailableSemaphores = new Semaphore[FramesInFlight];
        _renderFinishedSemaphores = new Semaphore[FramesInFlight];
        _inFlightFences = new Fence[FramesInFlight];

        var semaphoreInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < FramesInFlight; i++)
        {
            ThrowIfFailed(
                vk.CreateSemaphore(_device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]),
                "セマフォの作成"
            );
            ThrowIfFailed(
                vk.CreateSemaphore(_device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]),
                "セマフォの作成"
            );
            ThrowIfFailed(
                vk.CreateFence(_device, in fenceInfo, null, out _inFlightFences[i]),
                "フェンスの作成"
            );
        }
    }

    // --- private: スワップチェーン再構築 ---
    private void RecreateSwapchain()
    {
        var fb = _window.FramebufferSize;
        if (fb.X <= 0 || fb.Y <= 0)
            return;

        Vk.DeviceWaitIdle(_device);
        CleanupSwapchain();

        CreateSwapchain();
        CreateImageViews();
        CreateFramebuffers();
    }

    private void CleanupSwapchain()
    {
        var vk = Vk;

        foreach (var framebuffer in _framebuffers)
            vk.DestroyFramebuffer(_device, framebuffer, null);
        foreach (var view in _swapchainImageViews)
            vk.DestroyImageView(_device, view, null);

        _khrSwapchain.DestroySwapchain(_device, _swapchain, null);

        _framebuffers = [];
        _swapchainImageViews = [];
        _swapchainImages = [];
    }
}
