using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Promete.Graphics;

/// <summary>
/// Depresents font-family, size, and font style.
/// </summary>
public class Font
{
	/// <summary>
	/// Get a path to this font, or font-family name.
	/// </summary>
	public string? Path { get; private set; }

	/// <summary>
	/// Get a font identifier.
	/// </summary>
	public string Id { get; private set; }

	/// <summary>
	/// Get a path to this font, or font-family name.
	/// </summary>
	public Stream? Stream { get; private set; }

	/// <summary>
	/// Get a size of this font.
	/// </summary>
	public float Size { get; private set; }

	/// <summary>
	/// Get a style of this font.
	/// </summary>
	public FontStyle FontStyle { get; private set; }

	/// <summary>
	/// Initialize a new instance of <see cref="Font"/> class.
	/// </summary>
	/// <param name="path">relative path to the font, or font-family name of system fonts.</param>
	/// <param name="size">font size by pixel unit.</param>
	/// <param name="style">font style.</param>
	public Font(string path, float size = 16, FontStyle style = FontStyle.Normal)
	{
		Path = path;
		Id = path;
		Size = size;
		FontStyle = style;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="Font"/> class.
	/// </summary>
	/// <param name="stream">Stream of the font.</param>
	/// <param name="id">An ID to cache this font data.</param>
	/// <param name="size">font size by pixel unit.</param>
	/// <param name="style">font style.</param>
	public Font(Stream stream, string id, float size = 16, FontStyle style = FontStyle.Normal)
	{
		Id = id;
		Stream = stream;
		Size = size;
		FontStyle = style;
	}

	/// <summary>
	/// Get a default font.
	/// </summary>
	/// <param name="size">Font size.</param>
	/// <param name="style">Font style.</param>
	/// <returns>Generated defualt font.</returns>
	public static Font GetDefault(float size = 16, FontStyle style = FontStyle.Normal)
	{
		return new Font(defaultFont, "__PROMETE_SYSTEM_EMBEDDED_FONT_MPLUS__", size, style);
	}

	public override bool Equals(object? obj)
	{
		return obj is Font font &&
		       Path == font.Path &&
		       EqualityComparer<Stream?>.Default.Equals(Stream, font.Stream) &&
		       Size == font.Size &&
		       FontStyle == font.FontStyle;
	}

	public override int GetHashCode()
	{
		return System.HashCode.Combine(Path, Stream, Size, FontStyle);
	}

	private static readonly Stream defaultFont = EmbeddedResource.GetResourceAsStream("Promete.Resources.font.ttf");
}
