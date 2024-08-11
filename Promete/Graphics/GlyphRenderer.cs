using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Promete.Internal;
using Promete.Markup;
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

	public Texture2D Generate(string text, TextRenderingOptions options)
	{
		var font = options.Font;
		var imageSharpFont = ResolveFont(font);
		var size = GetTextBounds(text, imageSharpFont);
		var imageSize = options.Size == default ? (VectorInt)size.Size + (8, 8) : options.Size;
		using var img = new Image<Rgba32>(imageSize.X, imageSize.Y);

		var textOptions = new RichTextOptions(imageSharpFont)
		{
			WrappingLength = options.WordWrap ? options.Size.X : 0,
			VerticalAlignment = options.VerticalAlignment.ToSixLabors(),
			HorizontalAlignment = (SixLabors.Fonts.HorizontalAlignment)options.HorizontalAlignment,
			LineSpacing = options.LineSpacing,
		};
		var drawingOptions = new DrawingOptions();

		if (options.UseRichText)
		{
			var (t, decorations) = PtmlParser.Parse(text);
			text = t;
			var runs = new List<RichTextRun>();
			runs.AddRange(decorations.Select(d => CreateRunFromDecoration(d, font)));
			textOptions.TextRuns = runs.AsReadOnly();
		}

		if (!font.IsAntialiased)
		{
			textOptions.KerningMode = KerningMode.None;
			textOptions.TextAlignment = TextAlignment.Start;
			drawingOptions.GraphicsOptions.Antialias = false;
		}

		var brush = new SolidBrush(options.TextColor.ToSixLabors());
		Pen? pen = null;
		if (options.BorderColor is { } borderColor)
		{
			pen = new SolidPen(borderColor.ToSixLabors(), options.BorderThickness);
		}

		img.Mutate(ctx => ctx.DrawText(drawingOptions, textOptions, text, brush, pen));
		return window.TextureFactory.LoadFromImageSharpImage(img);
	}

	[Obsolete("Use Generate(string text, TextRenderingOptions options).")]
	public Texture2D Generate(string text, Font font, SDColor? color, SDColor? borderColor, int borderThickness)
	{
		return Generate(text, new TextRenderingOptions
		{
			Font = font,
			TextColor = color ?? SDColor.Black,
			BorderColor = borderColor,
			BorderThickness = borderThickness,
		});
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

	private RichTextRun CreateRunFromDecoration(PtmlDecoration decoration, Font baseFont)
	{
		var run = new RichTextRun
		{
			Start = decoration.Start,
			End = decoration.End,
		};

		switch (decoration.TagName.ToLowerInvariant())
		{
			case "b":
			{
				run.Font = ResolveFont(With(baseFont, style: FontStyle.Bold));
				break;
			}
			case "i":
			{
				run.Font = ResolveFont(With(baseFont, style: FontStyle.Italic));
				break;
			}
			case "color":
			{
				var color = FromHtml(decoration.Attribute);
				run.Brush = new SolidBrush(color.ToSixLabors());
				break;
			}
			case "size":
			{
				if (int.TryParse(decoration.Attribute, out var size))
				{
					run.Font = ResolveFont(With(baseFont, size: size));
				}
				break;
			}
		}

		return run;
	}

	private Font With(Font baseFont, float? size = null, FontStyle? style = null, bool? isAntialiased = null)
	{
		if (baseFont.Path != null)
		{
			return new Font(
				baseFont.Path,
				size ?? baseFont.Size,
				style ?? baseFont.FontStyle,
				isAntialiased ?? baseFont.IsAntialiased);
		}

		if (baseFont.Stream != null)
		{
			return new Font(
				baseFont.Stream,
				baseFont.Id,
				size ?? baseFont.Size,
				style ?? baseFont.FontStyle,
				isAntialiased ?? baseFont.IsAntialiased);
		}

		throw new InvalidOperationException("BUG: This font does not have either Path or Stream.");
	}

	private static SDColor FromHtml(string colorName)
	{
		try
		{
			return ColorTranslator.FromHtml(colorName);
		}
		catch (Exception e)
		{
			return SDColor.Black;
		}
	}
}
