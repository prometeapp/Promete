using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// フォーカスを持つ要素が、ツリーから外れる・隠れる・無効になる・レイヤーに覆われるなどしたときの、
/// フォーカスの扱いを検証します。
/// </summary>
public class FocusLifecycleTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _background;
    private int _backgroundClicks;

    public FocusLifecycleTests()
    {
        _background = new Button("background").Location(10, 10).Size(100, 30);
        _background.Clicked += () => _backgroundClicks++;
        _h.App.GlobalForeground.Add(_background);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    private Modal ShowModal(out Button inside)
    {
        var modal = new Modal((300, 200));
        modal.Size = (800, 600);
        inside = new Button("inside").Location(10, 10).Size(100, 30);
        modal.Body.Add(inside);
        modal.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);
        return modal;
    }

    [Fact]
    public void FocusSetBeforeTheFirstFrame_ShouldBeKept()
    {
        // 要素を追加した直後 (まだ一度も描画されていない) にフォーカスしても、外れない
        var button = new Button("new").Location(300, 10).Size(100, 30);
        _h.App.GlobalForeground.Add(button);
        _h.UI.SetFocus(button);

        _h.Frame();

        _h.UI.FocusedElement.Should().BeSameAs(button);
    }

    [Fact]
    public void FocusedElementRemovedFromTree_ShouldLoseFocus()
    {
        _h.UI.SetFocus(_background);

        _h.App.GlobalForeground.Remove(_background);
        _h.Frame(2);

        _h.UI.FocusedElement.Should().BeNull();
    }

    [Fact]
    public void FocusedElementInHiddenContainer_ShouldLoseFocus()
    {
        var container = new Container();
        var button = new Button("in").Location(300, 10).Size(100, 30);
        container.Add(button);
        _h.App.GlobalForeground.Add(container);
        _h.Frame(2);
        _h.UI.SetFocus(button);

        container.IsVisible = false;
        _h.Frame(2);

        _h.UI.FocusedElement.Should().BeNull();
    }

    [Fact]
    public void DisabledFocusedTextInput_ShouldStopAcceptingText()
    {
        var input = new TextInput().Location(300, 100);
        _h.App.GlobalForeground.Add(input);
        _h.Frame(2);
        _h.UI.SetFocus(input);
        _h.Frame(2);

        input.IsEnabled = false;
        _h.Frame(2);
        _h.TypeText("abc");

        input.Text.Should().BeEmpty();
        _h.UI.FocusedElement.Should().BeNull();
    }

    [Fact]
    public void OpeningAModal_ShouldClearFocus_SoEnterDoesNotClickTheButtonBehindIt()
    {
        _h.UI.SetFocus(_background);
        ShowModal(out _);

        _h.PressKey(KeyCode.Enter);
        _h.PressKey(KeyCode.Space);

        _backgroundClicks.Should().Be(0);
        _h.UI.FocusedElement.Should().BeNull();
    }

    [Fact]
    public void OpeningAModal_ShouldNotLetTypedTextReachATextInputBehindIt()
    {
        var input = new TextInput().Location(300, 100);
        _h.App.GlobalForeground.Add(input);
        _h.Frame(2);
        _h.UI.SetFocus(input);
        _h.Frame(2);
        ShowModal(out _);

        _h.TypeText("abc");

        input.Text.Should().BeEmpty();
    }

    [Fact]
    public void ClosingAModal_ShouldReturnFocusToTheElementThatHadItBefore()
    {
        _h.UI.SetFocus(_background);
        var modal = ShowModal(out var inside);
        _h.UI.SetFocus(inside);

        modal.Close();

        _h.UI.FocusedElement.Should().BeSameAs(_background);
    }

    [Fact]
    public void ClosingAModal_ShouldNotLeaveFocusOnItsOwnElements_SoEnterDoesNotClickThem()
    {
        var modal = ShowModal(out var inside);
        var insideClicks = 0;
        inside.Clicked += () => insideClicks++;
        _h.UI.SetFocus(inside);

        modal.Close();
        _h.Frame(2);
        _h.PressKey(KeyCode.Enter);

        _h.UI.FocusedElement.Should().BeNull();
        insideClicks.Should().Be(0);
    }

    [Fact]
    public void ShowingAModalTwice_ShouldPushOnlyOneLayer()
    {
        var modal = ShowModal(out _);

        modal.Show(_h.UI, _h.App.GlobalForeground);
        modal.Close();
        _h.Frame(2);

        _h.UI.CurrentModal.Should().BeNull();
        _h.Click(50, 20);
        _backgroundClicks.Should().Be(1);
    }
}
