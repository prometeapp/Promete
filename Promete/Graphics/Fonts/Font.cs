using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Promete.Internal;
using Promete.Markup;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

namespace Promete.Graphics.Fonts;

/// <summary>
/// ファイルおよびストリームから生成可能なベクターフォントを表します。
/// </summary>
public class Font : IFont
{
    /// <summary>
    /// フォントサイズを取得します。
    /// </summary>
    public float Size { get; }

    /// <summary>
    /// フォントスタイルを取得します。
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// アンチエイリアスが有効かどうかを取得します。
    /// </summary>
    public bool IsAntialiased { get; }

    private const char ZeroWidthSpace = '\u200B';

    private static readonly Dictionary<object, FontFamily> FontCache = new();
    private static readonly FontCollection FontCollection = new();

    private static readonly Lazy<FontFamily> DefaultFontFamily = new(() =>
    {
        if (!SystemFonts.Families.Any()) throw new NotSupportedException("No font families found.");
        Span<string> families =
            OperatingSystem.IsWindows()
                ? ["BIZ UDGothic", "Yu Gothic", "Meiryo", "MS Gothic", "Arial"]
                : OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsWatchOS() ||
                  OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst()
                    ? ["BIZ UDGothic", "Hiragino Sans", "Helvetica", "Arial"]
                    : OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() || OperatingSystem.IsAndroid()
                        ?
                        [
                            "Noto Sans CJK JP Regular", "Noto Sans CJK JP", "Droid Sans Fallback", "DejaVu Sans",
                            "Liberation Sans", "Arial"
                        ]
                        : throw new NotSupportedException("Unsupported platform.");

        foreach (var family in families)
            if (SystemFonts.TryGet(family, out var f))
                return f;

        // どれも見つからなかった場合は最初のフォントを返す
        return SystemFonts.Families.First();
    });

    private readonly SixLabors.Fonts.Font _internalFont;

    protected Font(SixLabors.Fonts.Font internalFont, float size, FontStyle style, bool isAntialiased)
    {
        _internalFont = internalFont;
        Size = size;
        Style = style;
        IsAntialiased = isAntialiased;
    }

    /// <inheritdoc />
    public Rect GetTextBounds(string text, TextRenderingOptions options)
    {
        (text, var textOptions) = CreateTextOptions(options, text);
        var size = TextMeasurer.MeasureBounds(text, textOptions);
        return new Rect(0, 0, (int)size.Right + 1, (int)size.Bottom + 1);
    }

