using System;
using System.Collections.Generic;
using System.IO;
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

	public Texture2D Generate(string text, Font font, SDColor? color, SDColor? borderColor, int borderThickness)
	{
		var imageSharpFont = ResolveFont(font);
		var size = GetTextBounds(text, imageSharpFont);
		using var img = new Image<Rgba32>((int)size.Width + 8, (int)size.Height + 8);
		var col = color ?? SDColor.Black;
		var imageSharpColor = Color.FromRgba(col.R, col.G, col.B, col.A);

		var textOptions = new RichTextOptions(imageSharpFont);
		var drawingOptions = new DrawingOptions();

		if (!font.IsAntialiased)
		{
			textOptions.HorizontalAlignment = HorizontalAlignment.Left;
			textOptions.VerticalAlignment = VerticalAlignment.Top;
			textOptions.KerningMode = KerningMode.None;
			textOptions.TextAlignment = TextAlignment.Start;
			drawingOptions.GraphicsOptions.Antialias = false;
		}

		var brush = new SolidBrush(imageSharpColor);
		if (borderColor != null)
		{
			var bc = borderColor.Value;
			var imageSharpBorderColor = Color.FromRgba(bc.R, bc.G, bc.B, bc.A);
			var pen = new SolidPen(imageSharpBorderColor, borderThickness);
			img.Mutate(ctx =>
			{
				ctx.DrawText(drawingOptions, textOptions, text, brush, pen);
			});
		}
		else
		{
			img.Mutate(ctx => ctx.DrawText(drawingOptions, textOptions, text, brush, null));
		}

		return window.TextureFactory.LoadFromImageSharpImage(img);
	}

	private Rect GetTextBounds(string text, SixLabors.Fonts.Font font)
	{
		var size = TextMeasurer.MeasureBounds(text, new TextOptions(font));
		return new Rect(0, 0, (int)size.Right, (int)size.Bottom);
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
