using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Promete.Backends.Vulkan;
using Promete.Graphics.Rendering.Vulkan;
using Silk.NET.Input;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace Promete.ImGui;

/// <summary>
/// Vulkan バックエンド用の ImGui コントローラです。
/// 入力処理と、スワップチェーンパスへの ImGui 描画データのレンダリングを行います。
/// </summary>
internal sealed unsafe class VulkanImGuiController : IImGuiController
{
    private const string VertexShaderSource = """
        #version 450
        layout(location = 0) in vec2 aPos;
        layout(location = 1) in vec2 aUV;
        layout(location = 2) in vec4 aColor;

        layout(push_constant) uniform PushConstants
        {
            vec2 uScale;
            vec2 uTranslate;
        };

        layout(location = 0) out vec2 fUv;
        layout(location = 1) out vec4 fColor;

        void main()
        {
            fUv = aUV;
            fColor = aColor;
            gl_Position = vec4(aPos * uScale + uTranslate, 0.0, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 450
        layout(location = 0) in vec2 fUv;
        layout(location = 1) in vec4 fColor;

        layout(set = 0, binding = 0) uniform sampler2D sTexture;

        layout(location = 0) out vec4 FragColor;

        void main()
        {
            FragColor = fColor * texture(sTexture, fUv);
        }
        """;

    private readonly VulkanDesktopGameView _view;
    private readonly VulkanContext _ctx;
    private readonly VulkanResourceManager _resources;
    private readonly IInputContext _input;
    private readonly IKeyboard? _keyboard;
    private readonly IMouse? _mouse;
    private readonly nint _imguiContext;
    private readonly FrameBuffers[] _frames;

    private PipelineLayout _pipelineLayout;
    private Pipeline _pipeline;
    private int _fontTextureId;
    private bool _disposed;

    public VulkanImGuiController(VulkanDesktopGameView view, IInputContext input, Action onConfigure)
    {
        _view = view;
        _ctx =
            view.RenderingContext
            ?? throw new InvalidOperationException("Vulkan コンテキストが初期化されていません。");
        _resources =
            view.RenderingResources
            ?? throw new InvalidOperationException("リソースマネージャが初期化されていません。");
        _input = input;
        _keyboard = input.Keyboards.Count > 0 ? input.Keyboards[0] : null;
        _mouse = input.Mice.Count > 0 ? input.Mice[0] : null;

        _imguiContext = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        var io = ImGuiNET.ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.Fonts.AddFontDefault();

        onConfigure();

        CreateFontTexture(io);
        CreatePipeline();

        _frames = new FrameBuffers[VulkanContext.FramesInFlight];
        for (var i = 0; i < _frames.Length; i++)
            _frames[i] = new FrameBuffers();

        SubscribeInput();
    }

    public void Update(float deltaTime)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        var io = ImGuiNET.ImGui.GetIO();

        var window = _view.NativeWindow;
        var size = window.Size;
        var framebuffer = window.FramebufferSize;
        io.DisplaySize = new Vector2(size.X, size.Y);
        if (size.X > 0 && size.Y > 0)
            io.DisplayFramebufferScale = new Vector2(
                framebuffer.X / (float)size.X,
                framebuffer.Y / (float)size.Y
            );

        io.DeltaTime = deltaTime > 0 ? deltaTime : 1f / 60f;

        if (_mouse is not null)
        {
            io.MousePos = _mouse.Position;
            io.MouseDown[0] = _mouse.IsButtonPressed(MouseButton.Left);
            io.MouseDown[1] = _mouse.IsButtonPressed(MouseButton.Right);
            io.MouseDown[2] = _mouse.IsButtonPressed(MouseButton.Middle);
        }

