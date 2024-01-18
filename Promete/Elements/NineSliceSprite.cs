using System.Drawing;
using System.IO;
using Promete.Graphics;

namespace Promete.Elements;

public class NineSliceSprite : ElementBase
{
	/// <summary>
	/// Get or set the texture.
	/// </summary>
	/// <value></value>
	public Texture9Sliced Texture { get; set; }

	/// <summary>
	/// Get or set the tint color.
	/// </summary>
	/// <value></value>
	public Color TintColor { get; set; } = Color.White;

	public NineSliceSprite(Texture9Sliced texture)
	{
		Texture = texture;
		base.Size = Texture.Size;
	}
}
