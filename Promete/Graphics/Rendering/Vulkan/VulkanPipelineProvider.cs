using System;
using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// 描画パイプラインの遅延生成とキャッシュを担います。
/// レンダーパス互換性により、パイプラインは「オフスクリーン用」「スワップチェーン用」の 2 系統をキャッシュします。
/// </summary>
internal sealed unsafe class VulkanPipelineProvider : IDisposable
{
    private readonly VulkanContext _ctx;
    private readonly VulkanResourceManager _resources;
    private readonly VulkanShaderManager _shaders;
    private readonly VulkanMaterialSystem _materials;
    private readonly VulkanShaderCompiler _compiler = new();
    private readonly Dictionary<
        (PipelineKind Kind, PassClass Pass, PrimitiveTopology Topology, StencilMode Stencil),
        Pipeline
    > _cache = [];
    private readonly Dictionary<
        (int ShaderId, CustomKind Kind, PassClass Pass, StencilMode Stencil),
        Pipeline
    > _customCache = [];

    private PipelineLayout _textureLayout;
    private PipelineLayout _primitiveLayout;
    private PipelineLayout _blitLayout;
    private PipelineLayout _pieLayout;
    private PipelineLayout _customSpriteLayout;
    private PipelineLayout _customBlitLayout;
    private PipelineLayout _maskedLayout;
    private PipelineLayout _stencilWriteLayout;
    private bool _initialized;
    private bool _disposed;

    public VulkanPipelineProvider(
        VulkanContext ctx,
        VulkanResourceManager resources,
        VulkanShaderManager shaders,
        VulkanMaterialSystem materials
    )
    {
        _ctx = ctx;
        _resources = resources;
        _shaders = shaders;
        _materials = materials;
    }

    /// <summary>描画先レンダーパスの系統。</summary>
    public enum PassClass
    {
        Offscreen,
        Swapchain,
    }

    /// <summary>カスタムシェーダーパイプラインの種別。</summary>
    public enum CustomKind
    {
        /// <summary>インスタンシングスプライト用 (texture_instanced 互換の頂点レイアウト)。</summary>
        Sprite,

        /// <summary>フルスクリーンブリット用 (頂点入力なし)。</summary>
        Blit,
    }

    /// <summary>ステンシルの動作モード。</summary>
    public enum StencilMode
    {
        /// <summary>ステンシルテスト無効。</summary>
        None,

        /// <summary>マスク書き込み (Always/Replace, ref=1, カラー書き込みなし)。</summary>
        WriteMask,

        /// <summary>マスク適用 (Equal, ref=1, 書き込みなし)。</summary>
        TestEqual,
    }

    private enum PipelineKind
    {
        Texture,
        Primitive,
        Blit,
        Pie,
        Masked,
        StencilWrite,
    }

    private enum VertexLayout
    {
        None,
        Position2D,
        PositionUv,
        InstancedSprite,
    }

    /// <summary>インスタンシングテクスチャ描画用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout TextureLayout
    {
        get
        {
            EnsureInitialized();
            return _textureLayout;
        }
    }

    /// <summary>プリミティブ描画用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout PrimitiveLayout
    {
        get
        {
            EnsureInitialized();
            return _primitiveLayout;
        }
    }

    /// <summary>ブリット用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout BlitLayout
    {
        get
        {
            EnsureInitialized();
            return _blitLayout;
        }
    }

    /// <summary>扇形テクスチャ描画用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout PieLayout
    {
        get
        {
            EnsureInitialized();
            return _pieLayout;
        }
    }

    /// <summary>カスタムスプライトシェーダー用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout CustomSpriteLayout
    {
        get
        {
            EnsureInitialized();
            return _customSpriteLayout;
        }
    }

    /// <summary>カスタムブリットシェーダー用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout CustomBlitLayout
    {
        get
        {
            EnsureInitialized();
            return _customBlitLayout;
        }
    }

    /// <summary>アルファマスク合成用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout MaskedLayout
    {
        get
        {
            EnsureInitialized();
            return _maskedLayout;
        }
    }

