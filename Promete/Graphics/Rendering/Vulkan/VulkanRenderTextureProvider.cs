using System;
using System.Collections.Generic;
using System.Drawing;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// Vulkan バックエンドにおける <see cref="IRenderTextureProvider"/> の実装です。
/// </summary>
internal sealed unsafe class VulkanRenderTextureProvider(
    VulkanContext ctx,
    VulkanResourceManager resources
) : IRenderTextureProvider
{
    private readonly Dictionary<RenderTexture, VulkanRenderTarget> _targets = [];

    public RenderTexture Create(VectorInt size)
    {
        var target = CreateTarget(size, textureId: null);
        var rt = new RenderTexture(size, CreateFlippedTexture(target.TextureId, size), this);
        _targets[rt] = target;
        return rt;
    }

    public IDisposable BeginCapture(RenderTexture renderTexture, Color? clearColor = null)
    {
        var target = _targets[renderTexture];
        ctx.PushRenderTarget(target, clearColor);
        return new CaptureScope(ctx);
    }

    public void Resize(RenderTexture renderTexture, VectorInt newSize)
    {
        var target = _targets[renderTexture];
        if (target.Extent.Width == (uint)newSize.X && target.Extent.Height == (uint)newSize.Y)
            return;

        // 使用中リソースを差し替えるため、デバイスの完了を待つ
        ctx.WaitIdle();
        DestroyTargetResources(target);

        var newTarget = CreateTarget(newSize, target.TextureId);
        target.Image = newTarget.Image;
        target.Memory = newTarget.Memory;
        target.View = newTarget.View;
        target.StencilImage = newTarget.StencilImage;
        target.StencilMemory = newTarget.StencilMemory;
        target.StencilView = newTarget.StencilView;
        target.Framebuffer = newTarget.Framebuffer;
        target.Extent = newTarget.Extent;

        renderTexture.Texture = CreateFlippedTexture(target.TextureId, newSize);
    }

    /// <summary>
    /// V 反転 UV を持つ RenderTexture 用の <see cref="Texture2D"/> を生成します。
    /// GL の RT テクスチャは bottom-up 格納であり、<see cref="FrameBuffer"/> 等はそれを前提に
    /// 子ノードを上下反転して補正しています。Vulkan の RT は top-down のため、
    /// UV を V 反転させることで GL と同じ見え方に揃えます。
    /// </summary>
    private static Texture2D CreateFlippedTexture(int textureId, VectorInt size) =>
        new(textureId, size, _ => { }, new Vector(0, 1), new Vector(1, 0));

    public void Release(RenderTexture renderTexture)
    {
        if (!_targets.Remove(renderTexture, out var target))
            return;

        resources.Destroy(target.TextureId);
        var vk = ctx.Vk;
        var device = ctx.Device;
        var framebuffer = target.Framebuffer;
        var view = target.View;
        var image = target.Image;
        var memory = target.Memory;
        var stencilView = target.StencilView;
        var stencilImage = target.StencilImage;
        var stencilMemory = target.StencilMemory;
        ctx.DeferDestroy(() =>
        {
            vk.DestroyFramebuffer(device, framebuffer, null);
            vk.DestroyImageView(device, view, null);
            vk.DestroyImage(device, image, null);
            vk.FreeMemory(device, memory, null);
            vk.DestroyImageView(device, stencilView, null);
            vk.DestroyImage(device, stencilImage, null);
            vk.FreeMemory(device, stencilMemory, null);
        });
    }

    /// <summary>
    /// 内部レンダーターゲットを取得します。（ブリッター・スクリーンショット用）
    /// </summary>
    internal VulkanRenderTarget GetTarget(RenderTexture renderTexture) => _targets[renderTexture];

    private VulkanRenderTarget CreateTarget(VectorInt size, int? textureId)
    {
        var width = (uint)Math.Max(1, size.X);
        var height = (uint)Math.Max(1, size.Y);

        var (image, memory) = ctx.CreateImage2D(
            width,
            height,
            VulkanContext.OffscreenFormat,
            ImageUsageFlags.ColorAttachmentBit
                | ImageUsageFlags.SampledBit
                | ImageUsageFlags.TransferSrcBit
        );

        // レンダーパスの initialLayout (General) に合わせて遷移しておく
        ctx.ExecuteOneTime(cmd =>
            ctx.TransitionImageLayout(
                cmd,
                image,
                ImageLayout.Undefined,
                ImageLayout.General,
                PipelineStageFlags.TopOfPipeBit,
                0,
                PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.FragmentShaderBit,
                AccessFlags.ColorAttachmentWriteBit | AccessFlags.ShaderReadBit
            )
        );

        var view = ctx.CreateImageView2D(image, VulkanContext.OffscreenFormat);

        // ステンシルアタッチメント
        var (stencilImage, stencilMemory) = ctx.CreateImage2D(
            width,
            height,
            ctx.StencilFormat,
            ImageUsageFlags.DepthStencilAttachmentBit
        );
        var stencilAspect = ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit;
        ctx.ExecuteOneTime(cmd =>
            ctx.TransitionImageLayout(
                cmd,
                stencilImage,
                ImageLayout.Undefined,
                ImageLayout.DepthStencilAttachmentOptimal,
                PipelineStageFlags.TopOfPipeBit,
                0,
                PipelineStageFlags.EarlyFragmentTestsBit | PipelineStageFlags.LateFragmentTestsBit,
                AccessFlags.DepthStencilAttachmentWriteBit | AccessFlags.DepthStencilAttachmentReadBit,
                stencilAspect
            )
        );
        var stencilView = ctx.CreateImageView2D(stencilImage, ctx.StencilFormat, stencilAspect);

        var framebuffer = ctx.CreateOffscreenFramebuffer(view, stencilView, width, height);

        int id;
        if (textureId is { } existingId)
        {
            resources.Replace(existingId, image, memory, view);
            id = existingId;
        }
        else
        {
            id = resources.Register(image, memory, view, ownsImage: false);
        }

        return new VulkanRenderTarget
        {
            Image = image,
            Memory = memory,
            View = view,
            StencilImage = stencilImage,
            StencilMemory = stencilMemory,
            StencilView = stencilView,
            Framebuffer = framebuffer,
            Extent = new Extent2D(width, height),
            TextureId = id,
        };
    }

    private void DestroyTargetResources(VulkanRenderTarget target)
    {
        var vk = ctx.Vk;
        var device = ctx.Device;
        vk.DestroyFramebuffer(device, target.Framebuffer, null);
        vk.DestroyImageView(device, target.View, null);
        vk.DestroyImage(device, target.Image, null);
        vk.FreeMemory(device, target.Memory, null);
        vk.DestroyImageView(device, target.StencilView, null);
        vk.DestroyImage(device, target.StencilImage, null);
        vk.FreeMemory(device, target.StencilMemory, null);
    }

    private sealed class CaptureScope(VulkanContext ctx) : IDisposable
    {
        public void Dispose() => ctx.PopRenderTarget();
    }
}
