using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class PopupTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _anchor;
    private readonly Button _other;
    private int _otherClicks;

    public PopupTests()
    {
        _anchor = new Button("anchor").Location(10, 10).Size(100, 30);
        _other = new Button("other").Location(400, 300).Size(80, 30);
        _other.Clicked += () => _otherClicks++;
        _h.App.GlobalForeground.Add(_anchor);
        _h.App.GlobalForeground.Add(_other);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    private Vector View => _h.App.View.Size;

    /// <summary>100x30 のボタンを1つ持つポップアップを作ります。</summary>
    private Popup CreatePopup(out Button item)
    {
        var popup = new Popup();
        item = new Button("item").Size(100, 30);
        popup.Add(item);
        return popup;
    }

    private void Show(Popup popup, UIElement? anchor = null)
    {
        popup.Show(_h.UI, anchor ?? _anchor);
        _h.Frame(2);
    }

    [Fact]
    public void Show_ShouldAttachToHost_RaiseOpened_AndPlaceBelowAnchor()
    {
        var popup = CreatePopup(out _);
        var opened = 0;
        popup.Opened += () => opened++;

        Show(popup);

        popup.IsOpen.Should().BeTrue();
        popup.Parent.Should().BeSameAs(_h.App.GlobalForeground);
        opened.Should().Be(1);
        popup.Anchor.Should().BeSameAs(_anchor);
        popup.Location.Should().Be(new Vector(10, 40));
    }

    [Fact]
    public void Show_WhenAlreadyOpen_ShouldDoNothing()
    {
        var popup = CreatePopup(out _);
        var opened = 0;
        popup.Opened += () => opened++;

        Show(popup);
        Show(popup);

        opened.Should().Be(1);
    }

    [Fact]
    public void Close_ShouldDetachRaiseClosedOnce_AndBeIdempotent()
    {
        var popup = CreatePopup(out _);
        var closed = 0;
        popup.Closed += () => closed++;
        Show(popup);

        popup.Close();
        popup.Close();

        popup.IsOpen.Should().BeFalse();
        popup.Parent.Should().BeNull();
        closed.Should().Be(1);
    }

    [Fact]
    public void OutsideClick_ShouldClose_AndNotReachElementBelow()
    {
        var popup = CreatePopup(out _);
        Show(popup);

        _h.Click(440, 315);

        popup.IsOpen.Should().BeFalse();
        _otherClicks.Should().Be(0);
    }

    [Fact]
    public void ClickInside_ShouldReachContent_AndKeepOpen()
    {
        var popup = CreatePopup(out var item);
        var clicks = 0;
        item.Clicked += () => clicks++;
        Show(popup);

        _h.Click(50, 55);

        clicks.Should().Be(1);
        popup.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void OutsideClick_WhenDisabled_ShouldKeepOpen_ButStillBeSwallowed()
    {
        var popup = CreatePopup(out _);
        popup.CloseOnOutsideClick = false;
        Show(popup);

        _h.Click(440, 315);

        popup.IsOpen.Should().BeTrue();
        _otherClicks.Should().Be(0);
    }

    [Fact]
    public void ClickingAnchorWhileOpen_ShouldOnlyClose()
    {
        var popup = CreatePopup(out _);
        var anchorClicks = 0;
        _anchor.Clicked += () => anchorClicks++;
        Show(popup);

        _h.Click(50, 20);

        popup.IsOpen.Should().BeFalse();
        anchorClicks.Should().Be(0);
    }

    [Fact]
    public void Escape_ShouldClose_AndMarkCancelConsumedForThatFrame()
    {
        var popup = CreatePopup(out _);
        Show(popup);

        _h.KeyDown(KeyCode.Escape);

        popup.IsOpen.Should().BeFalse();
        _h.UI.IsCancelConsumed.Should().BeTrue();

        _h.KeyUp(KeyCode.Escape);
        _h.UI.IsCancelConsumed.Should().BeFalse();
    }

    [Fact]
    public void Escape_WhenNoLayerOpen_ShouldNotBeConsumed()
    {
        _h.KeyDown(KeyCode.Escape);

        _h.UI.IsCancelConsumed.Should().BeFalse();
    }

    [Fact]
    public void Escape_WhenCloseOnCancelDisabled_ShouldKeepOpen_ButBeConsumed()
    {
        var popup = CreatePopup(out _);
        popup.CloseOnCancel = false;
        Show(popup);

        _h.KeyDown(KeyCode.Escape);

        popup.IsOpen.Should().BeTrue();
        _h.UI.IsCancelConsumed.Should().BeTrue();
    }

    [Fact]
    public void Show_ShouldMoveFocusToFocusTarget_AndRestoreItToPreviousOnClose()
    {
        var popup = CreatePopup(out var item);
        popup.FocusTarget = item;
        _h.UI.SetFocus(_anchor);

        Show(popup);
        _h.UI.FocusedElement.Should().BeSameAs(item);

        popup.Close();
        _h.UI.FocusedElement.Should().BeSameAs(_anchor);
    }

    [Fact]
    public void ShouldClose_WhenAnchorIsRemovedFromTree()
    {
        var popup = CreatePopup(out _);
        Show(popup);

        _h.App.GlobalForeground.Remove(_anchor);
        _h.Frame(2);

        popup.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void ShouldFollowAnchor_WhenItMoves()
    {
        var popup = CreatePopup(out _);
        Show(popup);

        _anchor.Location = (200, 100);
        _h.Frame(2);

        popup.Location.Should().Be(new Vector(200, 130));
    }

    [Fact]
    public void ShouldFlipAbove_WhenAnchorIsNearBottom()
    {
        var popup = CreatePopup(out _);
        var anchor = new Button("low").Size(100, 30);
        anchor.Location = (50, View.Y - 40);
        _h.App.GlobalForeground.Add(anchor);
        _h.Frame(2);

        Show(popup, anchor);

        popup.Location.Y.Should().Be(View.Y - 40 - 30);
        popup.AvailableSize.Y.Should().Be(View.Y - 40);
    }

    [Fact]
    public void ShouldStayInsideView_WhenAnchorIsNearRightEdge()
    {
        var popup = CreatePopup(out _);
        var anchor = new Button("right").Size(40, 30);
        anchor.Location = (View.X - 40, 100);
        _h.App.GlobalForeground.Add(anchor);
        _h.Frame(2);

        Show(popup, anchor);

        (popup.Location.X + popup.Size.X).Should().BeLessThanOrEqualTo(View.X);
    }

    [Fact]
    public void ShowAt_ShouldPlaceAtPointAndHaveNoAnchor()
    {
        var popup = CreatePopup(out _);

        popup.ShowAt(_h.UI, (300, 200));
        _h.Frame(2);

        popup.Anchor.Should().BeNull();
        popup.Location.Should().Be(new Vector(300, 200));
        popup.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void Popup_ShouldSizeToContent()
    {
        var popup = CreatePopup(out _);
        Show(popup);

        popup.Size.Should().Be(new VectorInt(100, 30));
    }

    [Fact]
    public void Popup_ShouldSizeToStackContainerContent()
    {
        var popup = new Popup();
        popup.Add(
            new StackContainer(StackDirection.Vertical, gap: 4, padding: 8)
            {
                new Button("a").Size(120, 32),
                new Button("b").Size(120, 32),
            }
        );

        Show(popup);

        popup.Size.Should().Be(new VectorInt(136, 84));
    }
}
