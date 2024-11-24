using System.Drawing;
using Promete.Graphics;

namespace Promete.Nodes;

public class NineSliceSprite(Texture9Sliced texture, Color? tintColor = default) : Node
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
