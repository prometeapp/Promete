using System.Collections.Generic;

namespace Promete.Graphics.Fonts;

/// <summary>
/// レイアウト済みのテキストを表します。
/// </summary>
public sealed class TextLayout
{
    internal TextLayout(
        string plainText,
        IReadOnlyList<PlacedGlyph> glyphs,
        IReadOnlyList<TextLine> lines,
        VectorInt size
    )
    {
        PlainText = plainText;
        Glyphs = glyphs;
        Lines = lines;
        Size = size;
    }

    /// <summary>
    /// 空のレイアウト結果を取得します。
    /// </summary>
    public static TextLayout Empty { get; } = new(string.Empty, [], [], VectorInt.Zero);

    /// <summary>
    /// 装飾を取り除いたテキストを取得します。
    /// </summary>
    public string PlainText { get; }

    /// <summary>
    /// 配置されたグリフを取得します。
    /// </summary>
    public IReadOnlyList<PlacedGlyph> Glyphs { get; }

    /// <summary>
    /// 行の情報を取得します。
    /// </summary>
    public IReadOnlyList<TextLine> Lines { get; }

    /// <summary>
    /// レイアウトされたテキスト全体を含む矩形のサイズを取得します。
    /// </summary>
    public VectorInt Size { get; }

    /// <summary>
    /// 指定した位置にある文字のインデックスを取得します。
    /// </summary>
    /// <param name="position">テキストの左上を原点とする座標。</param>
    /// <returns>
    /// 該当する文字のインデックス。文字の上にない場合は、最も近い文字のインデックス。
    /// テキストが空の場合は 0。
    /// </returns>
    public int HitTest(Vector position)
    {
        if (Lines.Count == 0)
            return 0;

        var line = FindLine(position.Y);
        var closestIndex = line.StartIndex;
        var closestDistance = float.MaxValue;

        foreach (var glyph in Glyphs)
        {
            if (glyph.CharIndex < line.StartIndex)
                continue;
            if (glyph.CharIndex >= line.StartIndex + line.Length)
                continue;

            // 文字の中央より右をクリックした場合は、次の文字の位置とみなす
            var center = glyph.Position.X + (glyph.Glyph.Advance / 2);
            var distance = System.Math.Abs(position.X - center);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestIndex = position.X < center ? glyph.CharIndex : glyph.CharIndex + 1;
        }

        return closestIndex;
    }

    private TextLine FindLine(float y)
    {
        foreach (var line in Lines)
        {
            if (y < line.Top + line.Height)
                return line;
        }

        return Lines[^1];
    }
}
