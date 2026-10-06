using System.Drawing;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

namespace Promete.Nodes;

/// <summary>
/// 4頂点を自由に設定してテクスチャを描画するスプライトクラスです。
/// </summary>
public class DeformedSprite : Node
{
    private Texture2D? _texture;

    public DeformedSprite(Texture2D? texture = null, Color? tintColor = default)
    {
        _texture = texture;
        TintColor = tintColor ?? Color.White;
        var size = texture?.Size ?? (0, 0);
        TopRight = (size.X, 0);
        BottomRight = size;
        BottomLeft = (0, size.Y);
    }

    /// <summary>
    /// スプライトに使用するテクスチャを取得または設定します。
    /// </summary>
    public Texture2D? Texture
    {
        get => _texture;
        set => _texture = value;
    }

    /// <summary>左上の頂点を親要素からの相対座標で取得または設定します。</summary>
    public Vector TopLeft { get; set; } = (0, 0);

    /// <summary>右上の頂点を親要素からの相対座標で取得または設定します。</summary>
    public Vector TopRight { get; set; } = (0, 0);

    /// <summary>右下の頂点を親要素からの相対座標で取得または設定します。</summary>
    public Vector BottomRight { get; set; } = (0, 0);

    /// <summary>左下の頂点を親要素からの相対座標で取得または設定します。</summary>
    public Vector BottomLeft { get; set; } = (0, 0);

    /// <summary>
    /// スプライトの色調を取得または設定します。
    /// </summary>
    public Color TintColor { get; set; }

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (Texture is not { } tex)
            return;

        queue.Enqueue(
            new DrawDeformedTextureCommand
            {
                Texture = tex,
                ModelMatrix = Parent?.ModelMatrix ?? System.Numerics.Matrix4x4.Identity,
                TintColor = TintColor,
                TopLeft = TopLeft,
                TopRight = TopRight,
                BottomRight = BottomRight,
                BottomLeft = BottomLeft,
                Material = Material,
            }
        );
    }
}
