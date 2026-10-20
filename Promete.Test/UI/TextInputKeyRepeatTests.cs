using FluentAssertions;
using Promete.Input;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// TextInput で BackSpace・Delete・矢印キー・Enter を押し続けたときの繰り返しを検証します。
/// ヘッドレスの経過時間は実時間なので、ProcessInput に経過時間を直接渡して、決定的に検証します。
/// 時間は 0.25 刻み (遅延 0.5 秒、間隔 0.25 秒) にします。
/// </summary>
public class TextInputKeyRepeatTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly TextInput _input;
    private readonly List<string> _submitted = [];

    public TextInputKeyRepeatTests()
    {
        _input = new TextInput { KeyRepeatDelay = 0.5f, KeyRepeatInterval = 0.25f };
        _input.Submitted += _submitted.Add;
        _h.App.GlobalForeground.Add(_input);
        _h.UI.SetFocus(_input);

        // フォーカス直後のバッファの破棄を済ませる
        _h.Frame(3);
    }

    public void Dispose() => _h.Dispose();

    private Keyboard Kb => _h.Keyboard;

    private void Down(Key key)
    {
        key.IsPressed = true;
        key.IsKeyDown = true;
    }

    private void Up(Key key)
    {
        key.IsPressed = false;
        key.IsKeyDown = false;
    }

    /// <summary>押した瞬間のフレームを 1 回、続けて経過時間 0.25 秒のフレームを n 回進めます。</summary>
    private void Hold(Key key, int frames)
    {
        Down(key);
        _input.ProcessInput(Kb, 0f);
        key.IsKeyDown = false;
        for (var i = 0; i < frames; i++)
            _input.ProcessInput(Kb, 0.25f);
    }

    [Fact]
    public void BackSpace_ShouldDeleteOnce_OnPress_AndNotRepeatBeforeDelay()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;

        Hold(Kb.BackSpace, frames: 1);

        _input.Text.Should().Be("abcdefghi");
    }

    [Fact]
    public void BackSpace_ShouldRepeatAfterDelay()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;

        // 押した瞬間に 1 文字、0.5 秒で繰り返しが始まって 1 文字、0.75 秒と 1.0 秒でそれぞれ 1 文字
        Hold(Kb.BackSpace, frames: 4);

        _input.Text.Should().Be("abcdef");
        _input.CursorPosition.Should().Be(6);
    }

    [Fact]
    public void BackSpace_ShouldStopAtTheStartOfTheText()
    {
        _input.Text = "ab";
        _input.CursorPosition = 2;

        Hold(Kb.BackSpace, frames: 20);

        _input.Text.Should().BeEmpty();
        _input.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void ReleasingTheKey_ShouldStopRepeating()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;
        Hold(Kb.BackSpace, frames: 2);
        _input.Text.Should().Be("abcdefgh");

        Up(Kb.BackSpace);
        _input.ProcessInput(Kb, 1f);
        _input.ProcessInput(Kb, 1f);

        _input.Text.Should().Be("abcdefgh");
    }

    [Fact]
    public void Delete_ShouldRepeat()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 0;

        Hold(Kb.Delete, frames: 4);

        _input.Text.Should().Be("efghij");
        _input.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void LeftAndRight_ShouldRepeat()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 5;

        Hold(Kb.Left, frames: 4);
        _input.CursorPosition.Should().Be(1);
        Up(Kb.Left);
        _input.ProcessInput(Kb, 0f);

        Hold(Kb.Right, frames: 4);
        _input.CursorPosition.Should().Be(5);
    }

    [Fact]
    public void UpAndDown_ShouldRepeat_WhenMultiline()
    {
        _input.Rows = 5;
        _input.Text = "a\nb\nc\nd\ne";
        _input.CursorPosition = 0;

        // 押した瞬間 + 繰り返し 3 回で、1 行ずつ 4 行下へ
        Hold(Kb.Down, frames: 4);
        _input.CursorPosition.Should().Be(8);

        Up(Kb.Down);
        _input.ProcessInput(Kb, 0f);
        Hold(Kb.Up, frames: 4);
        _input.CursorPosition.Should().Be(0);
    }

    [Fact]
    public void UpAndDown_ShouldNotMoveTheCaret_WhenSingleLine()
    {
        _input.Text = "abc";
        _input.CursorPosition = 1;

        Hold(Kb.Down, frames: 4);

        _input.CursorPosition.Should().Be(1);
    }

    [Fact]
    public void Enter_ShouldRepeatNewlines_WhenMultiline()
    {
        _input.Rows = 5;

        Hold(Kb.Enter, frames: 4);

        _input.Text.Should().Be("\n\n\n\n");
        _submitted.Should().BeEmpty();
    }

    [Fact]
    public void CtrlEnter_ShouldSubmitOnlyOnce_NotOnRepeat()
    {
        _input.Rows = 5;
        _input.Text = "ab";
        Down(Kb.ControlLeft);

        Hold(Kb.Enter, frames: 6);

        _submitted.Should().Equal("ab");
        _input.Text.Should().Be("ab");
    }

    [Fact]
    public void SingleLineEnter_ShouldSubmitOnlyOnce_NotOnRepeat()
    {
        _input.Text = "ab";

        Hold(Kb.Enter, frames: 6);

        _submitted.Should().Equal("ab");
    }

    [Fact]
    public void KeyHeldBeforeFocus_ShouldNotRepeat()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;

        // 押した瞬間を見ないまま、押され続けている
        Kb.BackSpace.IsPressed = true;
        Kb.BackSpace.IsKeyDown = false;
        _input.ProcessInput(Kb, 1f);
        _input.ProcessInput(Kb, 1f);

        _input.Text.Should().Be("abcdefghij");
    }

    [Fact]
    public void RepeatSettings_ShouldBeConfigurable()
    {
        _input.KeyRepeatDelay = 0.25f;
        _input.KeyRepeatInterval = 0.125f;
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;

        Down(Kb.BackSpace);
        _input.ProcessInput(Kb, 0f);
        Kb.BackSpace.IsKeyDown = false;
        // 0.25 秒で開始 (1 文字)、続く 0.125 秒ごとに 1 文字
        _input.ProcessInput(Kb, 0.25f);
        _input.ProcessInput(Kb, 0.125f);

        _input.Text.Should().Be("abcdefg");
    }

    [Fact]
    public void RegainingFocus_ShouldForgetTheHeldKey()
    {
        _input.Text = "abcdefghij";
        _input.CursorPosition = 10;
        Down(Kb.BackSpace);
        _input.ProcessInput(Kb, 0f);
        Kb.BackSpace.IsKeyDown = false;
        _input.Text.Should().Be("abcdefghi");

        _h.UI.SetFocus(null);
        _h.UI.SetFocus(_input);
        _h.Frame(2);
        var before = _input.Text;
        _input.ProcessInput(Kb, 1f);
        _input.ProcessInput(Kb, 1f);

        _input.Text.Should().Be(before);
    }
}
