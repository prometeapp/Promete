using System;
using System.Collections.Generic;
using System.Drawing;
using Promete.Backends;
using Promete.Internal;
using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// 全描画をスクリーンサイズの <see cref="RenderTexture"/> にキャプチャし、
/// ポストプロセスマテリアルを適用した後、スワップチェーンイメージへブリットするクラスです。
/// </summary>
internal sealed class VulkanScreenBlitter : IScreenBlitter
{
    private readonly VulkanContext _ctx;
    private readonly VulkanResourceManager _resources;
    private readonly VulkanPipelineProvider _pipelines;
    private readonly VulkanRenderTextureProvider _provider;
    private readonly VulkanShaderManager _shaders;
    private readonly VulkanMaterialSystem _materials;
    private readonly IGameView _view;

    // ピンポンバッファ（ポストプロセス使用時に遅延生成）
    private RenderTexture? _pingPong0;
    private RenderTexture? _pingPong1;
    private bool _shaderWarned;

    public VulkanScreenBlitter(
        VulkanContext ctx,
        VulkanResourceManager resources,
        VulkanPipelineProvider pipelines,
        VulkanRenderTextureProvider provider,
        VulkanShaderManager shaders,
        VulkanMaterialSystem materials,
        IGameView view
    )
    {
        _ctx = ctx;
        _resources = resources;
        _pipelines = pipelines;
        _provider = provider;
        _shaders = shaders;
        _materials = materials;
        _view = view;
        _view.Resize += OnViewResize;
    }

    /// <summary>
    /// 全描画のキャプチャ先 RenderTexture を取得します。
    /// </summary>
    public RenderTexture ScreenRenderTexture { get; private set; } = null!;

    /// <summary>
    /// 最後にスワップチェーンへブリットした RenderTexture (ポストプロセス適用後) を取得します。
    /// スクリーンショットはこれを読み出すことで表示内容と一致させます。
    /// </summary>
    public RenderTexture? LastBlitSource { get; private set; }

    public void InitializeScreenRenderTexture()
    {
        ScreenRenderTexture = _provider.Create(_view.Size);
    }

    public unsafe void BlitToScreen(IReadOnlyList<Material> materials)
    {
        var vk = _ctx.Vk;
        var src = ScreenRenderTexture;

        // ポストプロセスマテリアルをピンポンバッファへ順に適用
        if (materials.Count > 0)
        {
            EnsurePingPongBuffers();
            Span<RenderTexture> pingPongs = [_pingPong0!, _pingPong1!];
            var pingIdx = 0;

            foreach (var material in materials)
            {
                if (!_shaders.Contains(material.Shader.Handle))
                {
                    if (!_shaderWarned)
                    {
                        _shaderWarned = true;
                        LogHelper.Bug(
                            "ポストプロセスマテリアルのシェーダーがコンパイルされていないため、スキップします。"
                        );
                    }

                    continue;
                }

                var dst = pingPongs[pingIdx];
                using (dst.BeginCapture())
                {
                    var cmd = _ctx.CurrentCommandBuffer;
                    var pipeline = _pipelines.GetCustomBlitPipeline(
                        material.Shader.Handle,
                        VulkanPipelineProvider.PassClass.Offscreen
                    );
                    var layout = _pipelines.GetCustomLayout(
                        material.Shader.Handle,
                        VulkanPipelineProvider.CustomKind.Blit
                    );
                    vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);
                    BindSourceTexture(cmd, src, layout);
                    _materials.Apply(cmd, material, layout);
                    vk.CmdDraw(cmd, 3, 1, 0, 0);
                }

                src = dst;
                pingIdx = 1 - pingIdx;
            }
        }

        // 最終結果をスワップチェーンへブリット。
        // パスは終了せず開いたままにする (PostRender でのオーバーレイ描画用。EndFrame が閉じる)
        _ctx.BeginSwapchainPass(Color.Black);
        {
            var cmd = _ctx.CurrentCommandBuffer;
            var pipeline = _pipelines.GetBlitPipeline(VulkanPipelineProvider.PassClass.Swapchain);
            vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, pipeline);
            BindSourceTexture(cmd, src, _pipelines.BlitLayout);
            vk.CmdDraw(cmd, 3, 1, 0, 0);
        }

        LastBlitSource = src;
    }

    private unsafe void BindSourceTexture(CommandBuffer cmd, RenderTexture src, PipelineLayout layout)
    {
        var descriptorSet = _resources.GetDescriptorSet(src.Texture.Handle);
        _ctx.Vk.CmdBindDescriptorSets(
            cmd,
            PipelineBindPoint.Graphics,
            layout,
            0,
            1,
            in descriptorSet,
            0,
            null
        );
    }

    private void EnsurePingPongBuffers()
    {
        var size = ScreenRenderTexture.Size;
        _pingPong0 ??= _provider.Create(size);
        _pingPong1 ??= _provider.Create(size);
    }

    private void OnViewResize()
    {
        var size = _view.Size;
        ScreenRenderTexture.Resize(size);
        _pingPong0?.Resize(size);
        _pingPong1?.Resize(size);
    }
}