    /// <summary>ステンシル書き込み用のパイプラインレイアウトを取得します。</summary>
    public PipelineLayout StencilWriteLayout
    {
        get
        {
            EnsureInitialized();
            return _stencilWriteLayout;
        }
    }

    /// <summary>インスタンシングテクスチャ描画用のパイプラインを取得します。</summary>
    public Pipeline GetTexturePipeline(PassClass pass, StencilMode stencil = StencilMode.None) =>
        GetOrCreate(PipelineKind.Texture, pass, PrimitiveTopology.TriangleList, stencil);

    /// <summary>プリミティブ描画用のパイプラインを取得します。</summary>
    public Pipeline GetPrimitivePipeline(
        PassClass pass,
        PrimitiveTopology topology,
        StencilMode stencil = StencilMode.None
    ) => GetOrCreate(PipelineKind.Primitive, pass, topology, stencil);

    /// <summary>フルスクリーンブリット用のパイプラインを取得します。</summary>
    public Pipeline GetBlitPipeline(PassClass pass) =>
        GetOrCreate(PipelineKind.Blit, pass, PrimitiveTopology.TriangleList, StencilMode.None);

    /// <summary>扇形テクスチャ描画用のパイプラインを取得します。</summary>
    public Pipeline GetPiePipeline(PassClass pass, StencilMode stencil = StencilMode.None) =>
        GetOrCreate(PipelineKind.Pie, pass, PrimitiveTopology.TriangleList, stencil);

    /// <summary>アルファマスク合成用のパイプラインを取得します。</summary>
    public Pipeline GetMaskedPipeline(PassClass pass) =>
        GetOrCreate(PipelineKind.Masked, pass, PrimitiveTopology.TriangleList, StencilMode.None);

    /// <summary>ステンシルへのマスク書き込み用のパイプラインを取得します。</summary>
    public Pipeline GetStencilWritePipeline(PassClass pass) =>
        GetOrCreate(
            PipelineKind.StencilWrite,
            pass,
            PrimitiveTopology.TriangleList,
            StencilMode.WriteMask
        );

    /// <summary>カスタムシェーダーによるスプライト描画用のパイプラインを取得します。</summary>
    public Pipeline GetCustomSpritePipeline(
        int shaderId,
        PassClass pass,
        StencilMode stencil = StencilMode.None
    ) => GetOrCreateCustom(shaderId, CustomKind.Sprite, pass, stencil);

    /// <summary>カスタムシェーダーによるフルスクリーンブリット用のパイプラインを取得します。</summary>
    public Pipeline GetCustomBlitPipeline(int shaderId, PassClass pass) =>
        GetOrCreateCustom(shaderId, CustomKind.Blit, pass, StencilMode.None);

    /// <summary>
    /// 破棄されたカスタムシェーダーのパイプラインをキャッシュから除去します。
    /// </summary>
    public void InvalidateShader(int shaderId)
    {
        var keys = new List<(int, CustomKind, PassClass, StencilMode)>();
        foreach (var key in _customCache.Keys)
            if (key.ShaderId == shaderId)
                keys.Add(key);

        var vk = _ctx.Vk;
        var device = _ctx.Device;
        foreach (var key in keys)
        {
            if (_customCache.Remove(key, out var pipeline))
                _ctx.DeferDestroy(() => vk.DestroyPipeline(device, pipeline, null));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var vk = _ctx.Vk;
        var device = _ctx.Device;

        foreach (var pipeline in _cache.Values)
            vk.DestroyPipeline(device, pipeline, null);
        _cache.Clear();
        foreach (var pipeline in _customCache.Values)
            vk.DestroyPipeline(device, pipeline, null);
        _customCache.Clear();

        if (_initialized)
        {
            vk.DestroyPipelineLayout(device, _textureLayout, null);
            vk.DestroyPipelineLayout(device, _primitiveLayout, null);
            vk.DestroyPipelineLayout(device, _blitLayout, null);
            vk.DestroyPipelineLayout(device, _pieLayout, null);
            vk.DestroyPipelineLayout(device, _customSpriteLayout, null);
            vk.DestroyPipelineLayout(device, _customBlitLayout, null);
            vk.DestroyPipelineLayout(device, _maskedLayout, null);
            vk.DestroyPipelineLayout(device, _stencilWriteLayout, null);
        }

        _compiler.Dispose();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        var vk = _ctx.Vk;
        var device = _ctx.Device;
        var textureSetLayout = _resources.TextureSetLayout;
        var uboSetLayout = _materials.UboSetLayout;
        var textureAndUboLayouts = stackalloc DescriptorSetLayout[2] { textureSetLayout, uboSetLayout };

        // texture: set0 = sampler, push constant = mat4 (vertex)
        {
            var pushConstant = new PushConstantRange(ShaderStageFlags.VertexBit, 0, 64);
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &textureSetLayout,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _textureLayout);
        }

        // primitive: セットなし, push constant = vec4 (fragment)
        {
            var pushConstant = new PushConstantRange(ShaderStageFlags.FragmentBit, 0, 16);
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _primitiveLayout);
        }

