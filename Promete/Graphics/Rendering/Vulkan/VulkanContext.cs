using System;
using System.Collections.Generic;
using System.Drawing;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan のインスタンス・デバイス・スワップチェーン・フレーム同期を管理するコンテキストです。
/// Vulkan バックエンドの中核となる低レベルオブジェクトを保持します。
/// </summary>
internal sealed unsafe class VulkanContext : IDisposable
{
    private const int MaxFramesInFlight = 2;

    private readonly IWindow _window;

    private KhrSurface _khrSurface = null!;
    private KhrSwapchain _khrSwapchain = null!;

    private Instance _instance;
    private SurfaceKHR _surface;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private uint _queueFamilyIndex;
    private Queue _graphicsQueue;

    private SwapchainKHR _swapchain;
    private Format _swapchainFormat;
    private Extent2D _swapchainExtent;
    private Image[] _swapchainImages = [];
    private ImageView[] _swapchainImageViews = [];
    private Framebuffer[] _framebuffers = [];

    private RenderPass _renderPass;
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = [];

    private Semaphore[] _imageAvailableSemaphores = [];
    private Semaphore[] _renderFinishedSemaphores = [];
    private Fence[] _inFlightFences = [];

    private int _currentFrame;
    private bool _framebufferResized;
    private bool _disposed;

    public VulkanContext(IWindow window)
    {
        _window = window;
        _window.FramebufferResize += _ => _framebufferResized = true;
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
        CreateRenderPass();
        CreateFramebuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    /// <summary>
    /// 1 フレームを描画します。現状はクリアカラーで塗りつぶすのみです。
    /// TODO: Phase 3 でレンダリングコマンドキューの実行結果をここに統合する。
    /// </summary>
    public void DrawFrame(Color clearColor)
    {
        // 最小化中などフレームバッファサイズが 0 の間は描画しない
        var fb = _window.FramebufferSize;
        if (fb.X <= 0 || fb.Y <= 0)
            return;

        var vk = Vk;
        var fence = _inFlightFences[_currentFrame];
        vk.WaitForFences(_device, 1, in fence, true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = _khrSwapchain.AcquireNextImage(
            _device,
            _swapchain,
            ulong.MaxValue,
            _imageAvailableSemaphores[_currentFrame],
            default,
            ref imageIndex
        );

        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return;
        }

        if (result != Result.Success && result != Result.SuboptimalKhr)
            throw new InvalidOperationException($"スワップチェーンイメージの取得に失敗しました: {result}");

        vk.ResetFences(_device, 1, in fence);

        var cmd = _commandBuffers[_currentFrame];
        vk.ResetCommandBuffer(cmd, 0);
        RecordCommandBuffer(cmd, imageIndex, clearColor);

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

        ThrowIfFailed(vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, fence), "キューの送信");

        var swapchain = _swapchain;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
        };

        result = _khrSwapchain.QueuePresent(_graphicsQueue, in presentInfo);

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr || _framebufferResized)
        {
            _framebufferResized = false;
            RecreateSwapchain();
        }
        else if (result != Result.Success)
        {
            throw new InvalidOperationException($"プレゼントに失敗しました: {result}");
        }

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var vk = Vk;
        vk.DeviceWaitIdle(_device);

        CleanupSwapchain();

        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            vk.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
            vk.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
            vk.DestroyFence(_device, _inFlightFences[i], null);
        }

        vk.DestroyCommandPool(_device, _commandPool, null);
        vk.DestroyRenderPass(_device, _renderPass, null);
        vk.DestroyDevice(_device, null);
        _khrSurface.DestroySurface(_instance, _surface, null);
        vk.DestroyInstance(_instance, null);

        _khrSwapchain.Dispose();
        _khrSurface.Dispose();
        vk.Dispose();
    }

    private static void ThrowIfFailed(Result result, string operation)
    {
        if (result != Result.Success)
            throw new InvalidOperationException($"{operation}に失敗しました: {result}");
    }

    // --- 初期化 ---
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
                _physicalDevice = device;
                _queueFamilyIndex = queueFamily;
                return;
            }

            fallback ??= device;
            if (fallback.Value.Handle == device.Handle)
                fallbackQueueFamily = queueFamily;
        }

        _physicalDevice =
            fallback
            ?? throw new NotSupportedException("要件を満たす Vulkan デバイスが見つかりませんでした。");
        _queueFamilyIndex = fallbackQueueFamily;
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
        var vk = Vk;
        _swapchainImageViews = new ImageView[_swapchainImages.Length];

        for (var i = 0; i < _swapchainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapchainFormat,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
            };

            ThrowIfFailed(
                vk.CreateImageView(_device, in createInfo, null, out _swapchainImageViews[i]),
                "イメージビューの作成"
            );
        }
    }

    private void CreateRenderPass()
    {
        var vk = Vk;

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
            vk.CreateRenderPass(_device, in createInfo, null, out _renderPass),
            "レンダーパスの作成"
        );
    }

    private void CreateFramebuffers()
    {
        var vk = Vk;
        _framebuffers = new Framebuffer[_swapchainImageViews.Length];

        for (var i = 0; i < _swapchainImageViews.Length; i++)
        {
            var attachment = _swapchainImageViews[i];
            var createInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = _swapchainExtent.Width,
                Height = _swapchainExtent.Height,
                Layers = 1,
            };

            ThrowIfFailed(
                vk.CreateFramebuffer(_device, in createInfo, null, out _framebuffers[i]),
                "フレームバッファの作成"
            );
        }
    }

    private void CreateCommandPool()
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
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[MaxFramesInFlight];

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = MaxFramesInFlight,
        };

        fixed (CommandBuffer* p = _commandBuffers)
        {
            ThrowIfFailed(Vk.AllocateCommandBuffers(_device, in allocInfo, p), "コマンドバッファの確保");
        }
    }

    private void CreateSyncObjects()
    {
        var vk = Vk;
        _imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
        _inFlightFences = new Fence[MaxFramesInFlight];

        var semaphoreInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MaxFramesInFlight; i++)
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

    // --- フレーム描画 ---
    private void RecordCommandBuffer(CommandBuffer cmd, uint imageIndex, Color clearColor)
    {
        var vk = Vk;

        var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
        ThrowIfFailed(vk.BeginCommandBuffer(cmd, in beginInfo), "コマンドバッファの記録開始");

        var clearValue = new ClearValue(
            new ClearColorValue(
                clearColor.R / 255f,
                clearColor.G / 255f,
                clearColor.B / 255f,
                clearColor.A / 255f
            )
        );

        var renderPassBegin = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass,
            Framebuffer = _framebuffers[imageIndex],
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchainExtent),
            ClearValueCount = 1,
            PClearValues = &clearValue,
        };

        vk.CmdBeginRenderPass(cmd, in renderPassBegin, SubpassContents.Inline);

        // TODO: Phase 3 でここに描画コマンドを記録する
        vk.CmdEndRenderPass(cmd);
        ThrowIfFailed(vk.EndCommandBuffer(cmd), "コマンドバッファの記録終了");
    }

    // --- スワップチェーン再構築 ---
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
