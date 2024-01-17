using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Sprite : ElementBase
{
	public ITexture? Texture { get; set; }

	public Color? TintColor { get; set; }

	public override VectorInt Size
	{
		get => size ?? Texture?.Size ?? (0, 0);
		set => size = value;
	}

	public Sprite(ITexture texture)
	{
		Texture = texture;
	}

	public void ResetSize()
	{
		size = null;
	}

	private VectorInt? size;
}
