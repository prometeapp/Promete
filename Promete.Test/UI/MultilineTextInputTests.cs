using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// 複数行の TextInput (Rows &gt; 1) の挙動と、1行の TextInput が変わっていないことを検証します。
/// フォントは等幅で、1文字の幅を 8px、1行の高さを 16px として扱います。
/// </summary>
public class MultilineTextInputTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly TextInput _input;
    private readonly Button _below;
    private readonly List<string> _submitted = [];
    private int _changed;

    public MultilineTextInputTests()
    {
        _input = new TextInput("入力") { Rows = 3 }.Location(100, 100);
        _input.Submitted += _submitted.Add;
        _input.TextChanged += _ => _changed++;
        _below = new Button("below").Location(100, 300).Size(80, 30);
        _h.App.GlobalForeground.Add(_input);
        _h.App.GlobalForeground.Add(_below);
        _h.UI.SetFocus(_input);
        _h.Frame(3);
    }

    public void Dispose() => _h.Dispose();

    private void Type(string text) => _h.TypeText(text);

    [Fact]
    public void Rows_ShouldDefaultToOne_AndBeClamped()
    {
        var input = new TextInput();

        input.Rows.Should().Be(1);
        input.IsMultiline.Should().BeFalse();

        input.Rows = 0;
        input.Rows.Should().Be(1);
    }

    [Fact]
    public void Rows_ShouldDetermineHeight_WhenMultiline()
    {
        // 3 行 × 16px + 上下の余白 6px × 2
        _input.IsMultiline.Should().BeTrue();
        _input.Size.Y.Should().Be(3 * 16 + 12);
    }

    [Fact]
    public void Rows_One_ShouldNotChangeSize()
    {
        var input = new TextInput();
        input.Size = (200, 50);

        input.Rows = 1;

        input.Size.Should().Be(new VectorInt(200, 50));
    }

    [Fact]
    public void SingleLine_EnterShouldSubmit_AndNotInsertNewline()
    {
        var input = new TextInput { Text = "ab" }.Location(400, 100);
        var submitted = new List<string>();
        input.Submitted += submitted.Add;
        _h.App.GlobalForeground.Add(input);
        _h.UI.SetFocus(input);
        _h.Frame(3);

        _h.PressKey(KeyCode.Enter);

        submitted.Should().Equal("ab");
        input.Text.Should().Be("ab");
    }

    [Fact]
    public void Enter_ShouldInsertNewline_WhenMultiline()
    {
        Type("ab");
        _h.PressKey(KeyCode.Enter);
        Type("cd");

        _input.Text.Should().Be("ab\ncd");
        _input.CursorPosition.Should().Be(5);
        _submitted.Should().BeEmpty();
    }

    [Fact]
    public void CtrlEnter_ShouldSubmit_WithoutInsertingNewline()
    {
        Type("ab");

        _h.KeyDown(KeyCode.ControlLeft);
        _h.PressKey(KeyCode.Enter);
        _h.KeyUp(KeyCode.ControlLeft);

        _submitted.Should().Equal("ab");
        _input.Text.Should().Be("ab");
    }

    [Fact]
    public void TypedNewlineCharacters_ShouldBeIgnored()
    {
        Type("a\r\nb");

        _input.Text.Should().Be("ab");
    }

    [Fact]
    public void TextChanged_ShouldFireOnEdits()
    {
        Type("a");
        _h.PressKey(KeyCode.Enter);
        _h.PressKey(KeyCode.BackSpace);

        _changed.Should().Be(3);
    }

    [Fact]
    public void UpAndDown_ShouldMoveCaretByLine_KeepingColumn()
    {
        _input.Text = "abcd\nefgh";
        _input.CursorPosition = 2;

        _h.PressKey(KeyCode.Down);
        _input.CursorPosition.Should().Be(7);

        // 最後の行では、これ以上下へは動かない
        _h.PressKey(KeyCode.Down);
        _input.CursorPosition.Should().Be(7);

        _h.PressKey(KeyCode.Up);
        _input.CursorPosition.Should().Be(2);
    }

    [Fact]
    public void UpAndDown_ShouldRememberColumnAcrossShortLines()
    {
        _input.Text = "abcdef\nxy\nabcdef";
        _input.CursorPosition = 5;

        _h.PressKey(KeyCode.Down);
        _input.CursorPosition.Should().Be(9);

        _h.PressKey(KeyCode.Down);
        _input.CursorPosition.Should().Be(15);
    }

    [Fact]
    public void UpAndDown_ShouldNotMoveUIFocus_WhenMultiline()
    {
        _input.Text = "ab";

        _h.PressKey(KeyCode.Down);

        _h.UI.FocusedElement.Should().BeSameAs(_input);
    }

    [Fact]
    public void Down_ShouldStillMoveUIFocus_WhenSingleLine()
    {
        var single = new TextInput().Location(400, 100);
        _h.App.GlobalForeground.Add(single);
        _h.Frame(2);
        _h.UI.SetFocus(single);
        _h.Frame(2);

        _h.PressKey(KeyCode.Down);

        _h.UI.FocusedElement.Should().NotBeSameAs(single);
    }

    [Fact]
    public void HomeAndEnd_ShouldMoveWithinTheCaretLine()
    {
        _input.Text = "ab\ncd";
        _input.CursorPosition = 4;

        _h.PressKey(KeyCode.Home);
        _input.CursorPosition.Should().Be(3);

        _h.PressKey(KeyCode.End);
        _input.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void LeftAndRight_ShouldMoveAcrossNewline()
    {
        _input.Text = "ab\ncd";
        _input.CursorPosition = 3;

        _h.PressKey(KeyCode.Left);
        _input.CursorPosition.Should().Be(2);

        _h.PressKey(KeyCode.Right);
        _h.PressKey(KeyCode.Right);
        _input.CursorPosition.Should().Be(4);
    }

    [Fact]
    public void BackSpaceAndDelete_ShouldJoinLines()
    {
        _input.Text = "ab\ncd";
        _input.CursorPosition = 3;
        _h.PressKey(KeyCode.BackSpace);
        _input.Text.Should().Be("abcd");

        _input.Text = "ab\ncd";
        _input.CursorPosition = 2;
        _h.PressKey(KeyCode.Delete);
        _input.Text.Should().Be("abcd");
    }

    [Fact]
    public void LongLine_ShouldWrapToWidth_AndCaretFollowsToSecondLine()
    {
        // 幅 200 - 左右の余白 6 × 2 = 188px。8px の文字が 23 文字で折り返す
        _input.Text = new string('a', 30);
        _input.CursorPosition = 30;
        _h.Frame(2);

        var caret = _input.CaretLocation;
        caret.X.Should().Be(6 + 7 * 8);
        caret.Y.Should().Be(6 + 16);
        _input.CaretHeight.Should().Be(16);
    }

    [Fact]
    public void SingleLine_CaretShouldUseGlyphPositions_AndKeepItsVerticalMargins()
    {
        var input = new TextInput { Text = "abc" };
        input.CursorPosition = 2;

        input.CaretLocation.Should().Be(new Vector(6 + 16, 4));
        input.CaretHeight.Should().Be(input.Size.Y - 8);
    }

    [Fact]
    public void Scrolling_ShouldFollowCaret_WhenLinesExceedRows()
    {
        // 2 行分の表示範囲 (32px) に、5 行 (80px) のテキスト
        _input.Rows = 2;
        _input.Text = "l0\nl1\nl2\nl3\nl4";
        _input.CursorPosition = _input.Text.Length;
        _h.Frame(2);

        _input.ScrollOffset.Should().Be(80 - 32);
        _input.TextNode.Location.Y.Should().Be(-(80 - 32));

        // 先頭の行までキャレットを戻すと、スクロールも戻る
        for (var i = 0; i < 4; i++)
            _h.PressKey(KeyCode.Up);

        _input.ScrollOffset.Should().Be(0);
        _input.TextNode.Location.Y.Should().Be(0);
    }

    [Fact]
    public void Scrolling_ShouldOnlyMoveWhenCaretLeavesTheView()
    {
        _input.Rows = 2;
        _input.Text = "l0\nl1\nl2\nl3\nl4";
        _input.CursorPosition = _input.Text.Length;
        _h.Frame(2);

        // 見えている範囲 (3・4 行目) の中で動かしてもスクロールしない
        _h.PressKey(KeyCode.Up);
        _input.ScrollOffset.Should().Be(48);

        // 範囲の外 (2 行目) に出ると、その行が見えるまでスクロールする
        _h.PressKey(KeyCode.Up);
        _input.ScrollOffset.Should().Be(32);
    }

    [Fact]
    public void Scrolling_ShouldKeepCaretInsideTheBox()
    {
        _input.Rows = 2;
        _input.Text = "l0\nl1\nl2\nl3\nl4";
        _input.CursorPosition = _input.Text.Length;
        _h.Frame(2);

        var y = _input.CaretLocation.Y;

        y.Should().BeGreaterThanOrEqualTo(6);
        (y + _input.CaretHeight).Should().BeLessThanOrEqualTo(_input.Size.Y - 6);
    }

    [Fact]
    public void ShortText_ShouldNotScroll()
    {
        _input.Text = "a\nb";
        _input.CursorPosition = 3;
        _h.Frame(2);

        _input.ScrollOffset.Should().Be(0);
    }

    [Fact]
    public void Scroll_ShouldComeBack_WhenTextBecomesShorter()
    {
        _input.Rows = 2;
        _input.Text = "l0\nl1\nl2\nl3\nl4";
        _input.CursorPosition = _input.Text.Length;
        _h.Frame(2);

        _input.Text = "l0";
        _h.Frame(2);

        _input.ScrollOffset.Should().Be(0);
    }

    [Fact]
    public void Multiline_ShouldClipTextToTheBox_WithATrimmableContainer()
    {
        var viewport = (Container)_input.TextNode.Parent!;

        viewport.IsTrimmable.Should().BeTrue();
        viewport.Location.Should().Be(new Vector(6, 6));
        viewport.Size.Should().Be(new VectorInt(200 - 12, 3 * 16));
        _input.TextNode.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void SingleLine_ShouldNotClipText()
    {
        var input = new TextInput();
        _h.App.GlobalForeground.Add(input);
        _h.Frame(2);

        var viewport = (Container)input.TextNode.Parent!;
        viewport.IsTrimmable.Should().BeFalse();
    }

    [Fact]
    public void BackToSingleLine_ShouldStopClippingAndWrapping()
    {
        Type(new string('a', 30));
        _input.TextNode.Layout.Lines.Count.Should().Be(2);

        _input.Rows = 1;
        _h.Frame(2);

        ((Container)_input.TextNode.Parent!).IsTrimmable.Should().BeFalse();
        _input.TextNode.Layout.Lines.Count.Should().Be(1);
        _input.TextNode.WrapMode.Should().Be(WrapMode.None);
    }

    [Fact]
    public void Multiline_ShouldWrapTheTextNodeToTheInnerWidth()
    {
        Type(new string('a', 30));

        _input.TextNode.WrapMode.Should().Be(WrapMode.Mixed);
        _input.TextNode.PreferredSize.X.Should().Be(200 - 12);
        _input.TextNode.Layout.Lines.Count.Should().Be(2);
    }

    [Fact]
    public void WrapMode_ShouldChangeWhereLinesBreak()
    {
        // 内側の幅は 40px (5 文字分)
        _input.Size = (52, _input.Size.Y);
        _input.Text = "aaa bbbbb";
        _h.Frame(2);

        string FirstLine() =>
            _input.Text.Substring(
                _input.TextNode.Layout.Lines[0].StartIndex,
                _input.TextNode.Layout.Lines[0].Length
            );

        _input.WrapMode = WrapMode.Character;
        FirstLine().Should().Contain("b");

        _input.WrapMode = WrapMode.Word;
        FirstLine().Should().NotContain("b");
    }

    [Fact]
    public void WrapModeNone_ShouldNotWrap_AndKinsokuModeShouldBeForwarded()
    {
        _input.WrapMode = WrapMode.None;
        _input.KinsokuMode = KinsokuMode.None;
        Type(new string('a', 30));

        _input.TextNode.Layout.Lines.Count.Should().Be(1);
        _input.TextNode.KinsokuMode.Should().Be(KinsokuMode.None);
    }

    [Fact]
    public void DefaultStyle_ShouldDrawCaretAtCaretLocation()
    {
        Type("ab");
        _h.PressKey(KeyCode.Enter);
        Type("c");
        _h.Frame(2);

        var cursor = Enumerable
            .Range(0, _input.Count)
            .Select(i => _input[i])
            .OfType<Shape>()
            .Single(s => s.Name == "_cursor");

        var caret = _input.CaretLocation;
        cursor.Vertices[0].Should().Be(new VectorInt((int)caret.X, (int)caret.Y));
        caret.Y.Should().Be(6 + 16);
    }
}
