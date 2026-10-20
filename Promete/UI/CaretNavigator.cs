using Promete.Graphics.Fonts;

namespace Promete.UI;

/// <summary>
/// <see cref="TextLayout"/> を基に、キャレット (文字の間の位置) の座標や、行をまたぐ移動先を求める計算です。
/// </summary>
/// <remarks>
/// キャレットの位置は、文字のインデックス (0〜文字数) で表します。
/// 折り返しで行が分かれた位置では、キャレットは次の行の先頭にあるものとして扱います。
/// </remarks>
internal static class CaretNavigator
{
    /// <summary>
    /// キャレットが属する行のインデックスを求めます。
    /// </summary>
    /// <returns>行のインデックス。テキストが空で行がない場合は 0。</returns>
    public static int FindLine(TextLayout layout, int index)
    {
        var lines = layout.Lines;
        var found = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartIndex > index)
                break;
            found = i;
        }

        return found;
    }

    /// <summary>
    /// キャレットの X 座標 (テキストの左端が 0) を求めます。
    /// </summary>
    public static int GetX(TextLayout layout, int index)
    {
        if (layout.Lines.Count == 0)
            return 0;

        var lineIndex = FindLine(layout, index);
        var line = layout.Lines[lineIndex];
        if (index >= line.StartIndex + line.Length)
            return line.Length == 0 ? 0 : line.Width;

        // その位置の文字の左端。グリフがない文字 (空白など) の場合は、直前のグリフの右端
        var x = 0;
        foreach (var glyph in layout.Glyphs)
        {
            if (glyph.LineIndex != lineIndex || glyph.CharIndex > index)
                continue;

            x =
                glyph.CharIndex == index
                    ? glyph.Position.X
                    : glyph.Position.X + (int)System.MathF.Round(glyph.Glyph.Advance);
            if (glyph.CharIndex == index)
                break;
        }

        return x;
    }

    /// <summary>
    /// 行の先頭のキャレット位置を求めます。
    /// </summary>
    public static int LineStart(TextLayout layout, int lineIndex) =>
        layout.Lines.Count == 0 ? 0 : layout.Lines[lineIndex].StartIndex;

    /// <summary>
    /// 行の末尾のキャレット位置を求めます。
    /// 折り返しで次の行に続く行では、次の行の先頭に移ってしまわないよう、最後の文字の手前になります。
    /// </summary>
    public static int LineEnd(TextLayout layout, int lineIndex)
    {
        if (layout.Lines.Count == 0)
            return 0;

        var line = layout.Lines[lineIndex];
        var end = line.StartIndex + line.Length;
        var continuesOnNextLine =
            lineIndex + 1 < layout.Lines.Count
            && layout.Lines[lineIndex + 1].StartIndex == end
            && line.Length > 0;
        return continuesOnNextLine ? end - 1 : end;
    }

    /// <summary>
    /// 1行上または下に移動した先のキャレット位置を求めます。
    /// </summary>
    /// <param name="layout">レイアウト。</param>
    /// <param name="index">現在のキャレット位置。</param>
    /// <param name="desiredX">移動先で目指す X 座標。</param>
    /// <param name="direction">-1 で上、1 で下。</param>
    /// <returns>移動先のキャレット位置。移動できない場合は <paramref name="index"/> のまま。</returns>
    public static int MoveVertically(TextLayout layout, int index, int desiredX, int direction)
    {
        if (layout.Lines.Count == 0)
            return index;

        var target = FindLine(layout, index) + direction;
        if (target < 0 || target >= layout.Lines.Count)
            return index;

        var line = layout.Lines[target];
        return layout.HitTest(new Vector(desiredX, line.Top + line.Height / 2f));
    }
}
