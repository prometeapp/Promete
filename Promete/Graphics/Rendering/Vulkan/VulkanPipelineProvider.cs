using System;
using System.Collections.Generic;
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
    private readonly VulkanShaderCompiler _compiler = new();
    private readonly Dictionary<(PipelineKind Kind, PassClass Pass, PrimitiveTopology Topology), Pipeline> _cache = [];

    private PipelineLayout _textureLayout;
    private PipelineLayout _primitiveLayout;
    private PipelineLayout _blitLayout;
    private bool _initialized;
    private bool _disposed;

    public VulkanPipelineProvider(VulkanContext ctx, VulkanResourceManager resources)
    {
        _ctx = ctx;
        _resources = resources;
    }

    /// <summary>描画先レンダーパスの系統。</summary>
    public enum PassClass
    {
        Offscreen,
        Swapchain,
    }

    private enum PipelineKind
    {
        Texture,
        Primitive,
        Blit,
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

    /// <summary>インスタンシングテクスチャ描画用のパイプラインを取得します。</summary>
    public Pipeline GetTexturePipeline(PassClass pass) =>
        GetOrCreate(PipelineKind.Texture, pass, PrimitiveTopology.TriangleList);

    /// <summary>プリミティブ描画用のパイプラインを取得します。</summary>
    public Pipeline GetPrimitivePipeline(PassClass pass, PrimitiveTopology topology) =>
        GetOrCreate(PipelineKind.Primitive, pass, topology);

    /// <summary>フルスクリーンブリット用のパイプラインを取得します。</summary>
    public Pipeline GetBlitPipeline(PassClass pass) =>
        GetOrCreate(PipelineKind.Blit, pass, PrimitiveTopology.TriangleList);

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

        if (_initialized)
        {
            vk.DestroyPipelineLayout(device, _textureLayout, null);
            vk.DestroyPipelineLayout(device, _primitiveLayout, null);
            vk.DestroyPipelineLayout(device, _blitLayout, null);
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

        _initialized = true;
    }

    private Pipeline GetOrCreate(PipelineKind kind, PassClass pass, PrimitiveTopology topology)
    {
        var key = (kind, pass, topology);
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        EnsureInitialized();
        var pipeline = kind switch
        {
            PipelineKind.Texture => CreateTexturePipeline(pass),
            PipelineKind.Primitive => CreatePrimitivePipeline(pass, topology),
            PipelineKind.Blit => CreateBlitPipeline(pass),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        _cache[key] = pipeline;
        return pipeline;
    }

    private RenderPass GetRenderPass(PassClass pass) =>
        pass == PassClass.Offscreen ? _ctx.OffscreenClearPass : _ctx.SwapchainPass;

    private Pipeline CreateTexturePipeline(PassClass pass)
    {
        // binding 0: 頂点 (pos2 + uv2), binding 1: インスタンス (mat4 + tint + uvRect = 24 floats)
        var bindings = stackalloc VertexInputBindingDescription[2]
        {
            new VertexInputBindingDescription(0, 16, VertexInputRate.Vertex),
            new VertexInputBindingDescription(1, 96, VertexInputRate.Instance),
        };

        var attributes = stackalloc VertexInputAttributeDescription[8]
        {
            new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0),
            new VertexInputAttributeDescription(1, 0, Format.R32G32Sfloat, 8),
            new VertexInputAttributeDescription(2, 1, Format.R32G32B32A32Sfloat, 0),
            new VertexInputAttributeDescription(3, 1, Format.R32G32B32A32Sfloat, 16),
            new VertexInputAttributeDescription(4, 1, Format.R32G32B32A32Sfloat, 32),
            new VertexInputAttributeDescription(5, 1, Format.R32G32B32A32Sfloat, 48),
            new VertexInputAttributeDescription(6, 1, Format.R32G32B32A32Sfloat, 64),
            new VertexInputAttributeDescription(7, 1, Format.R32G32B32A32Sfloat, 80),
        };

        return CreatePipeline(
            "texture_instanced",
            _textureLayout,
            GetRenderPass(pass),
            PrimitiveTopology.TriangleList,
            enableBlend: true,
            bindings,
            2,
            attributes,
            8
        );
    }

    private Pipeline CreatePrimitivePipeline(PassClass pass, PrimitiveTopology topology)
    {
        var bindings = stackalloc VertexInputBindingDescription[1]
        {
            new VertexInputBindingDescription(0, 8, VertexInputRate.Vertex),
        };
        var attributes = stackalloc VertexInputAttributeDescription[1]
        {
            new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, 0),
        };

        return CreatePipeline(
            "primitive",
            _primitiveLayout,
            GetRenderPass(pass),
            topology,
            enableBlend: true,
            bindings,
            1,
            attributes,
            1
        );
    }

    private Pipeline CreateBlitPipeline(PassClass pass)
    {
        return CreatePipeline(
            "blit",
            _blitLayout,
            GetRenderPass(pass),
            PrimitiveTopology.TriangleList,
            enableBlend: false,
            null,
            0,
            null,
            0
        );
    }

    private Pipeline CreatePipeline(
        string shaderName,
        PipelineLayout layout,
        RenderPass renderPass,
        PrimitiveTopology topology,
        bool enableBlend,
        VertexInputBindingDescription* bindings,
        uint bindingCount,
        VertexInputAttributeDescription* attributes,
        uint attributeCount
    )
    {
        var vk = _ctx.Vk;
        var device = _ctx.Device;

        var vertSpv = _compiler.Compile(
            EmbeddedResource.GetResourceAsString($"Promete.Resources.shaders.vulkan.{shaderName}.vert"),
            ShaderKind.VertexShader,
            $"{shaderName}.vert"
        );
        var fragSpv = _compiler.Compile(
            EmbeddedResource.GetResourceAsString($"Promete.Resources.shaders.vulkan.{shaderName}.frag"),
            ShaderKind.FragmentShader,
            $"{shaderName}.frag"
        );

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
            PDynamicState = &dynamicState,
            Layout = layout,
            RenderPass = renderPass,
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

        Silk.NET.Core.Native.SilkMarshal.Free((nint)entryPoint);
        vk.DestroyShaderModule(device, vertModule, null);
        vk.DestroyShaderModule(device, fragModule, null);

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
