using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Promete.Markup;

namespace Promete.Graphics.Fonts;

/// <summary>
/// テキストを行に分割し、各グリフの配置位置を決定します。
/// </summary>
/// <remarks>
/// このクラスは <see cref="IGlyphSource" /> が提供する送り幅とメトリクスのみに依存し、
/// グリフの実体には触れません。そのため、ベクターフォント・ビットマップフォント・外字が
/// 混在していても同一の処理で行を組み立てられます。
/// </remarks>
public static class TextLayoutEngine
{
    /// <summary>
    /// 行頭に置いてはならない文字。句読点・閉じ括弧・拗促音・繰り返し記号など。
    /// </summary>
    private static readonly SearchValues<char> LineStartProhibited = SearchValues.Create(
        ",)]｝、〕〉》」』】〙〗〟'\"｠»"
            + "ゝゞーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎゕゖ"
            + "。.:;/？！?!‼⁇⁈⁉・:;。.、,"
            + "々〻‐゠–〜～"
    );

    /// <summary>
    /// 行末に置いてはならない文字。開き括弧など。
    /// </summary>
    private static readonly SearchValues<char> LineEndProhibited = SearchValues.Create(
        "([{｟〔〈《「『【〘〖〝'\"«（［｛"
    );

    /// <summary>
    /// テキストをレイアウトします。
    /// </summary>
    /// <param name="text">レイアウトするテキスト。</param>
    /// <param name="font">基準となるフォント。</param>
    /// <param name="options">レイアウトのオプション。</param>
    public static TextLayout Layout(string text, Font font, TextRenderingOptions options)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(text))
            return TextLayout.Empty;

        var (plainText, attributes) = ResolveAttributes(text, font, options);
        var items = CollectItems(plainText, font, attributes, options);
        var lineRanges = SplitIntoLines(items, options);
        ApplyMaxLines(items, lineRanges, font, options);

        return Place(plainText, items, lineRanges, font, options);
    }

    /// <summary>
    /// テキストを含む矩形のサイズを取得します。
    /// </summary>
    public static VectorInt Measure(string text, Font font, TextRenderingOptions options)
    {
        return Layout(text, font, options).Size;
    }

    /// <summary>
    /// PTML の装飾を解決し、文字ごとの描画属性を求めます。
    /// </summary>
    private static (string PlainText, CharAttribute[] Attributes) ResolveAttributes(
        string text,
        Font font,
        TextRenderingOptions options
    )
    {
        var plainText = text;
        var decorations = Array.Empty<PtmlDecoration>() as IReadOnlyList<PtmlDecoration>;

        if (options.UseRichText)
            (plainText, decorations) = PtmlParser.Parse(text);

        var attributes = new CharAttribute[plainText.Length];
        var initial = new CharAttribute(font.RenderOptions, options.TextColor);
        Array.Fill(attributes, initial);

        // 後に適用された装飾が優先されるよう、解析された順に上書きしていく
        foreach (var decoration in decorations)
        {
            var start = Math.Clamp(decoration.Start, 0, plainText.Length);
            var end = Math.Clamp(decoration.End, start, plainText.Length);

            for (var i = start; i < end; i++)
                attributes[i] = ApplyDecoration(attributes[i], decoration, font);
        }

        return (plainText, attributes);
    }

    private static CharAttribute ApplyDecoration(
        CharAttribute attribute,
        PtmlDecoration decoration,
        Font font
    )
    {
        switch (decoration.TagName.ToLowerInvariant())
        {
            case "b":
                return attribute with
                {
                    Options = attribute.Options with
                    {
                        Style = AddStyle(attribute.Options.Style, FontStyle.Bold),
                    },
                };

            case "i":
                return attribute with
                {
                    Options = attribute.Options with
                    {
                        Style = AddStyle(attribute.Options.Style, FontStyle.Italic),
                    },
                };

            case "color":
                return string.IsNullOrEmpty(decoration.Attribute)
                    ? attribute
                    : attribute with { Color = ParseColor(decoration.Attribute) };

            case "size":
                return float.TryParse(
                    decoration.Attribute,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var size
                ) && size > 0
                    ? attribute with { Options = attribute.Options with { Size = size } }
                    : attribute;

            case "tex":
            {
                // 置換文字の位置に、名前で登録された外字を差し込む
                if (string.IsNullOrEmpty(decoration.Attribute))
                    return attribute;
                if (font.Source is not INamedGlyphSource named)
                    return attribute;
                return named.TryGetCodepointByName(decoration.Attribute, out var codepoint)
                    ? attribute with { SubstituteCodepoint = codepoint }
                    : attribute;
            }

            default:
                return attribute;
        }
    }

    private static FontStyle AddStyle(FontStyle current, FontStyle addition)
    {
        return (current, addition) switch
        {
            (FontStyle.Italic, FontStyle.Bold) => FontStyle.BoldItalic,
            (FontStyle.Bold, FontStyle.Italic) => FontStyle.BoldItalic,
            (FontStyle.BoldItalic, _) => FontStyle.BoldItalic,
            _ => addition,
        };
    }

    private static Color ParseColor(string value)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch (Exception)
        {
            return Color.Black;
        }
    }

    /// <summary>
    /// 文字列を走査し、グリフと送り幅を確定させます。
    /// </summary>
    private static List<LayoutItem> CollectItems(
        string text,
        Font font,
        CharAttribute[] attributes,
        TextRenderingOptions options
    )
    {
        var items = new List<LayoutItem>(text.Length);
        var previousCodepoint = -1;

        for (var i = 0; i < text.Length; )
        {
            var isSurrogatePair = char.IsHighSurrogate(text[i]) && i + 1 < text.Length
                && char.IsLowSurrogate(text[i + 1]);
            var codepoint = isSurrogatePair ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
            var length = isSurrogatePair ? 2 : 1;
            var attribute = attributes[i];

            if (codepoint == '\n')
            {
                items.Add(LayoutItem.NewLine(i, attribute));
                previousCodepoint = -1;
                i += length;
                continue;
            }

            // 復帰文字は改行文字とあわせて 1 つの改行として扱う
            if (codepoint == '\r')
            {
                i += length;
                continue;
            }

            // 外字が割り当てられている場合は、置換文字の代わりにそのグリフを描画する
            var resolved = attribute.SubstituteCodepoint >= 0 ? attribute.SubstituteCodepoint : codepoint;

            // 外字を解決できなかった置換文字は、フォントが字形を持っていても描画しない
            var isUnresolvedPlaceholder =
                codepoint == PtmlParser.ObjectReplacementCharacter
                && attribute.SubstituteCodepoint < 0;

            var glyph = default(GlyphInfo);
            var hasGlyph =
                !isUnresolvedPlaceholder
                && font.Source.TryGetGlyph(resolved, attribute.Options, out glyph);
            var advance = hasGlyph ? glyph.Advance + options.LetterSpacing : 0;

            if (hasGlyph && options.UseKerning && previousCodepoint >= 0)
                advance += font.Source.GetKerning(previousCodepoint, resolved, attribute.Options);

            items.Add(
                new LayoutItem(
                    i,
                    resolved,
                    glyph,
                    hasGlyph,
                    advance,
                    attribute,
                    false,
                    CanBreakBefore(items, codepoint, options)
                )
            );

            previousCodepoint = resolved;
            i += length;
        }

        return items;
    }

    /// <summary>
    /// 直前の文字との関係から、この文字の直前で改行できるかどうかを判定します。
    /// </summary>
    private static bool CanBreakBefore(
        List<LayoutItem> items,
        int codepoint,
        TextRenderingOptions options
    )
    {
        if (items.Count == 0)
            return false;

        var previous = items[^1];
        if (previous.IsNewline)
            return false;

        if (options.KinsokuMode == KinsokuMode.Standard)
        {
            // 行頭に置けない文字の前、および行末に置けない文字の後では改行しない
            if (IsProhibitedAtLineStart(codepoint) || IsProhibitedAtLineEnd(previous.Codepoint))
                return false;
        }

        return options.WrapMode switch
        {
            WrapMode.Character => true,
            WrapMode.Word => IsBreakingSpace(previous.Codepoint),
            WrapMode.Mixed => IsBreakingSpace(previous.Codepoint)
                || IsWideCharacter(codepoint)
                || IsWideCharacter(previous.Codepoint),
            _ => false,
        };
    }

    /// <summary>
    /// 行頭に置いてはならない文字かどうかを判定します。
    /// </summary>
    private static bool IsProhibitedAtLineStart(int codepoint)
    {
        return LineStartProhibited.Contains((char)codepoint);
    }

    /// <summary>
    /// 行末に置いてはならない文字かどうかを判定します。
    /// </summary>
    private static bool IsProhibitedAtLineEnd(int codepoint)
    {
        return LineEndProhibited.Contains((char)codepoint);
    }

    private static bool IsBreakingSpace(int codepoint)
    {
        return codepoint is ' ' or '\t' or 0x3000;
    }

    /// <summary>
    /// 和文として扱う文字かどうかを判定します。
    /// </summary>
    private static bool IsWideCharacter(int codepoint)
    {
        return codepoint
            is >= 0x1100 and <= 0x115F // ハングル字母
                or >= 0x2E80 and <= 0x303E // CJK 部首・記号
                or >= 0x3041 and <= 0x33FF // かな・ハングル・CJK 互換
                or >= 0x3400 and <= 0x4DBF // CJK 拡張 A
                or >= 0x4E00 and <= 0x9FFF // CJK 統合漢字
                or >= 0xA000 and <= 0xA4CF // イ文字
                or >= 0xAC00 and <= 0xD7A3 // ハングル音節
                or >= 0xF900 and <= 0xFAFF // CJK 互換漢字
                or >= 0xFE30 and <= 0xFE4F // CJK 互換形
                or >= 0xFF00 and <= 0xFF60 // 全角形
                or >= 0xFFE0 and <= 0xFFE6
                or >= 0x20000 and <= 0x3FFFD; // CJK 拡張 B 以降
    }

    /// <summary>
    /// 明示的な改行と折り返しによって、テキストを行に分割します。
    /// </summary>
    private static List<LineRange> SplitIntoLines(
        List<LayoutItem> items,
        TextRenderingOptions options
    )
    {
        var lines = new List<LineRange>();
        var wraps = options.WrapMode != WrapMode.None && options.Size.X > 0;
        var maxWidth = options.Size.X;

        var lineStart = 0;
        var lastBreak = -1;
        var width = 0f;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item.IsNewline)
            {
                lines.Add(new LineRange(lineStart, i));
                lineStart = i + 1;
                lastBreak = -1;
                width = 0;
                continue;
            }

            // この文字の直前で改行できるかどうかは、溢れ判定より先に確定させる必要がある
            if (item.CanBreakBefore)
                lastBreak = i;

            if (wraps && width > 0 && width + item.Advance > maxWidth)
            {
                // 改行可能な位置まで巻き戻す。存在しなければ現在位置で強制的に折り返す
                var breakAt = lastBreak > lineStart ? lastBreak : i;
                lines.Add(new LineRange(lineStart, breakAt));

                lineStart = breakAt;
                lastBreak = -1;
                width = SumAdvance(items, lineStart, i);
            }

            width += item.Advance;
        }

        lines.Add(new LineRange(lineStart, items.Count));
        return lines;
    }

    /// <summary>
    /// 行数の上限を適用し、省略された場合は末尾へ省略記号を挿入します。
    /// </summary>
    private static void ApplyMaxLines(
        List<LayoutItem> items,
        List<LineRange> lines,
        Font font,
        TextRenderingOptions options
    )
    {
        if (options.MaxLines <= 0 || lines.Count <= options.MaxLines)
            return;

        var lastIndex = options.MaxLines - 1;
        var lastLine = lines[lastIndex];
        lines.RemoveRange(options.MaxLines, lines.Count - options.MaxLines);

        // 表示されない行に対応する要素を取り除く
        items.RemoveRange(lastLine.End, items.Count - lastLine.End);

        if (string.IsNullOrEmpty(options.Ellipsis))
        {
            lines[lastIndex] = new LineRange(lastLine.Start, items.Count);
            return;
        }

        var attribute = items.Count > lastLine.Start
            ? items[^1].Attribute
            : new CharAttribute(font.RenderOptions, options.TextColor);

        var ellipsisAttributes = new CharAttribute[options.Ellipsis.Length];
        Array.Fill(ellipsisAttributes, attribute);
        var ellipsis = CollectItems(options.Ellipsis, font, ellipsisAttributes, options);
        var ellipsisWidth = SumAdvance(ellipsis, 0, ellipsis.Count);

        // 省略記号が収まるまで、行末から文字を取り除く
        if (options.Size.X > 0)
        {
            while (
                items.Count > lastLine.Start
                && SumAdvance(items, lastLine.Start, items.Count) + ellipsisWidth > options.Size.X
            )
            {
                items.RemoveAt(items.Count - 1);
            }
        }

        items.AddRange(ellipsis);
        lines[lastIndex] = new LineRange(lastLine.Start, items.Count);
    }

    private static float SumAdvance(List<LayoutItem> items, int start, int end)
    {
        var sum = 0f;
        for (var i = start; i < end; i++)
            sum += items[i].Advance;
        return sum;
    }

    /// <summary>
    /// 行ごとの高さと整列を解決し、各グリフの位置を確定させます。
    /// </summary>
    private static TextLayout Place(
        string plainText,
        List<LayoutItem> items,
        List<LineRange> lineRanges,
        Font font,
        TextRenderingOptions options
    )
    {
        var defaultMetrics = font.Metrics;
        var lines = new List<TextLine>(lineRanges.Count);
        var widths = new int[lineRanges.Count];
        var metrics = new FontMetrics[lineRanges.Count];

        var contentWidth = 0;
        var contentHeight = 0;

        for (var i = 0; i < lineRanges.Count; i++)
        {
            var range = lineRanges[i];
            widths[i] = (int)MathF.Ceiling(SumAdvance(items, range.Start, range.End));
            metrics[i] = MeasureLine(items, range, font, defaultMetrics);

            contentWidth = Math.Max(contentWidth, widths[i]);
            contentHeight += (int)MathF.Round(metrics[i].LineHeight * options.LineSpacing);
        }

        var areaWidth = options.Size.X > 0 ? options.Size.X : contentWidth;
        var areaHeight = options.Size.Y > 0 ? options.Size.Y : contentHeight;
        var offsetY = GetVerticalOffset(options.VerticalAlignment, contentHeight, areaHeight);

        var glyphs = new List<PlacedGlyph>(items.Count);
        var top = offsetY;

        for (var i = 0; i < lineRanges.Count; i++)
        {
            var range = lineRanges[i];
            var lineHeight = (int)MathF.Round(metrics[i].LineHeight * options.LineSpacing);
            var baseline = (int)MathF.Round(metrics[i].Ascender);
            var penX = (float)GetHorizontalOffset(
                options.HorizontalAlignment,
                widths[i],
                areaWidth
            );

            for (var j = range.Start; j < range.End; j++)
            {
                var item = items[j];
                if (item.HasGlyph)
                {
                    glyphs.Add(
                        new PlacedGlyph(
                            item.Glyph,
                            item.Attribute.Options,
                            new VectorInt((int)MathF.Round(penX), top + baseline),
                            item.Attribute.Color,
                            i,
                            item.CharIndex
                        )
                    );
                }

                penX += item.Advance;
            }

            lines.Add(
                new TextLine(
                    range.Start < items.Count ? items[range.Start].CharIndex : plainText.Length,
                    range.End - range.Start,
                    widths[i],
                    top,
                    lineHeight,
                    baseline
                )
            );

            top += lineHeight;
        }

        return new TextLayout(
            plainText,
            glyphs,
            lines,
            new VectorInt(Math.Max(areaWidth, contentWidth), Math.Max(areaHeight, contentHeight))
        );
    }

    /// <summary>
    /// 行に含まれるグリフから、その行のメトリクスを求めます。
    /// </summary>
    private static FontMetrics MeasureLine(
        List<LayoutItem> items,
        LineRange range,
        Font font,
        FontMetrics defaultMetrics
    )
    {
        var result = defaultMetrics;
        var isFirst = true;

        for (var i = range.Start; i < range.End; i++)
        {
            var options = items[i].Attribute.Options;

            // 既定のサイズと同一であれば、フォントのメトリクスを再取得する必要はない
            if (options.Size.Equals(font.Size) && !isFirst)
                continue;

            var metrics = font.Source.GetMetrics(options);
            result = isFirst
                ? metrics
                : new FontMetrics(
                    Math.Max(result.Ascender, metrics.Ascender),
                    Math.Min(result.Descender, metrics.Descender),
                    Math.Max(result.LineHeight, metrics.LineHeight)
                );
            isFirst = false;
        }

        return result;
    }

    private static int GetHorizontalOffset(HorizontalAlignment alignment, int width, int areaWidth)
    {
        return alignment switch
        {
            HorizontalAlignment.Center => (areaWidth - width) / 2,
            HorizontalAlignment.Right => areaWidth - width,
            _ => 0,
        };
    }

    private static int GetVerticalOffset(VerticalAlignment alignment, int height, int areaHeight)
    {
        return alignment switch
        {
            VerticalAlignment.Center => (areaHeight - height) / 2,
            VerticalAlignment.Bottom => areaHeight - height,
            _ => 0,
        };
    }

    /// <summary>
    /// 1 文字分の描画属性を表します。
    /// </summary>
    private readonly record struct CharAttribute(
        GlyphRenderOptions Options,
        Color Color,
        int SubstituteCodepoint = -1
    );

    /// <summary>
    /// レイアウト途中の 1 文字分の情報を表します。
    /// </summary>
    private readonly record struct LayoutItem(
        int CharIndex,
        int Codepoint,
        GlyphInfo Glyph,
        bool HasGlyph,
        float Advance,
        CharAttribute Attribute,
        bool IsNewline,
        bool CanBreakBefore
    )
    {
        public static LayoutItem NewLine(int charIndex, CharAttribute attribute)
        {
            return new LayoutItem(charIndex, '\n', default, false, 0, attribute, true, false);
        }
    }

    /// <summary>
    /// 1 行に含まれる <see cref="LayoutItem" /> の範囲を表します。
    /// </summary>
    private readonly record struct LineRange(int Start, int End);
}
