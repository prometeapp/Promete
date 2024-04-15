using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Sprite(Texture2D? texture = null, Color? tintColor = default) : ElementBase
{
	public Texture2D? Texture { get; set; } = texture;

	public Color TintColor { get; set; } = tintColor ?? Color.White;

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
