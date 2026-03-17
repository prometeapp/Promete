using Promete.Graphics;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes.Renderer.GL;

public class GLNineSliceSpriteRenderer : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        var sprite = (NineSliceSprite)node;

        var left = sprite.Texture.TopLeft.Size.X;
        var right = sprite.Texture.TopRight.Size.X;
        var top = sprite.Texture.TopLeft.Size.Y;
        var bottom = sprite.Texture.BottomLeft.Size.Y;

        var xSpan = sprite.Width - left - right;
        var ySpan = sprite.Height - top - bottom;

        void Enqueue(Texture2D tex, Vector pivot, float? width = null, float? height = null)
        {
            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tex,
                ModelMatrix = sprite.ModelMatrix,
                TintColor = sprite.TintColor,
                Width = width ?? tex.Size.X,
                Height = height ?? tex.Size.Y,
                Pivot = pivot,
            });
        }

        Enqueue(sprite.Texture.TopLeft,     (0, 0));
        Enqueue(sprite.Texture.TopCenter,    Vector.Right * left,             xSpan);
        Enqueue(sprite.Texture.TopRight,     Vector.Right * (left + xSpan));
        Enqueue(sprite.Texture.MiddleLeft,   Vector.Down * top,               null, ySpan);
        Enqueue(sprite.Texture.MiddleCenter, (left, top),                     xSpan, ySpan);
        Enqueue(sprite.Texture.MiddleRight,  (left + xSpan, top),             null, ySpan);
        Enqueue(sprite.Texture.BottomLeft,   (0, top + ySpan));
        Enqueue(sprite.Texture.BottomCenter, (left, top + ySpan),             xSpan);
        Enqueue(sprite.Texture.BottomRight,  (left + xSpan, top + ySpan));
    }
}
