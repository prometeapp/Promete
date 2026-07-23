using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Promete.Nodes;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// <see cref="MaskedContainer"/> のレンダリングを支援するヘルパークラスです。
/// アルファマスク方式のサブレンダリングと、ステンシル/アルファマスクの描画を担います。
/// </summary>
internal sealed unsafe class VulkanMaskedContainerHelper(
    PrometeApp app,
    RenderCommandQueue queue,
    VulkanContext ctx,
    VulkanResourceManager resources,
    VulkanPipelineProvider pipelines,
    IRenderTextureProvider renderTextureProvider
) : IDisposable
{
    // push constant: mat4 (16) + vec4 tint (4) = 20 floats
    private const int PushConstantFloats = 20;

    // MaskedContainer ごとの RenderTexture キャッシュ
    private readonly Dictionary<MaskedContainer, RenderTexture> _renderTextureCache = [];

    private Buffer _quadVbo;
    private DeviceMemory _quadVboMemory;
    private Buffer _quadEbo;
    private DeviceMemory _quadEboMemory;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// MaskedContainerの子要素を専用の RenderTexture にレンダリングし、テクスチャを返します。
    /// </summary>
    public Texture2D RenderToTexture(MaskedContainer container, RenderContext renderContext)
    {
        var size = container.Size;
        if (size.X <= 0 || size.Y <= 0)
            size = new VectorInt(1, 1);

        // RenderTexture を取得または作成
        if (!_renderTextureCache.TryGetValue(container, out var rt))
        {
            rt = renderTextureProvider.Create(size);
            _renderTextureCache[container] = rt;
        }
        else if (rt.Size != size)
        {
            rt.Resize(size);
        }

        using var capture = rt.BeginCapture(Color.Transparent);

        // 子要素を相対座標でレンダリングするため、一時的にMaskedContainerの変換を除去
        // (GL と異なり Vulkan の RT は top-down のため Y 反転は不要)
        var originalLocation = container.Location;
        var originalAngle = container.Angle;
        var originalScale = container.Scale;
        var originalParent = container.Parent;

        container.Parent = null;
        container.Location = (0, 0);
        container.Angle = 0.Degrees;
        container.Scale = (1, 1);

        // 子要素のModelMatrixを再計算させる
        container.BeforeRender();
        var sorted = container.SortedChildren;
        foreach (var child in sorted)
            child.BeforeRender();

        // 子要素をコマンドキュー経由でレンダリング（スコープで外側を保護）
        queue.PushScope();
        foreach (var child in sorted)
            app.CollectNode(child, queue, renderContext);
        queue.PopScopeAndFlush();

        // MaskedContainerの状態を元に戻す
        container.Parent = originalParent;
        container.Location = originalLocation;
        container.Angle = originalAngle;
        container.Scale = originalScale;

        container.BeforeRender();
        foreach (var child in sorted)
            child.BeforeRender();

        return rt.Texture;
    }

    /// <summary>
    /// ステンシルバッファにマスクテクスチャの形状を書き込みます。
    /// </summary>
    public void DrawMaskToStencil(Texture2D maskTexture, Node node)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (!ctx.IsFrameActive || !resources.Contains(maskTexture.Handle))
            return;

        EnsureInitialized();

        var pipeline = pipelines.GetStencilWritePipeline(VulkanPipelineProvider.PassClass.Offscreen);
        var maskSet = resources.GetDescriptorSet(maskTexture.Handle);
        DrawQuad(pipeline, pipelines.StencilWriteLayout, node, [maskSet]);
    }

    /// <summary>
    /// コンテンツテクスチャにマスクテクスチャの濃淡を適用して描画します。
    /// </summary>
    public void DrawMasked(Texture2D contentTexture, Texture2D maskTexture, Node node)
    {
        PrometeApp.Current.ThrowIfNotMainThread();
        if (
            !ctx.IsFrameActive
            || !resources.Contains(contentTexture.Handle)
            || !resources.Contains(maskTexture.Handle)
        )
            return;

        EnsureInitialized();

        var pipeline = pipelines.GetMaskedPipeline(VulkanPipelineProvider.PassClass.Offscreen);
        var contentSet = resources.GetDescriptorSet(contentTexture.Handle);
        var maskSet = resources.GetDescriptorSet(maskTexture.Handle);
        DrawQuad(pipeline, pipelines.MaskedLayout, node, [contentSet, maskSet]);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var rt in _renderTextureCache.Values)
            rt.Dispose();
        _renderTextureCache.Clear();

        if (!_initialized)
            return;
        var vk = ctx.Vk;
        vk.DestroyBuffer(ctx.Device, _quadVbo, null);
        vk.FreeMemory(ctx.Device, _quadVboMemory, null);
        vk.DestroyBuffer(ctx.Device, _quadEbo, null);
        vk.FreeMemory(ctx.Device, _quadEboMemory, null);
    }

    /// <summary>
    /// ノードの位置・サイズでクワッドを描画します。
    /// </summary>
    private void DrawQuad(
        Pipeline pipeline,
        PipelineLayout layout,
        Node node,
        ReadOnlySpan<DescriptorSet> descriptorSets
    )
    {
        var vk = ctx.Vk;
        var cmd = ctx.CurrentCommandBuffer;

        var size = node.Size;
        var model = Matrix4x4.CreateScale(size.X, size.Y, 1) * node.ModelMatrix;

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

        var push = stackalloc float[PushConstantFloats];
        *(Matrix4x4*)push = mvp;
        push[16] = 1f;
        push[17] = 1f;
        push[18] = 1f;
        push[19] = 1f;

        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);
        vk.CmdPushConstants(
            cmd,
            layout,
            ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            0,
            PushConstantFloats * sizeof(float),
            push
        );

        fixed (DescriptorSet* sets = descriptorSets)
        {
            vk.CmdBindDescriptorSets(
                cmd,
                PipelineBindPoint.Graphics,
                layout,
                0,
                (uint)descriptorSets.Length,
                sets,
                0,
                null
            );
        }

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
            1.0f, 0.0f, 1.0f, 0.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            0.0f, 1.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 0.0f, 0.0f,
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
