using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.UI;

namespace Promete.Test.UI;

/// <summary>
/// 既定のフォントは実行環境によって違い、比例フォントの場合もあります。
/// そのため、位置を比べるテストでは同じ文字 (a) だけを並べ、文字ごとの幅の違いが出ないようにします。
/// 座標の期待値は、レイアウトが持つグリフの位置や行の幅と突き合わせます。
/// </summary>
public class CaretNavigatorTests
{
    private static TextLayout Layout(string text, int wrapWidth = 0)
    {
        var options = new TextRenderingOptions
        {
            Size = (wrapWidth, 0),
            WrapMode = wrapWidth > 0 ? WrapMode.Character : WrapMode.None,
        };
        return TextLayoutEngine.Layout(text, Font.GetDefault(), options);
    }

    /// <summary>
    /// 同じ文字 5 つが 1 行に収まり、6 つ目は収まらない幅。
    /// 幅の丸めで 5 つ目が収まらなくならないよう、1px の余裕を持たせます。
    /// </summary>
    private static int FiveChars => Layout("aaaaa").Size.X + 1;

    [Fact]
    public void GetX_ShouldReturnCharacterLeftEdge()
    {
        var layout = Layout("aaa");

        CaretNavigator.GetX(layout, 0).Should().Be(0);
        CaretNavigator.GetX(layout, 1).Should().Be(layout.Glyphs[1].Position.X);
        CaretNavigator.GetX(layout, 2).Should().Be(layout.Glyphs[2].Position.X);
        CaretNavigator.GetX(layout, 1).Should().BeGreaterThan(0);
        CaretNavigator.GetX(layout, 2).Should().BeGreaterThan(CaretNavigator.GetX(layout, 1));
    }

    [Fact]
    public void GetX_ShouldBeLineWidth_AtEndOfText()
    {
        var layout = Layout("aaa");

        CaretNavigator.GetX(layout, 3).Should().Be(layout.Lines[0].Width);
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
    public void GetX_ShouldReturnLineWidth_AtEndOfEachLine()
    {
        var layout = Layout("ab\ncd");

        CaretNavigator.GetX(layout, 2).Should().Be(layout.Lines[0].Width);
        CaretNavigator.GetX(layout, 5).Should().Be(layout.Lines[1].Width);
    }

    [Fact]
    public void SoftWrap_ShouldPlaceCaretAtWrapBoundaryOnTheNextLine()
    {
        // 5 文字ごとに折り返す
        var layout = Layout("aaaaaaaaaa", FiveChars);

        layout.Lines.Count.Should().Be(2);
        CaretNavigator.FindLine(layout, 5).Should().Be(1);
        CaretNavigator.GetX(layout, 5).Should().Be(0);
        CaretNavigator.GetX(layout, 7).Should().Be(layout.Glyphs[7].Position.X);
        CaretNavigator.GetX(layout, 7).Should().BeGreaterThan(0);
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
        var layout = Layout("aaaaaaaaaa", FiveChars);

        // 1行目の末尾は 5 だが、そこは2行目の先頭と同じ位置なので、1つ手前にする
        CaretNavigator.LineEnd(layout, 0).Should().Be(4);
        CaretNavigator.LineEnd(layout, 1).Should().Be(10);
    }

    [Fact]
    public void MoveVertically_ShouldKeepColumn()
    {
        var layout = Layout("aaaa\naaaa");

        // 1行目の 3 文字目の位置から下へ。2行目の同じ桁 (インデックス 5 + 2) になる
        CaretNavigator.MoveVertically(layout, 2, CaretNavigator.GetX(layout, 2), 1).Should().Be(7);
        // 2行目の 4 文字目の位置から上へ
        CaretNavigator
            .MoveVertically(layout, 8, CaretNavigator.GetX(layout, 8), -1)
            .Should()
            .Be(3);
    }

    [Fact]
    public void MoveVertically_ShouldClampToShorterLine()
    {
        var layout = Layout("aaaaaa\naa");

        CaretNavigator.MoveVertically(layout, 6, CaretNavigator.GetX(layout, 6), 1).Should().Be(9);
    }

    [Fact]
    public void MoveVertically_ShouldLandOnBlankLine()
    {
        var layout = Layout("aaa\n\naaa");

        CaretNavigator.MoveVertically(layout, 2, CaretNavigator.GetX(layout, 2), 1).Should().Be(4);
        CaretNavigator.MoveVertically(layout, 4, 0, 1).Should().Be(5);
    }

    [Fact]
    public void MoveVertically_ShouldNotMove_AtFirstOrLastLine()
    {
        var layout = Layout("aa\naa");

        CaretNavigator.MoveVertically(layout, 1, CaretNavigator.GetX(layout, 1), -1).Should().Be(1);
        CaretNavigator.MoveVertically(layout, 4, CaretNavigator.GetX(layout, 4), 1).Should().Be(4);
    }

    [Fact]
    public void MoveVertically_ShouldWorkAcrossSoftWrappedLines()
    {
        var layout = Layout("aaaaaaaaaa", FiveChars);

        CaretNavigator.MoveVertically(layout, 2, CaretNavigator.GetX(layout, 2), 1).Should().Be(7);
        CaretNavigator.MoveVertically(layout, 7, CaretNavigator.GetX(layout, 7), -1).Should().Be(2);
    }

    [Fact]
    public void MoveVertically_ShouldStopAtTheEndOfASoftWrappedLine_NotEnterTheNextLine()
    {
        var layout = Layout("aaaaaaaaaa", FiveChars);

        // 末尾 (2行目の右端) から上へ。1行目の右端を指しても、2行目の先頭 (5) に入らない
        var up = CaretNavigator.MoveVertically(layout, 10, CaretNavigator.GetX(layout, 10), -1);
        up.Should().Be(4);
        CaretNavigator.FindLine(layout, up).Should().Be(0);
    }

    [Fact]
    public void MoveVertically_ShouldBeAbleToLeaveASoftWrappedLineRepeatedly()
    {
        var layout = Layout("aaaaaaaaaaaaaaa", FiveChars);
        var x = CaretNavigator.GetX(layout, 15);

        // 3 行を、下から順に上がっていける
        var first = CaretNavigator.MoveVertically(layout, 15, x, -1);
        var second = CaretNavigator.MoveVertically(layout, first, x, -1);

        CaretNavigator.FindLine(layout, first).Should().Be(1);
        CaretNavigator.FindLine(layout, second).Should().Be(0);
    }

    [Fact]
    public void MoveVertically_ShouldDoNothing_ForEmptyText()
    {
        CaretNavigator.MoveVertically(Layout(""), 0, 0, 1).Should().Be(0);
    }
}
