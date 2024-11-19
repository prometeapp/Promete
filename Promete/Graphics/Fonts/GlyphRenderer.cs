using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Promete.Internal;
using Promete.Markup;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SDColor = System.Drawing.Color;

namespace Promete.Graphics.Fonts;

public static class GlyphRenderer
{
	private static readonly Dictionary<object, FontFamily> FontCache = new();
	private static readonly FontCollection FontCollection = new();

	private const char ZeroWidthSpace = '\u200B';

	public static Rect GetTextBounds(string text, Font font)
	{
		var f = ResolveFont(font);
		return GetTextBounds(text, f);
	}

	public static Texture2D GenerateText(this TextureFactory factory, string text, TextRenderingOptions options)
	{
		var font = options.Font;
		var imageSharpFont = ResolveFont(font);
		var size = GetTextBounds(text, imageSharpFont);
		var imageSize = options.Size == default ? (VectorInt)size.Size + (8, 8) : options.Size;
		using var img = new Image<Rgba32>(imageSize.X, imageSize.Y);

		var textOptions = new RichTextOptions(imageSharpFont)
		{
			WrappingLength = options.WordWrap ? options.Size.X : -1f,
			VerticalAlignment = options.VerticalAlignment.ToSixLabors(),
			HorizontalAlignment = options.HorizontalAlignment.ToSixLabors(),
			LineSpacing = options.LineSpacing,
		};
		var drawingOptions = new DrawingOptions();

		if (options.UseRichText)
		{
			var (t, decorations) = PtmlParser.Parse(text);
			// Note: ImageSharpの不具合により、TextRun.Endが文字列の末尾インデックスと同じのときに挙動がおかしくなるため、workaroundとしてZeroWidthSpaceを追加する
			//       下記が修正され次第対応を外す
			//       https://github.com/SixLabors/ImageSharp.Drawing/issues/337
			text = t + ZeroWidthSpace;

			// Note: decorationsを逆順にして重複を除去することで、後に適用されたデコレーションが優先されるようにする
			var runs = decorations
				.Select(d => CreateRunFromDecoration(d, font))
				.OfType<RichTextRun>()
				.Reverse()
				.DistinctBy(r => (r.Start, r.End))
				.Reverse()
				.ToList();
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
		return factory.LoadFromImageSharpImage(img);
	}

	private static Rect GetTextBounds(string text, SixLabors.Fonts.Font font)
	{
		var size = TextMeasurer.MeasureBounds(text, new TextOptions(font));
		return new Rect(0, 0, (int)size.Right, (int)size.Bottom);
	}

	private static SixLabors.Fonts.Font ResolveFont(Font f)
	{
		FontFamily family;
		if (FontCache.TryGetValue(f.Id, out var value))
		{
			family = value;
		}
		else if (f.Path != null && File.Exists(f.Path))
		{
			family = FontCollection.Add(f.Path);
		}
		else if (f.Path != null)
		{
			family = SystemFonts.Get(f.Path);
		}
		else if (f.Stream != null)
		{
			f.Stream.Position = 0;
			family = FontCollection.Add(f.Stream);
		}
		else
		{
			throw new ArgumentException("Font class must have either a path or a stream.");
		}

		FontCache[f.Id] = family;
		return new SixLabors.Fonts.Font(family, f.Size, (SixLabors.Fonts.FontStyle)f.FontStyle);
	}

	private static RichTextRun? CreateRunFromDecoration(PtmlDecoration decoration, Font baseFont)
	{
		// Start == Endの場合は無視
		if (decoration.Start == decoration.End) return null;

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
				if (string.IsNullOrEmpty(decoration.Attribute)) break;
				var color = FromHtml(decoration.Attribute);
				run.Brush = new SolidBrush(color.ToSixLabors());
				break;
			}
			case "size":
			{
				if (int.TryParse(decoration.Attribute, out var size))
				{
					run.Font = ResolveFont(With(baseFont, size));
				}
				break;
			}
			default:
				return null;
		}

		return run;
	}

	private static Font With(Font baseFont, float? size = null, FontStyle? style = null, bool? isAntialiased = null)
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
