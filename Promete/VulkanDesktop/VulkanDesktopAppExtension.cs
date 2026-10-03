using Promete.Backends.Vulkan;
using Promete.Graphics.Rendering;
using Promete.Windowing;

namespace Promete.VulkanDesktop;

/// <summary>
/// Vulkan を使用したデスクトップアプリケーション用の拡張機能を提供するクラスです。
/// </summary>
public static class VulkanDesktopAppExtension
{
    /// <summary>
    /// PrometeApp を Vulkan デスクトップアプリケーションとして構築します。
    /// </summary>
    /// <remarks>
    /// 実験的なバックエンドです。スプライト・プリミティブ・トリム・RenderTexture の描画に対応しています。
    /// カスタムシェーダー・マスク・扇形テクスチャ・ポストプロセスは未対応です。
    /// 詳細は VULKAN_PORTING_PLAN.md を参照してください。
    /// </remarks>
    /// <param name="builder">PrometeAppのビルダー</param>
    /// <param name="opts">ウィンドウの設定</param>
    /// <returns>構築されたPrometeAppインスタンス</returns>
    public static PrometeApp BuildWithVulkanDesktop(
        this PrometeApp.PrometeAppBuilder builder,
        WindowOptions? opts = null
    )
    {
        // TODO: Phase 3 で Vulkan の CommandRunner 群を登録する
        return builder.Use<RenderCommandQueue>().Build<VulkanDesktopBackend>(opts);
    }
}
