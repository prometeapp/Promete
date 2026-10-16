using System.Drawing;

namespace Promete.Graphics.Rendering.Commands;

/// <summary>
/// 4つの頂点に合わせてテクスチャを変形して描画するコマンドです。
/// </summary>
/// <remarks>
/// 頂点はすべてワールド座標 (描画先の左上を原点とするピクセル座標) で指定します。
/// </remarks>
public sealed class DrawDeformedTextureCommand : IRenderCommand
{
    public required Texture2D Texture { get; init; }

    /// <summary>テクスチャの左上に対応する頂点のワールド座標。</summary>
    public required Vector TopLeft { get; init; }

    /// <summary>テクスチャの右上に対応する頂点のワールド座標。</summary>
    public required Vector TopRight { get; init; }

    /// <summary>テクスチャの左下に対応する頂点のワールド座標。</summary>
    public required Vector BottomLeft { get; init; }

    /// <summary>テクスチャの右下に対応する頂点のワールド座標。</summary>
    public required Vector BottomRight { get; init; }

    public required Color TintColor { get; init; }

    /// <summary>適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</summary>
    public Material? Material { get; init; }
}