        ImGuiNET.ImGui.NewFrame();
    }

    public void Render()
    {
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        ImGuiNET.ImGui.Render();

        if (!_ctx.IsFrameActive)
            return;

        RenderDrawData(ImGuiNET.ImGui.GetDrawData());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _ctx.WaitIdle();

        var vk = _ctx.Vk;
        var device = _ctx.Device;
        foreach (var frame in _frames)
            frame.Dispose(_ctx);
        vk.DestroyPipeline(device, _pipeline, null);
        vk.DestroyPipelineLayout(device, _pipelineLayout, null);
        _resources.Destroy(_fontTextureId);

        ImGuiNET.ImGui.DestroyContext(_imguiContext);
    }

    // --- 初期化 ---
    private void CreateFontTexture(ImGuiIOPtr io)
    {
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out _);
        var span = new ReadOnlySpan<byte>((void*)pixels, width * height * 4);
        _fontTextureId = _resources.CreateTexture(span, (uint)width, (uint)height);
        io.Fonts.SetTexID(_fontTextureId);
        io.Fonts.ClearTexData();
    }

    private void CreatePipeline()
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;

        // レイアウト: set0 = combined sampler, push constant = vec2 scale + vec2 translate
        var setLayout = _resources.TextureSetLayout;
        var pushConstant = new PushConstantRange(ShaderStageFlags.VertexBit, 0, 16);
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstant,
        };
        vk.CreatePipelineLayout(device, in layoutInfo, null, out _pipelineLayout);

        // シェーダー
        using var compiler = new VulkanShaderCompiler();
        var vertSpv = compiler.Compile(VertexShaderSource, ShaderKind.VertexShader, "imgui.vert");
        var fragSpv = compiler.Compile(FragmentShaderSource, ShaderKind.FragmentShader, "imgui.frag");
        var vertModule = CreateShaderModule(vertSpv);
        var fragModule = CreateShaderModule(fragSpv);

        var entryPoint = (byte*)Silk.NET.Core.Native.SilkMarshal.StringToPtr("main");
        var stages = stackalloc PipelineShaderStageCreateInfo[2]
        {
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertModule,
                PName = entryPoint,
            },
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragModule,
                PName = entryPoint,
            },
        };

        // 頂点レイアウト: ImDrawVert (pos2 + uv2 + col u8x4)
        var binding = new VertexInputBindingDescription(0, 20, VertexInputRate.Vertex);
        var attributes = stackalloc VertexInputAttributeDescription[3]
        {
            new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0),
            new VertexInputAttributeDescription(1, 0, Format.R32G32Sfloat, 8),
            new VertexInputAttributeDescription(2, 0, Format.R8G8B8A8Unorm, 16),
        };
        var vertexInput = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 1,
            PVertexBindingDescriptions = &binding,
            VertexAttributeDescriptionCount = 3,
            PVertexAttributeDescriptions = attributes,
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
        };

        var rasterization = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            PolygonMode = PolygonMode.Fill,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.Clockwise,
            LineWidth = 1f,
        };

        var multisample = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        var blendAttachment = new PipelineColorBlendAttachmentState
        {
            BlendEnable = true,
            SrcColorBlendFactor = BlendFactor.SrcAlpha,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.One,
            DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = BlendOp.Add,
            ColorWriteMask =
                ColorComponentFlags.RBit
                | ColorComponentFlags.GBit
                | ColorComponentFlags.BBit
                | ColorComponentFlags.ABit,
        };
        var colorBlend = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            AttachmentCount = 1,
            PAttachments = &blendAttachment,
        };

        var dynamicStates = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };
        var dynamicState = new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 2,
            PDynamicStates = dynamicStates,
        };

        var createInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = stages,
            PVertexInputState = &vertexInput,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterization,
            PMultisampleState = &multisample,
            PColorBlendState = &colorBlend,
            PDynamicState = &dynamicState,
            Layout = _pipelineLayout,
            RenderPass = _ctx.SwapchainPass,
            Subpass = 0,
        };

        var result = vk.CreateGraphicsPipelines(device, default, 1, in createInfo, null, out _pipeline);

        Silk.NET.Core.Native.SilkMarshal.Free((nint)entryPoint);
        vk.DestroyShaderModule(device, vertModule, null);
        vk.DestroyShaderModule(device, fragModule, null);

        if (result != Result.Success)
            throw new InvalidOperationException($"ImGui パイプラインの作成に失敗しました: {result}");
    }

    private ShaderModule CreateShaderModule(byte[] spirv)
    {
        fixed (byte* code = spirv)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spirv.Length,
                PCode = (uint*)code,
            };
            var result = _ctx.Vk.CreateShaderModule(_ctx.Device, in createInfo, null, out var module);
            if (result != Result.Success)
                throw new InvalidOperationException($"シェーダーモジュールの作成に失敗しました: {result}");
            return module;
        }
    }

    // --- 入力 ---
    private void SubscribeInput()
    {
        if (_keyboard is not null)
        {
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
            _keyboard.KeyChar += OnKeyChar;
        }

        if (_mouse is not null)
            _mouse.Scroll += OnScroll;
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode) => OnKey(key, true);

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode) => OnKey(key, false);

    private void OnKey(Key key, bool down)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        var io = ImGuiNET.ImGui.GetIO();

        var imguiKey = TranslateKey(key);
        if (imguiKey != ImGuiKey.None)
            io.AddKeyEvent(imguiKey, down);

        // 修飾キー
        switch (key)
        {
            case Key.ControlLeft or Key.ControlRight:
                io.AddKeyEvent(ImGuiKey.ModCtrl, down);
                break;
            case Key.ShiftLeft or Key.ShiftRight:
                io.AddKeyEvent(ImGuiKey.ModShift, down);
                break;
            case Key.AltLeft or Key.AltRight:
                io.AddKeyEvent(ImGuiKey.ModAlt, down);
                break;
            case Key.SuperLeft or Key.SuperRight:
                io.AddKeyEvent(ImGuiKey.ModSuper, down);
                break;
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        ImGuiNET.ImGui.GetIO().AddInputCharacter(c);
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imguiContext);
        ImGuiNET.ImGui.GetIO().AddMouseWheelEvent(wheel.X, wheel.Y);
    }

    private static ImGuiKey TranslateKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
            return ImGuiKey.A + (key - Key.A);
        if (key is >= Key.Number0 and <= Key.Number9)
            return ImGuiKey._0 + (key - Key.Number0);
        if (key is >= Key.F1 and <= Key.F12)
            return ImGuiKey.F1 + (key - Key.F1);
        if (key is >= Key.Keypad0 and <= Key.Keypad9)
            return ImGuiKey.Keypad0 + (key - Key.Keypad0);

        return key switch
        {
            Key.Tab => ImGuiKey.Tab,
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.PageUp => ImGuiKey.PageUp,
            Key.PageDown => ImGuiKey.PageDown,
            Key.Home => ImGuiKey.Home,
            Key.End => ImGuiKey.End,
            Key.Insert => ImGuiKey.Insert,
            Key.Delete => ImGuiKey.Delete,
            Key.Backspace => ImGuiKey.Backspace,
            Key.Space => ImGuiKey.Space,
            Key.Enter => ImGuiKey.Enter,
            Key.Escape => ImGuiKey.Escape,
            Key.Apostrophe => ImGuiKey.Apostrophe,
            Key.Comma => ImGuiKey.Comma,
            Key.Minus => ImGuiKey.Minus,
            Key.Period => ImGuiKey.Period,
            Key.Slash => ImGuiKey.Slash,
            Key.Semicolon => ImGuiKey.Semicolon,
            Key.Equal => ImGuiKey.Equal,
            Key.LeftBracket => ImGuiKey.LeftBracket,
            Key.BackSlash => ImGuiKey.Backslash,
            Key.RightBracket => ImGuiKey.RightBracket,
            Key.GraveAccent => ImGuiKey.GraveAccent,
            Key.CapsLock => ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiKey.ScrollLock,
            Key.NumLock => ImGuiKey.NumLock,
            Key.PrintScreen => ImGuiKey.PrintScreen,
            Key.Pause => ImGuiKey.Pause,
            Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
            Key.KeypadDivide => ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiKey.KeypadAdd,
            Key.KeypadEnter => ImGuiKey.KeypadEnter,
            Key.KeypadEqual => ImGuiKey.KeypadEqual,
            Key.ControlLeft => ImGuiKey.LeftCtrl,
            Key.ShiftLeft => ImGuiKey.LeftShift,
            Key.AltLeft => ImGuiKey.LeftAlt,
            Key.SuperLeft => ImGuiKey.LeftSuper,
            Key.ControlRight => ImGuiKey.RightCtrl,
            Key.ShiftRight => ImGuiKey.RightShift,
            Key.AltRight => ImGuiKey.RightAlt,
            Key.SuperRight => ImGuiKey.RightSuper,
            Key.Menu => ImGuiKey.Menu,
            _ => ImGuiKey.None,
        };
    }

    // --- レンダリング ---
    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        var framebufferWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        var framebufferHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (framebufferWidth <= 0 || framebufferHeight <= 0 || drawData.CmdListsCount == 0)
            return;

        var vk = _ctx.Vk;
        var cmd = _ctx.CurrentCommandBuffer;
        var frame = _frames[_ctx.FrameIndex];

        // 頂点・インデックスデータを転送
        var vertexSize = (ulong)(drawData.TotalVtxCount * sizeof(ImDrawVert));
        var indexSize = (ulong)(drawData.TotalIdxCount * sizeof(ushort));
        if (vertexSize == 0 || indexSize == 0)
            return;

        frame.EnsureCapacity(_ctx, vertexSize, indexSize);

        var vtxOffsetBytes = 0ul;
        var idxOffsetBytes = 0ul;
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = new ImDrawListPtr(((ImDrawList**)drawData.NativePtr->CmdLists.Data)[i]);
            var listVtxSize = (ulong)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            var listIdxSize = (ulong)(cmdList.IdxBuffer.Size * sizeof(ushort));
            System.Buffer.MemoryCopy(
                (void*)cmdList.VtxBuffer.Data,
                frame.VertexMapped + vtxOffsetBytes,
                listVtxSize,
                listVtxSize
            );
            System.Buffer.MemoryCopy(
                (void*)cmdList.IdxBuffer.Data,
                frame.IndexMapped + idxOffsetBytes,
                listIdxSize,
                listIdxSize
            );
            vtxOffsetBytes += listVtxSize;
            idxOffsetBytes += listIdxSize;
        }

        // パイプラインをバインドし、描画設定を行う
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline);

        var offset = 0ul;
        var vertexBuffer = frame.VertexBuffer;
        vk.CmdBindVertexBuffers(cmd, 0, 1, in vertexBuffer, in offset);
        vk.CmdBindIndexBuffer(cmd, frame.IndexBuffer, 0, IndexType.Uint16);

        var viewport = new Viewport(0, 0, framebufferWidth, framebufferHeight, 0f, 1f);
        vk.CmdSetViewport(cmd, 0, 1, in viewport);

        // 射影: スケールと平行移動を push constant で渡す
        var pushData = stackalloc float[4]
        {
            2f / drawData.DisplaySize.X,
            2f / drawData.DisplaySize.Y,
            -1f - (drawData.DisplayPos.X * (2f / drawData.DisplaySize.X)),
            -1f - (drawData.DisplayPos.Y * (2f / drawData.DisplaySize.Y)),
        };
        vk.CmdPushConstants(cmd, _pipelineLayout, ShaderStageFlags.VertexBit, 0, 16, pushData);

        // 描画コマンドを実行
        var globalVtxOffset = 0;
        var globalIdxOffset = 0;
        var clipOffset = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = new ImDrawListPtr(((ImDrawList**)drawData.NativePtr->CmdLists.Data)[i]);
            for (var j = 0; j < cmdList.CmdBuffer.Size; j++)
            {
                var drawCmd = cmdList.CmdBuffer[j];

                var clipMin = new Vector2(
                    (drawCmd.ClipRect.X - clipOffset.X) * clipScale.X,
                    (drawCmd.ClipRect.Y - clipOffset.Y) * clipScale.Y
                );
                var clipMax = new Vector2(
                    (drawCmd.ClipRect.Z - clipOffset.X) * clipScale.X,
                    (drawCmd.ClipRect.W - clipOffset.Y) * clipScale.Y
                );
                clipMin = Vector2.Max(clipMin, Vector2.Zero);
                clipMax = Vector2.Min(clipMax, new Vector2(framebufferWidth, framebufferHeight));
                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                var scissor = new Rect2D(
                    new Offset2D((int)clipMin.X, (int)clipMin.Y),
                    new Extent2D((uint)(clipMax.X - clipMin.X), (uint)(clipMax.Y - clipMin.Y))
                );
                vk.CmdSetScissor(cmd, 0, 1, in scissor);

                // TexID は Promete のテクスチャ ID (VulkanResourceManager)
                var textureId = (int)drawCmd.TextureId;
                if (!_resources.Contains(textureId))
                    continue;
                var descriptorSet = _resources.GetDescriptorSet(textureId);
                vk.CmdBindDescriptorSets(
                    cmd,
                    PipelineBindPoint.Graphics,
                    _pipelineLayout,
                    0,
                    1,
                    in descriptorSet,
                    0,
                    null
                );

                vk.CmdDrawIndexed(
                    cmd,
                    drawCmd.ElemCount,
                    1,
                    (uint)(globalIdxOffset + drawCmd.IdxOffset),
                    (int)(globalVtxOffset + drawCmd.VtxOffset),
                    0
                );
            }

            globalIdxOffset += cmdList.IdxBuffer.Size;
            globalVtxOffset += cmdList.VtxBuffer.Size;
        }
    }

    /// <summary>
    /// フレームスロットごとの頂点・インデックスバッファです。容量不足時に再確保します。
    /// </summary>
    private sealed class FrameBuffers
    {
        private DeviceMemory _vertexMemory;
        private DeviceMemory _indexMemory;
        private ulong _vertexCapacity;
        private ulong _indexCapacity;

        public VkBuffer VertexBuffer { get; private set; }

        public VkBuffer IndexBuffer { get; private set; }

        public byte* VertexMapped { get; private set; }

        public byte* IndexMapped { get; private set; }

        public void EnsureCapacity(VulkanContext ctx, ulong vertexSize, ulong indexSize)
        {
            // このスロットのフェンスは BeginFrame で待機済みのため、旧バッファは即時破棄できる
            if (_vertexCapacity < vertexSize)
            {
                DestroyBuffer(ctx, VertexBuffer, _vertexMemory);
                _vertexCapacity = Math.Max(vertexSize, 64 * 1024);
                CreateBuffer(
                    ctx,
                    _vertexCapacity,
                    BufferUsageFlags.VertexBufferBit,
                    out var buffer,
                    out _vertexMemory,
                    out var mapped
                );
                VertexBuffer = buffer;
                VertexMapped = mapped;
            }

            if (_indexCapacity < indexSize)
            {
                DestroyBuffer(ctx, IndexBuffer, _indexMemory);
                _indexCapacity = Math.Max(indexSize, 16 * 1024);
                CreateBuffer(
                    ctx,
                    _indexCapacity,
                    BufferUsageFlags.IndexBufferBit,
                    out var buffer,
                    out _indexMemory,
                    out var mapped
                );
                IndexBuffer = buffer;
                IndexMapped = mapped;
            }
        }

        public void Dispose(VulkanContext ctx)
        {
            DestroyBuffer(ctx, VertexBuffer, _vertexMemory);
            DestroyBuffer(ctx, IndexBuffer, _indexMemory);
            VertexBuffer = default;
            IndexBuffer = default;
        }

        private static void CreateBuffer(
            VulkanContext ctx,
            ulong size,
            BufferUsageFlags usage,
            out VkBuffer buffer,
            out DeviceMemory memory,
            out byte* mapped
        )
        {
            (buffer, memory) = ctx.CreateBuffer(
                size,
                usage,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
            );
            void* p;
            ctx.Vk.MapMemory(ctx.Device, memory, 0, size, 0, &p);
            mapped = (byte*)p;
        }

        private static void DestroyBuffer(VulkanContext ctx, VkBuffer buffer, DeviceMemory memory)
        {
            if (buffer.Handle == 0)
                return;
            ctx.Vk.DestroyBuffer(ctx.Device, buffer, null);
            ctx.Vk.FreeMemory(ctx.Device, memory, null);
        }
    }
}
