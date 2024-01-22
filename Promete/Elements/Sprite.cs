using System.Drawing;
using Promete.Graphics;

namespace Promete.Elements;

public class Sprite(
	ITexture texture,
	Color? tintColor = default,
	string name = "",
	Vector? location = default,
	Vector? scale = default) : ElementBase(name, location, scale, texture.Size)
{
	public ITexture? Texture { get; set; } = texture;

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
