using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// UIManager のレイヤースタック (モーダル・ポップアップの土台) の挙動を検証します。
/// </summary>
public class UILayerTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _outside;
    private int _outsideClicks;

    public UILayerTests()
    {
        _outside = new Button("outside").Location(10, 10).Size(100, 30);
        _outside.Clicked += () => _outsideClicks++;
        _h.App.GlobalForeground.Add(_outside);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    /// <summary>(300, 100) に 120x80 のポップアップ用レイヤーを積みます。ルートは背景を兼ねません。</summary>
    private Panel PushPopup(UILayerOptions options, out Button item)
    {
        var popup = new Panel().Location(300, 100).Size(120, 80);
        item = new Button("item").Location(10, 10).Size(100, 30);
        popup.Add(item);
        _h.App.GlobalForeground.Add(popup);
        _h.UI.PushLayer(popup, options);
        _h.Frame(2);
        return popup;
    }

    [Fact]
    public void OutsideClick_ShouldInvokeCallback_AndNotReachElementBelow()
    {
        var invoked = 0;
        PushPopup(new UILayerOptions { OnOutsideClick = () => invoked++ }, out _);

        _h.Click(50, 20);

        invoked.Should().Be(1);
        _outsideClicks.Should().Be(0);
    }

    [Fact]
    public void ClickInsideLayer_ShouldNotInvokeOutsideCallback()
    {
        var invoked = 0;
        var clicked = 0;
        PushPopup(new UILayerOptions { OnOutsideClick = () => invoked++ }, out var item);
        item.Clicked += () => clicked++;

        _h.Click(320, 120);

        invoked.Should().Be(0);
        clicked.Should().Be(1);
    }

    [Fact]
    public void ClickOnRoot_ShouldNotCountAsOutside_WhenRootIsNotBackdrop()
    {
        var invoked = 0;
        PushPopup(new UILayerOptions { OnOutsideClick = () => invoked++ }, out _);

        // ポップアップ本体 (ルート) の、項目以外の部分
        _h.Click(410, 170);

        invoked.Should().Be(0);
    }

    [Fact]
    public void ClickOnRoot_ShouldCountAsOutside_WhenRootIsBackdrop()
    {
        var invoked = 0;
        PushPopup(
            new UILayerOptions { RootIsBackdrop = true, OnOutsideClick = () => invoked++ },
            out _
        );

        _h.Click(410, 170);

        invoked.Should().Be(1);
    }

    [Fact]
    public void Escape_ShouldInvokeCancelOfTopLayerOnly()
    {
        var lower = 0;
        var upper = 0;
        PushPopup(new UILayerOptions { OnCancel = () => lower++ }, out _);
        PushPopup(new UILayerOptions { OnCancel = () => upper++ }, out _);

        _h.PressKey(KeyCode.Escape);

        upper.Should().Be(1);
        lower.Should().Be(0);
    }

    [Fact]
    public void Escape_ShouldDoNothing_WhenLayerHasNoCancelCallback()
    {
        var popup = PushPopup(new UILayerOptions(), out _);

        _h.PressKey(KeyCode.Escape);

        _h.UI.RemoveLayer(popup).Should().BeTrue();
    }

    [Fact]
    public void RemoveLayer_ShouldRestoreFocus_WhenRequested()
    {
        _h.UI.SetFocus(_outside);
        var popup = PushPopup(new UILayerOptions { RestoreFocus = true }, out var item);
        _h.UI.SetFocus(item);

        _h.UI.RemoveLayer(popup);

        _h.UI.FocusedElement.Should().BeSameAs(_outside);
    }

    [Fact]
    public void RemoveLayer_ShouldNotRestoreFocus_ByDefault()
    {
        _h.UI.SetFocus(_outside);
        var popup = PushPopup(new UILayerOptions(), out var item);
        _h.UI.SetFocus(item);

        _h.UI.RemoveLayer(popup);

        _h.UI.FocusedElement.Should().BeSameAs(item);
    }

    [Fact]
    public void RemoveLayer_ShouldReturnFalse_ForUnknownRoot()
    {
        _h.UI.RemoveLayer(new Panel()).Should().BeFalse();
    }

    [Fact]
    public void PopupAboveModal_ShouldKeepModalAsCurrentModal_AndBlockModalContent()
    {
        var modal = new Modal((300, 200));
        modal.Size = (800, 600);
        var modalClicks = 0;
        var modalButton = new Button("m").Location(10, 10).Size(100, 30);
        modalButton.Clicked += () => modalClicks++;
        modal.Body.Add(modalButton);
        modal.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);

        PushPopup(new UILayerOptions(), out _);

        _h.UI.CurrentModal.Should().BeSameAs(modal);

        // モーダル内ボタン (260, 210) は、ポップアップが最上位の間は押せない
        _h.Click(270, 220);
        modalClicks.Should().Be(0);
    }

    [Fact]
    public void RemovingLowerLayer_ShouldKeepTopLayerActive()
    {
        var invoked = 0;
        var lower = PushPopup(new UILayerOptions(), out _);
        PushPopup(new UILayerOptions { OnOutsideClick = () => invoked++ }, out _);

        _h.UI.RemoveLayer(lower);
        _h.Click(50, 20);

        invoked.Should().Be(1);
    }
}
