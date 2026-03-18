using System.Drawing;
using System.Numerics;
using Promete.Graphics;

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
}