        // blit: set0 = sampler のみ
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &textureSetLayout,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _blitLayout);
        }

        // pie: set0 = sampler, push constant = mat4 + vec4 + vec2 (両ステージ, 96 bytes)
        {
            var pushConstant = new PushConstantRange(
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                0,
                96
            );
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &textureSetLayout,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _pieLayout);
        }

        // custom sprite: set0 = sampler, set1 = UBO, push constant = mat4 (vertex)
        {
            var pushConstant = new PushConstantRange(ShaderStageFlags.VertexBit, 0, 64);
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 2,
                PSetLayouts = textureAndUboLayouts,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _customSpriteLayout);
        }

        // custom blit: set0 = sampler, set1 = UBO
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 2,
                PSetLayouts = textureAndUboLayouts,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _customBlitLayout);
        }

        // masked: set0 = content, set1 = mask, push constant = mat4 + vec4 (両ステージ, 80 bytes)
        {
            var twoTextureLayouts = stackalloc DescriptorSetLayout[2] { textureSetLayout, textureSetLayout };
            var pushConstant = new PushConstantRange(
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                0,
                80
            );
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 2,
                PSetLayouts = twoTextureLayouts,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _maskedLayout);
        }

        // stencil write: set0 = sampler, push constant = mat4 + vec4 (両ステージ, 80 bytes)
        {
            var pushConstant = new PushConstantRange(
                ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                0,
                80
            );
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &textureSetLayout,
                PushConstantRangeCount = 1,
                PPushConstantRanges = &pushConstant,
            };
            vk.CreatePipelineLayout(device, in layoutInfo, null, out _stencilWriteLayout);
        }

        _initialized = true;
    }

    private Pipeline GetOrCreate(
        PipelineKind kind,
        PassClass pass,
        PrimitiveTopology topology,
        StencilMode stencil
    )
    {
        var key = (kind, pass, topology, stencil);
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        EnsureInitialized();
        var pipeline = kind switch
        {
            PipelineKind.Texture => CreateEmbeddedPipeline(
                "texture_instanced",
                "texture_instanced",
                _textureLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: true,
                VertexLayout.InstancedSprite,
                stencil
            ),
            PipelineKind.Primitive => CreateEmbeddedPipeline(
                "primitive",
                "primitive",
                _primitiveLayout,
                pass,
                topology,
                enableBlend: true,
                VertexLayout.Position2D,
                stencil
            ),
            PipelineKind.Blit => CreateEmbeddedPipeline(
                "blit",
                "blit",
                _blitLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: false,
                VertexLayout.None,
                stencil
            ),
            PipelineKind.Pie => CreateEmbeddedPipeline(
                "pie",
                "pie",
                _pieLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: true,
                VertexLayout.PositionUv,
                stencil
            ),
            PipelineKind.Masked => CreateEmbeddedPipeline(
                "masked",
                "masked",
                _maskedLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: true,
                VertexLayout.PositionUv,
                stencil
            ),
            PipelineKind.StencilWrite => CreateEmbeddedPipeline(
                "masked",
                "stencil_mask",
                _stencilWriteLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: false,
                VertexLayout.PositionUv,
                StencilMode.WriteMask
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        _cache[key] = pipeline;
        return pipeline;
    }

    private Pipeline GetOrCreateCustom(
        int shaderId,
        CustomKind kind,
        PassClass pass,
        StencilMode stencil
    )
    {
        var key = (shaderId, kind, pass, stencil);
        if (_customCache.TryGetValue(key, out var cached))
            return cached;

        EnsureInitialized();
        var entry = _shaders.Get(shaderId);
        var pipeline = kind switch
        {
            CustomKind.Sprite => CreatePipeline(
                entry.VertexModule,
                entry.FragmentModule,
                _customSpriteLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: true,
                VertexLayout.InstancedSprite,
                stencil
            ),
            CustomKind.Blit => CreatePipeline(
                entry.VertexModule,
                entry.FragmentModule,
                _customBlitLayout,
                pass,
                PrimitiveTopology.TriangleList,
                enableBlend: false,
                VertexLayout.None,
                stencil
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        _customCache[key] = pipeline;
        return pipeline;
    }

    private RenderPass GetRenderPass(PassClass pass) =>
        pass == PassClass.Offscreen ? _ctx.OffscreenClearPass : _ctx.SwapchainPass;

    private Pipeline CreateEmbeddedPipeline(
        string vertexShaderName,
        string fragmentShaderName,
        PipelineLayout layout,
        PassClass pass,
        PrimitiveTopology topology,
        bool enableBlend,
        VertexLayout vertexLayout,
        StencilMode stencil
    )
    {
        var vertSpv = _compiler.Compile(
            EmbeddedResource.GetResourceAsString($"Promete.Resources.shaders.vulkan.{vertexShaderName}.vert"),
            ShaderKind.VertexShader,
            $"{vertexShaderName}.vert"
        );
        var fragSpv = _compiler.Compile(
            EmbeddedResource.GetResourceAsString($"Promete.Resources.shaders.vulkan.{fragmentShaderName}.frag"),
            ShaderKind.FragmentShader,
            $"{fragmentShaderName}.frag"
        );

        var vertModule = CreateShaderModule(vertSpv);
        var fragModule = CreateShaderModule(fragSpv);

        try
        {
            return CreatePipeline(vertModule, fragModule, layout, pass, topology, enableBlend, vertexLayout, stencil);
        }
        finally
        {
            _ctx.Vk.DestroyShaderModule(_ctx.Device, vertModule, null);
            _ctx.Vk.DestroyShaderModule(_ctx.Device, fragModule, null);
        }
    }

    private Pipeline CreatePipeline(
        ShaderModule vertModule,
        ShaderModule fragModule,
        PipelineLayout layout,
        PassClass pass,
        PrimitiveTopology topology,
        bool enableBlend,
        VertexLayout vertexLayout,
        StencilMode stencil
    )
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;

        var entryPoint = (byte*)SilkMarshal.StringToPtr("main");
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

        // 頂点入力レイアウト
        var bindings = stackalloc VertexInputBindingDescription[2];
        var attributes = stackalloc VertexInputAttributeDescription[8];
        uint bindingCount = 0;
        uint attributeCount = 0;
        switch (vertexLayout)
        {
            case VertexLayout.Position2D:
                bindings[0] = new VertexInputBindingDescription(0, 8, VertexInputRate.Vertex);
                attributes[0] = new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0);
                bindingCount = 1;
                attributeCount = 1;
                break;
            case VertexLayout.PositionUv:
                bindings[0] = new VertexInputBindingDescription(0, 16, VertexInputRate.Vertex);
                attributes[0] = new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0);
                attributes[1] = new VertexInputAttributeDescription(1, 0, Format.R32G32Sfloat, 8);
                bindingCount = 1;
                attributeCount = 2;
                break;
            case VertexLayout.InstancedSprite:
                bindings[0] = new VertexInputBindingDescription(0, 16, VertexInputRate.Vertex);
                bindings[1] = new VertexInputBindingDescription(1, 96, VertexInputRate.Instance);
                attributes[0] = new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0);
                attributes[1] = new VertexInputAttributeDescription(1, 0, Format.R32G32Sfloat, 8);
                attributes[2] = new VertexInputAttributeDescription(2, 1, Format.R32G32B32A32Sfloat, 0);
                attributes[3] = new VertexInputAttributeDescription(3, 1, Format.R32G32B32A32Sfloat, 16);
                attributes[4] = new VertexInputAttributeDescription(4, 1, Format.R32G32B32A32Sfloat, 32);
                attributes[5] = new VertexInputAttributeDescription(5, 1, Format.R32G32B32A32Sfloat, 48);
                attributes[6] = new VertexInputAttributeDescription(6, 1, Format.R32G32B32A32Sfloat, 64);
                attributes[7] = new VertexInputAttributeDescription(7, 1, Format.R32G32B32A32Sfloat, 80);
                bindingCount = 2;
                attributeCount = 8;
                break;
        }

        var vertexInput = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = bindingCount,
            PVertexBindingDescriptions = bindings,
            VertexAttributeDescriptionCount = attributeCount,
            PVertexAttributeDescriptions = attributes,
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = topology,
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

        // ステンシル書き込みモードではカラーバッファへ書き込まない
        var blendAttachment = new PipelineColorBlendAttachmentState
        {
            BlendEnable = enableBlend,
            SrcColorBlendFactor = BlendFactor.SrcAlpha,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = BlendOp.Add,
            SrcAlphaBlendFactor = BlendFactor.One,
            DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
            AlphaBlendOp = BlendOp.Add,
            ColorWriteMask =
                stencil == StencilMode.WriteMask
                    ? 0
                    : ColorComponentFlags.RBit
                        | ColorComponentFlags.GBit
                        | ColorComponentFlags.BBit
                        | ColorComponentFlags.ABit,
        };

        var stencilOp = stencil switch
        {
            StencilMode.WriteMask => new StencilOpState
            {
                FailOp = StencilOp.Keep,
                PassOp = StencilOp.Replace,
                DepthFailOp = StencilOp.Keep,
                CompareOp = CompareOp.Always,
                CompareMask = 0xFF,
                WriteMask = 0xFF,
                Reference = 1,
            },
            StencilMode.TestEqual => new StencilOpState
            {
                FailOp = StencilOp.Keep,
                PassOp = StencilOp.Keep,
                DepthFailOp = StencilOp.Keep,
                CompareOp = CompareOp.Equal,
                CompareMask = 0xFF,
                WriteMask = 0x00,
                Reference = 1,
            },
            _ => default,
        };

        var depthStencil = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = false,
            DepthWriteEnable = false,
            StencilTestEnable = stencil != StencilMode.None,
            Front = stencilOp,
            Back = stencilOp,
        };

        var colorBlend = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            AttachmentCount = 1,
            PAttachments = &blendAttachment,
        };

        var dynamicStates = stackalloc DynamicState[2]
        {
            DynamicState.Viewport,
            DynamicState.Scissor,
        };
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
            PDepthStencilState = pass == PassClass.Offscreen ? &depthStencil : null,
            PDynamicState = &dynamicState,
            Layout = layout,
            RenderPass = GetRenderPass(pass),
            Subpass = 0,
        };

        var result = vk.CreateGraphicsPipelines(
            device,
            default,
            1,
            in createInfo,
            null,
            out var pipeline
        );

        SilkMarshal.Free((nint)entryPoint);

        if (result != Result.Success)
            throw new InvalidOperationException($"パイプラインの作成に失敗しました: {result}");
        return pipeline;
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
}
