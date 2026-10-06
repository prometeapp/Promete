using System.Drawing;
using System.Numerics;

namespace Promete.Graphics.Rendering.Commands;

/// <summary>
/// 4頂点を指定してテクスチャを描画するコマンドです。
/// </summary>
public readonly struct DrawDeformedTextureCommand : IRenderCommand
{
    public required Texture2D Texture { get; init; }
    public required Matrix4x4 ModelMatrix { get; init; }
    public required Color TintColor { get; init; }
    public required Vector TopLeft { get; init; }
    public required Vector TopRight { get; init; }
    public required Vector BottomRight { get; init; }
    public required Vector BottomLeft { get; init; }
    public Material? Material { get; init; }
}
