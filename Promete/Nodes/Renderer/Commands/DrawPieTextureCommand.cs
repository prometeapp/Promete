using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Nodes;

namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// 扇状テクスチャ描画コマンドです。
/// </summary>
public sealed class DrawPieTextureCommand : IRenderCommand
{
    public required Texture2D Texture { get; init; }
    public required Matrix4x4 ModelMatrix { get; init; }
    public required Color TintColor { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required float StartPercent { get; init; }
    public required float Percent { get; init; }

    /// <summary>適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</summary>
    public Material? Material { get; init; }
}
