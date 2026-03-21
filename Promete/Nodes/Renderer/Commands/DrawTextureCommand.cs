using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Nodes;

namespace Promete.Nodes.Renderer.Commands;

/// <summary>
/// テクスチャ描画コマンドです。
/// </summary>
public readonly struct DrawTextureCommand : IRenderCommand
{
    public required Texture2D Texture { get; init; }
    public required Matrix4x4 ModelMatrix { get; init; }
    public required Color TintColor { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }

    /// <summary>描画オフセット（ローカル座標）。デフォルトは原点。</summary>
    public Vector Pivot { get; init; }

    /// <summary>適用するマテリアル。null の場合はデフォルトシェーダーを使用します。</summary>
    public Material? Material { get; init; }

    public DrawTextureCommand()
    {
    }
}