    /// <inheritdoc />
    public Texture2D GenerateTexture(TextureFactory factory, string text, TextRenderingOptions options)
    {
        (text, var textOptions) = CreateTextOptions(options, text);
        var drawingOptions = CreateDrawingOptions(options);

        var size = TextMeasurer.MeasureBounds(text, textOptions);
        var imageSize = new VectorInt((int)size.Right, (int)size.Bottom) + VectorInt.One;
        if (imageSize.X == 0 || imageSize.Y == 0) return default;

        using var img = new Image<Rgba32>(imageSize.X, imageSize.Y);

        var brush = new SolidBrush(options.TextColor.ToSixLabors());
        Pen? pen = null;
        if (options.BorderColor is { } borderColor)
            pen = new SolidPen(borderColor.ToSixLabors(), options.BorderThickness);

        img.Mutate(ctx => ctx.DrawText(drawingOptions, textOptions, text, brush, pen));
        return factory.LoadFromImageSharpImage(img);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Font f && f._internalFont == _internalFont && f.Size.Equals(Size) && f.Style == Style && f.IsAntialiased == IsAntialiased;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_internalFont, Size, Style, IsAntialiased);
    }

    /// <summary>
    /// フォントサイズを変更した新しいフォントを生成します。
    /// </summary>
    /// <param name="size">新しいフォントサイズ。</param>
    /// <returns>生成されたフォント。</returns>
    public Font With(float size)
    {
        return new Font(_internalFont, size, Style, IsAntialiased);
    }

    /// <summary>
    /// フォントスタイルを変更した新しいフォントを生成します。
    /// </summary>
    /// <param name="style">新しいフォントスタイル。</param>
    /// <returns>生成されたフォント。</returns>
    public Font With(FontStyle style)
    {
        return new Font(_internalFont, Size, style, IsAntialiased);
    }

    /// <summary>
    /// フォントサイズおよびスタイルを変更した新しいフォントを生成します。
    /// </summary>
    /// <param name="size">新しいフォントサイズ。</param>
    /// <param name="style">新しいフォントスタイル。</param>
    /// <returns>生成されたフォント。</returns>
    public Font With(float size, FontStyle style)
    {
        return new Font(_internalFont, size, style, IsAntialiased);
    }

    /// <summary>
    /// フォントサイズおよびスタイル、アンチエイリアスの有効/無効を変更した新しいフォントを生成します。
    /// </summary>
    /// <param name="size">新しいフォントサイズ。</param>
    /// <param name="style">新しいフォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    public Font With(float size, FontStyle style, bool isAntialiased)
    {
        return new Font(_internalFont, size, style, isAntialiased);
    }

    /// <summary>
    /// ファイルパスを指定し、フォントを生成します。
    /// </summary>
    /// <param name="path">フォントファイルのパス。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    /// <exception cref="FileNotFoundException">フォントファイルが見つからない場合。</exception>
    public static Font FromFile(string path, float size = 16, FontStyle style = FontStyle.Normal, bool isAntialiased = true)
    {
        if (FontCache.TryGetValue(path, out var f)) return FromFontFamily(f, size, style, isAntialiased);

        if (!File.Exists(path)) throw new FileNotFoundException("Font file not found.", path);
        var family = FontCollection.Add(path);
        FontCache[path] = family;

        return FromFontFamily(family, size, style, isAntialiased);
    }

    /// <summary>
    /// ストリームを指定し、フォントを生成します。
    /// </summary>
    /// <param name="stream">フォントファイルのストリーム。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    public static Font FromFile(Stream stream, float size = 16, FontStyle style = FontStyle.Normal, bool isAntialiased = true)
    {
        if (FontCache.TryGetValue(stream, out var f)) return FromFontFamily(f, size, style, isAntialiased);
        stream.Position = 0;
        var family = FontCollection.Add(stream);
        FontCache[stream] = family;
        return FromFontFamily(family, size, style, isAntialiased);
    }

    /// <summary>
    /// システムフォントを指定し、フォントを生成します。
    /// </summary>
    /// <param name="fontFamily">指定するフォントファミリー。指定可能な文字列は環境によって異なります。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    public static Font FromSystem(string fontFamily, float size = 16, FontStyle style = FontStyle.Normal, bool isAntialiased = true)
    {
        return FromSystem(fontFamily, CultureInfo.CurrentCulture, size, style, isAntialiased);
    }

    /// <summary>
    /// システムフォントを指定し、フォントを生成します。
    /// </summary>
    /// <param name="fontFamily">指定するフォントファミリー。指定可能な文字列は環境によって異なります。</param>
    /// <param name="culture">カルチャ情報。</param>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    public static Font FromSystem(string fontFamily, CultureInfo culture, float size = 16,
        FontStyle style = FontStyle.Normal, bool isAntialiased = true)
    {
        if (FontCache.TryGetValue(fontFamily, out var f)) return FromFontFamily(f, size, style, isAntialiased);

        var family = SystemFonts.Get(fontFamily, culture);
        FontCache[fontFamily] = family;
        return FromFontFamily(family, size, style, isAntialiased);
    }

    /// <summary>
    /// デフォルトのフォントを生成します。
    /// </summary>
    /// <param name="size">フォントサイズ。</param>
    /// <param name="style">フォントスタイル。</param>
    /// <param name="isAntialiased">アンチエイリアスの有効/無効。</param>
    /// <returns>生成されたフォント。</returns>
    public static Font GetDefault(float size = 16, FontStyle style = FontStyle.Normal, bool isAntialiased = true)
    {
        return FromFontFamily(DefaultFontFamily.Value, size, style, isAntialiased);
    }

    private static Font FromFontFamily(FontFamily family, float size, FontStyle style, bool isAntialiased)
    {
        var internalFont = new SixLabors.Fonts.Font(family, size, (SixLabors.Fonts.FontStyle)style);
        return new Font(internalFont, size, style, isAntialiased);
    }

    private (string plainText, RichTextOptions options) CreateTextOptions(TextRenderingOptions options, string text)
    {
        var textOptions = new RichTextOptions(_internalFont)
        {
            WrappingLength = options.WordWrap ? options.Size.X : -1f,
            VerticalAlignment = options.VerticalAlignment.ToSixLabors(),
            HorizontalAlignment = options.HorizontalAlignment.ToSixLabors(),
            LineSpacing = options.LineSpacing,
            KerningMode = IsAntialiased ? default : KerningMode.None,
            TextAlignment = IsAntialiased ? default : TextAlignment.Start
        };
        if (!options.UseRichText) return (text, textOptions);

        var (t, decorations) = PtmlParser.Parse(text);
        // Note: ImageSharpの不具合により、TextRun.Endが文字列の末尾インデックスと同じのときに挙動がおかしくなるため、workaroundとしてZeroWidthSpaceを追加する
        //       下記が修正され次第対応を外す
        //       https://github.com/SixLabors/ImageSharp.Drawing/issues/337
        text = t + ZeroWidthSpace;

        // Note: decorationsを逆順にして重複を除去することで、後に適用されたデコレーションが優先されるようにする
        var runs = decorations
            .Select(CreateRunFromDecoration)
            .OfType<RichTextRun>()
            .Reverse()
            .DistinctBy(r => (r.Start, r.End))
            .Reverse()
            .ToList();
        textOptions.TextRuns = runs.AsReadOnly();
        return (text, textOptions);
    }

    private DrawingOptions CreateDrawingOptions(TextRenderingOptions options)
    {
        return new DrawingOptions
        {
            GraphicsOptions =
            {
                Antialias = IsAntialiased
            }
        };
    }

    private RichTextRun? CreateRunFromDecoration(PtmlDecoration decoration)
    {
        // Start == Endの場合は無視
        if (decoration.Start == decoration.End) return null;

        var run = new RichTextRun
        {
            Start = decoration.Start,
            End = decoration.End
        };

        switch (decoration.TagName.ToLowerInvariant())
        {
            case "b":
            {
                run.Font = new SixLabors.Fonts.Font(_internalFont, SixLabors.Fonts.FontStyle.Bold);
                break;
            }
            case "i":
            {
                run.Font = new SixLabors.Fonts.Font(_internalFont, SixLabors.Fonts.FontStyle.Italic);
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
                    run.Font = new SixLabors.Fonts.Font(_internalFont, size);
                break;
            }
            default:
                return null;
        }

        return run;
    }

    private static Color FromHtml(string colorName)
    {
        try
        {
            return ColorTranslator.FromHtml(colorName);
        }
        catch (Exception)
        {
            return Color.Black;
        }
    }
}
