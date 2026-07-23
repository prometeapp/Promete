using System;
using System.Collections.Generic;
using System.Drawing;
using Promete.Backends;
using Promete.Internal;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// 全描画をスクリーンサイズの <see cref="RenderTexture"/> にキャプチャし、
/// スワップチェーンイメージへブリットするクラスです。
/// </summary>
internal sealed class VulkanScreenBlitter : IScreenBlitter
{
    private readonly VulkanContext _ctx;
    private readonly VulkanResourceManager _resources;
    private readonly VulkanPipelineProvider _pipelines;
    private readonly VulkanRenderTextureProvider _provider;
    private readonly IGameView _view;
    private bool _postProcessWarned;

    public VulkanScreenBlitter(
        VulkanContext ctx,
        VulkanResourceManager resources,
        VulkanPipelineProvider pipelines,
        VulkanRenderTextureProvider provider,
        IGameView view
    )
    {
        _ctx = ctx;
        _resources = resources;
        _pipelines = pipelines;
        _provider = provider;
        _view = view;
        _view.Resize += OnViewResize;
    }

    /// <summary>
    /// 全描画のキャプチャ先 RenderTexture を取得します。
    /// </summary>
    public RenderTexture ScreenRenderTexture { get; private set; } = null!;

    public void InitializeScreenRenderTexture()
    {
        ScreenRenderTexture = _provider.Create(_view.Size);
    }

    public unsafe void BlitToScreen(IReadOnlyList<Material> materials)
    {
        // TODO: Phase 3+ でポストプロセスマテリアルに対応する
        if (materials.Count > 0 && !_postProcessWarned)
        {
            _postProcessWarned = true;
            LogHelper.Bug("Vulkan バックエンドはまだポストプロセスマテリアルをサポートしていません。");
        }

        var vk = _ctx.Vk;
        _ctx.BeginSwapchainPass(Color.Black);

        var cmd = _ctx.CurrentCommandBuffer;
        var pipeline = _pipelines.GetBlitPipeline(VulkanPipelineProvider.PassClass.Swapchain);
        vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);

        var descriptorSet = _resources.GetDescriptorSet(ScreenRenderTexture.Texture.Handle);
        vk.CmdBindDescriptorSets(
            cmd,
            PipelineBindPoint.Graphics,
            _pipelines.BlitLayout,
            0,
            1,
            in descriptorSet,
            0,
            null
        );

        // フルスクリーントライアングル（頂点バッファ不要）
        vk.CmdDraw(cmd, 3, 1, 0, 0);

        _ctx.EndSwapchainPass();
    }

    private void OnViewResize()
    {
        var size = _view.Size;
        ScreenRenderTexture.Resize(size);
    }
}
