using System.Drawing;
using Promete.Nodes;

namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// プリミティブ図形描画コマンドです。
/// </summary>
public sealed class DrawPrimitiveCommand : IRenderCommand
{
    public required Vector[] WorldVertices { get; init; }
    public required ShapeType ShapeType { get; init; }
    public required Color Color { get; init; }
    public int LineWidth { get; init; }
    public Color? LineColor { get; init; }

    /// <summary>適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</summary>
    public Material? Material { get; init; }
}
