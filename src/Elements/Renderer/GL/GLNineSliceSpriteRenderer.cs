using Promete.Elements.Renderer.GL.Helper;

namespace Promete.Elements.Renderer.GL;

public class GLNineSliceSpriteRenderer(GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var el = (NineSliceSprite)element;

		var left = el.Texture.TopLeft.Size.X;
		var right = el.Texture.TopRight.Size.X;
		var top = el.Texture.TopLeft.Size.Y;
		var bottom = el.Texture.BottomLeft.Size.Y;

		var xSpan = el.Width - left - right;
		var ySpan = el.Height - top - bottom;
		var loc = el.AbsoluteLocation;
		var scale = el.AbsoluteScale;

		void Draw(Texture2D tex, Vector location, float? width = null, float? height = null)
		{
			helper.Draw(tex, loc + location * scale, scale, el.TintColor, width, height);
		}

		// 9枚を全て描画する
		Draw(el.Texture.TopLeft, (0, 0));
		Draw(el.Texture.TopCenter, Vector.Right * left, xSpan);
		Draw(el.Texture.TopRight, Vector.Right * (left + xSpan));
		Draw(el.Texture.MiddleLeft, Vector.Down * top, null, ySpan);
		Draw(el.Texture.MiddleCenter, (left, top), xSpan, ySpan);
		Draw(el.Texture.MiddleRight, (left + xSpan, top), null, ySpan);
		Draw(el.Texture.BottomLeft, (0, top + ySpan), null);
		Draw(el.Texture.BottomCenter, (left, top + ySpan), xSpan);
		Draw(el.Texture.BottomRight, (left + xSpan, top + ySpan), null);
	}
}
