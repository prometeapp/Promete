using System.Drawing;
using Promete.Graphics;
using Promete.Nodes.Renderer;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes;

/// <summary>
/// 9スライス方式でテクスチャを描画するノードです。
/// </summary>
public class NineSliceSprite(Texture9Sliced texture, Color? tintColor = default) : Node
{
    /// <summary>
    /// テクスチャを取得または設定します。
    /// </summary>
    public Texture9Sliced Texture { get; set; } = texture;

    /// <summary>
    /// 色合いを取得または設定します。
    /// </summary>
    public Color TintColor { get; set; } = tintColor ?? Color.White;

    internal override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        var left = Texture.TopLeft.Size.X;
        var right = Texture.TopRight.Size.X;
        var top = Texture.TopLeft.Size.Y;
        var bottom = Texture.BottomLeft.Size.Y;

        var xSpan = Width - left - right;
        var ySpan = Height - top - bottom;

        void Enqueue(Texture2D tex, Vector pivot, float? width = null, float? height = null)
        {
            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tex,
                ModelMatrix = ModelMatrix,
                TintColor = TintColor,
                Width = width ?? tex.Size.X,
                Height = height ?? tex.Size.Y,
                Pivot = pivot,
                Material = Material,
            });
        }

        Enqueue(Texture.TopLeft,     (0, 0));
        Enqueue(Texture.TopCenter,    Vector.Right * left,             xSpan);
        Enqueue(Texture.TopRight,     Vector.Right * (left + xSpan));
        Enqueue(Texture.MiddleLeft,   Vector.Down * top,               null, ySpan);
        Enqueue(Texture.MiddleCenter, (left, top),                     xSpan, ySpan);
        Enqueue(Texture.MiddleRight,  (left + xSpan, top),             null, ySpan);
        Enqueue(Texture.BottomLeft,   (0, top + ySpan));
        Enqueue(Texture.BottomCenter, (left, top + ySpan),             xSpan);
        Enqueue(Texture.BottomRight,  (left + xSpan, top + ySpan));
    }
}
