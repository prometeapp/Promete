using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.UI;

namespace Promete.Test.UI;

/// <summary>
/// 既定のフォント (等幅) の寸法は実行環境によって違うので、<see cref="TestFont"/> で測った値を使います。
/// 以下では、1文字の幅を W として表します。
/// </summary>
public class CaretNavigatorTests
{
    private static readonly int W = TestFont.CharWidth;

    private static TextLayout Layout(string text, int wrapWidth = 0)
    {
        var options = new TextRenderingOptions
        {
            Size = (wrapWidth, 0),
            WrapMode = wrapWidth > 0 ? WrapMode.Character : WrapMode.None,
        };
        return TextLayoutEngine.Layout(text, Font.GetDefault(), options);
    }

    /// <summary>5 文字で折り返す幅。</summary>
    private static int FiveChars => 5 * W;

    [Fact]
    public void GetX_ShouldReturnCharacterLeftEdge()
    {
        var layout = Layout("abc");

        CaretNavigator.GetX(layout, 0).Should().Be(0);
        CaretNavigator.GetX(layout, 1).Should().Be(W);
        CaretNavigator.GetX(layout, 3).Should().Be(3 * W);
    }

    [Fact]
    public void GetX_ShouldBeZero_ForEmptyText()
    {
        CaretNavigator.GetX(Layout(""), 0).Should().Be(0);
    }

    [Fact]
    public void FindLine_ShouldFollowHardNewlines()
    {
        var layout = Layout("ab\ncd");

        CaretNavigator.FindLine(layout, 0).Should().Be(0);
        CaretNavigator.FindLine(layout, 2).Should().Be(0);
        CaretNavigator.FindLine(layout, 3).Should().Be(1);
        CaretNavigator.FindLine(layout, 5).Should().Be(1);
    }

    [Fact]
    public void FindLine_ShouldPlaceCaretAtEndOfTextAfterTrailingNewline_OnTheLastEmptyLine()
    {
        var layout = Layout("ab\n");

        CaretNavigator.FindLine(layout, 3).Should().Be(1);
        CaretNavigator.GetX(layout, 3).Should().Be(0);
    }

    [Fact]
    public void FindLine_ShouldPlaceCaretOnBlankLine()
    {
        var layout = Layout("ab\n\ncd");

        CaretNavigator.FindLine(layout, 3).Should().Be(1);
        CaretNavigator.GetX(layout, 3).Should().Be(0);
        CaretNavigator.FindLine(layout, 4).Should().Be(2);
    }

    [Fact]
    public void GetX_ShouldReturnLineWidth_AtEndOfLine()
    {
        var layout = Layout("ab\ncd");

        CaretNavigator.GetX(layout, 2).Should().Be(2 * W);
        CaretNavigator.GetX(layout, 5).Should().Be(2 * W);
    }

    [Fact]
    public void SoftWrap_ShouldPlaceCaretAtWrapBoundaryOnTheNextLine()
    {
        // 5 文字ごとに折り返す
        var layout = Layout("abcdefghij", FiveChars);

        layout.Lines.Count.Should().Be(2);
        CaretNavigator.FindLine(layout, 5).Should().Be(1);
        CaretNavigator.GetX(layout, 5).Should().Be(0);
        CaretNavigator.GetX(layout, 7).Should().Be(2 * W);
    }

    [Fact]
    public void LineStart_AndLineEnd_ShouldFollowLines()
    {
        var layout = Layout("ab\ncd");

        CaretNavigator.LineStart(layout, 1).Should().Be(3);
        CaretNavigator.LineEnd(layout, 0).Should().Be(2);
        CaretNavigator.LineEnd(layout, 1).Should().Be(5);
    }

    [Fact]
    public void LineEnd_ShouldStayBeforeTheLastCharacter_OnSoftWrappedLine()
    {
        var layout = Layout("abcdefghij", FiveChars);

        // 1行目の末尾は 5 だが、そこは2行目の先頭と同じ位置なので、1つ手前にする
        CaretNavigator.LineEnd(layout, 0).Should().Be(4);
        CaretNavigator.LineEnd(layout, 1).Should().Be(10);
    }

    [Fact]
    public void MoveVertically_ShouldKeepColumn()
    {
        var layout = Layout("abcd\nefgh");

        // 1行目の 2 文字目 (x = 2W) から下へ
        CaretNavigator.MoveVertically(layout, 2, 2 * W, 1).Should().Be(7);
        // 2行目の 3 文字目から上へ
        CaretNavigator.MoveVertically(layout, 8, 3 * W, -1).Should().Be(3);
    }

    [Fact]
    public void MoveVertically_ShouldClampToShorterLine()
    {
        var layout = Layout("abcdef\nxy");

        CaretNavigator.MoveVertically(layout, 6, 6 * W, 1).Should().Be(9);
    }

    [Fact]
    public void MoveVertically_ShouldLandOnBlankLine()
    {
        var layout = Layout("abc\n\ndef");

        CaretNavigator.MoveVertically(layout, 2, 2 * W, 1).Should().Be(4);
        CaretNavigator.MoveVertically(layout, 4, 0, 1).Should().Be(5);
    }

    [Fact]
    public void MoveVertically_ShouldNotMove_AtFirstOrLastLine()
    {
        var layout = Layout("ab\ncd");

        CaretNavigator.MoveVertically(layout, 1, W, -1).Should().Be(1);
        CaretNavigator.MoveVertically(layout, 4, W, 1).Should().Be(4);
    }

    [Fact]
    public void MoveVertically_ShouldWorkAcrossSoftWrappedLines()
    {
        var layout = Layout("abcdefghij", FiveChars);

        CaretNavigator.MoveVertically(layout, 2, 2 * W, 1).Should().Be(7);
        CaretNavigator.MoveVertically(layout, 7, 2 * W, -1).Should().Be(2);
    }

    [Fact]
    public void MoveVertically_ShouldDoNothing_ForEmptyText()
    {
        CaretNavigator.MoveVertically(Layout(""), 0, 0, 1).Should().Be(0);
    }
}
