using System.Drawing;
using System.IO;
using Promete.Graphics;

namespace Promete.Elements;

public class NineSliceSprite(
	Texture9Sliced texture,
	Color? tintColor = default,
	string name = "",
	Vector? location = default,
	Vector? scale = default) : ElementBase(name, location, scale, texture.Size)
{
	/// <summary>
	/// Get or set the texture.
	/// </summary>
	/// <value></value>
	public Texture9Sliced Texture { get; set; } = texture;

	/// <summary>
	/// Get or set the tint color.
	/// </summary>
	/// <value></value>
	public Color TintColor { get; set; } = tintColor ?? Color.White;
}
