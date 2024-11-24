using Promete.Graphics;
using Promete.Nodes.Renderer.GL.Helper;

namespace Promete.Nodes.Renderer.GL;

public class GLNineSliceSpriteRenderer(GLTextureRendererHelper helper) : NodeRendererBase
{
    public override void Render(Node node)
    {
        var sprite = (NineSliceSprite)node;

        var left = sprite.Texture.TopLeft.Size.X;
        var right = sprite.Texture.TopRight.Size.X;
        var top = sprite.Texture.TopLeft.Size.Y;
        var bottom = sprite.Texture.BottomLeft.Size.Y;

        var xSpan = sprite.Width - left - right;
        var ySpan = sprite.Height - top - bottom;
        var loc = sprite.AbsoluteLocation;
        var scale = sprite.AbsoluteScale;

        void Draw(Texture2D tex, Vector location, float? width = null, float? height = null)
        {
            var w = width ?? tex.Size.X;
            var h = height ?? tex.Size.Y;
            helper.Draw(tex, sprite, sprite.TintColor, location, w, h);
        }

        // 9枚を全て描画する
        Draw(sprite.Texture.TopLeft, (0, 0));
        Draw(sprite.Texture.TopCenter, Vector.Right * left, xSpan);
        Draw(sprite.Texture.TopRight, Vector.Right * (left + xSpan));
        Draw(sprite.Texture.MiddleLeft, Vector.Down * top, null, ySpan);
        Draw(sprite.Texture.MiddleCenter, (left, top), xSpan, ySpan);
        Draw(sprite.Texture.MiddleRight, (left + xSpan, top), null, ySpan);
        Draw(sprite.Texture.BottomLeft, (0, top + ySpan));
        Draw(sprite.Texture.BottomCenter, (left, top + ySpan), xSpan);
        Draw(sprite.Texture.BottomRight, (left + xSpan, top + ySpan));
    }
}
