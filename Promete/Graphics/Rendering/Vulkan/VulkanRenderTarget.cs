using Silk.NET.Vulkan;

namespace Promete.Graphics.Rendering.Vulkan;

/// <summary>
/// オフスクリーン描画先のイメージ・フレームバッファ一式を保持します。
/// </summary>
internal sealed class VulkanRenderTarget
{
    /// <summary>カラーアタッチメントのイメージ。</summary>
    public required Image Image { get; set; }

    /// <summary>イメージのメモリ。</summary>
    public required DeviceMemory Memory { get; set; }

    /// <summary>イメージビュー。</summary>
    public required ImageView View { get; set; }

    /// <summary>ステンシルアタッチメントのイメージ。</summary>
    public required Image StencilImage { get; set; }

    /// <summary>ステンシルイメージのメモリ。</summary>
    public required DeviceMemory StencilMemory { get; set; }

    /// <summary>ステンシルイメージビュー。</summary>
    public required ImageView StencilView { get; set; }

    /// <summary>オフスクリーンパス用フレームバッファ。</summary>
    public required Framebuffer Framebuffer { get; set; }

    /// <summary>ターゲットの大きさ。</summary>
    public required Extent2D Extent { get; set; }

    /// <summary>リソーステーブル上のテクスチャ ID（<see cref="Texture2D.Handle"/> と同一）。</summary>
    public required int TextureId { get; init; }
}
