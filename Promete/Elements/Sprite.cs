using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Sprite(ITexture texture) : ElementBase
{
	public ITexture? Texture { get; set; } = texture;

	public Color? TintColor { get; set; }

	public override VectorInt Size
	{
		get => size ?? Texture?.Size ?? (0, 0);
		set => size = value;
	}

	public void ResetSize()
	{
		size = null;
	}

	private VectorInt? size;
}
