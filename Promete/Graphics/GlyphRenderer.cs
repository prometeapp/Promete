using System;
using System.Collections.Generic;
using System.IO;
using Promete.Elements;
using Promete.Windowing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using SDColor = System.Drawing.Color;

namespace Promete.Graphics;

public class GlyphRenderer(IWindow window)
{
	private readonly Dictionary<object, FontFamily> fontCache = new();
	private readonly FontCollection fontCollection = new();

	public Rect GetTextBounds(string text, Font font)
	{
		var f = ResolveFont(font);
		return GetTextBounds(text, f);
	}

	public ITexture Generate(string text, Font font, SDColor? color, SDColor? borderColor, int borderThickness)
	{
		var f = ResolveFont(font);
		var size = GetTextBounds(text, f);
		using var img = new Image<Rgba32>((int)size.Width + 8, (int)size.Height + 8);
		var col = color ?? SDColor.Black;
		var isColor = Color.FromRgba(col.R, col.G, col.B, col.A);

		if (borderColor != null)
		{
			var bc = borderColor.Value;
			var isBorderColor = Color.FromRgba(bc.R, bc.G, bc.B, bc.A);
			img.Mutate(ctx => ctx.DrawText(text, f, new SolidBrush(isColor), new SolidPen(isBorderColor, borderThickness),
				PointF.Empty));
		}
		else
		{
			img.Mutate(ctx => ctx.DrawText(text, f, isColor, PointF.Empty));
		}

		return window.TextureFactory.LoadFromImageSharpImage(img);
	}

	private Rect GetTextBounds(string text, SixLabors.Fonts.Font font)
	{
		var size = TextMeasurer.MeasureBounds(text, new TextOptions(font));
		return new Rect(0, 0, (int)size.Width, (int)size.Height);
	}

	private SixLabors.Fonts.Font ResolveFont(Font f)
	{
		FontFamily family;
		if (fontCache.TryGetValue(f.Id, out var value))
		{
			family = value;
		}
		else if (f.Path != null && File.Exists(f.Path))
		{
			family = fontCollection.Add(f.Path);
		}
		else if (f.Path != null)
		{
			family = SystemFonts.Get(f.Path);
		}
		else if (f.Stream != null)
		{
			f.Stream.Position = 0;
			family = fontCollection.Add(f.Stream);
		}
		else
		{
			throw new ArgumentException("Font class must have either a path or a stream.");
		}

		fontCache[f.Id] = family;
		return new SixLabors.Fonts.Font(family, f.Size, (SixLabors.Fonts.FontStyle)f.FontStyle);
	}
}
