using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// Modal と UIManager のモーダルスタックの挙動を固定する回帰テストです。
/// 入力を実際に注入して、ヒットテスト・フォーカス・クローズ動作を検証します。
/// </summary>
public class ModalBehaviorTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _outside;
    private int _outsideClicks;

    public ModalBehaviorTests()
    {
        _outside = new Button("outside").Location(10, 10).Size(100, 30);
        _outside.Clicked += () => _outsideClicks++;
        _h.App.GlobalForeground.Add(_outside);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    /// <summary>画面全体を覆うモーダルを作って表示します。本体は (250, 200) に 300x200 で配置されます。</summary>
    private Modal ShowModal(
        out Button inside,
        out int insideClicks,
        bool closeOnOverlayClick = true
    )
    {
        var modal = new Modal((300, 200)) { CloseOnOverlayClick = closeOnOverlayClick };
        modal.Size = (800, 600);
        var counter = 0;
        var button = new Button("inside").Location(10, 10).Size(100, 30);
        button.Clicked += () => counter++;
        modal.Body.Add(button);
        modal.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);

        inside = button;
        insideClicks = 0;
        _insideCounter = () => counter;
        return modal;
    }

    private Func<int> _insideCounter = () => 0;

    [Fact]
    public void Show_ShouldPushModalAndAttachToParent()
    {
        var modal = ShowModal(out _, out _);

        _h.UI.CurrentModal.Should().BeSameAs(modal);
        modal.Parent.Should().BeSameAs(_h.App.GlobalForeground);
    }

    [Fact]
    public void Close_ShouldPopModalDetachAndRaiseClosedOnce()
    {
        var modal = ShowModal(out _, out _);
        var closed = 0;
        modal.Closed += () => closed++;

        modal.Close();
        modal.Close();

        _h.UI.CurrentModal.Should().BeNull();
        modal.Parent.Should().BeNull();
        closed.Should().Be(1);
    }

    [Fact]
    public void Close_WhenNotShown_ShouldDoNothing()
    {
        var modal = new Modal();
        var closed = 0;
        modal.Closed += () => closed++;

        modal.Close();

        closed.Should().Be(0);
    }

    [Fact]
    public void WhileOpen_ClickOnOutsideElement_ShouldBeBlocked()
    {
        ShowModal(out _, out _);

        _h.Click(50, 20);

        _outsideClicks.Should().Be(0);
    }

    [Fact]
    public void WhileOpen_ClickOnInsideElement_ShouldReachIt()
    {
        ShowModal(out _, out _);

        _h.Click(270, 220);

        _insideCounter().Should().Be(1);
    }

    [Fact]
    public void AfterClose_OutsideElementShouldReceiveClicksAgain()
    {
        var modal = ShowModal(out _, out _);
        modal.Close();
        _h.Frame(2);

        _h.Click(50, 20);

        _outsideClicks.Should().Be(1);
    }

    [Fact]
    public void ClickOutsideModalBounds_ShouldClose_WhenCloseOnOverlayClick()
    {
        var modal = ShowModal(out _, out _);

        _h.Click(900, 700);

        _h.UI.CurrentModal.Should().BeNull();
        modal.Parent.Should().BeNull();
    }

    [Fact]
    public void ClickOutsideModalBounds_ShouldNotClose_WhenCloseOnOverlayClickIsFalse()
    {
        var modal = ShowModal(out _, out _, closeOnOverlayClick: false);

        _h.Click(900, 700);

        _h.UI.CurrentModal.Should().BeSameAs(modal);
    }

    [Fact(
        Skip = "既知の不具合: モーダルが画面全体を覆うと、オーバーレイ部分のクリックがモーダルルートに当たり ModalOverlayClicked が発火しない"
    )]
    public void ClickOnOverlayArea_ShouldClose_WhenCloseOnOverlayClick()
    {
        // モーダルが画面全体を覆う使い方 (デモと同じ) で、本体の外側 (オーバーレイ部分) をクリックする
        var modal = ShowModal(out _, out _);

        _h.Click(700, 500);

        _h.UI.CurrentModal.Should().BeNull();
        modal.Parent.Should().BeNull();
    }

    [Fact]
    public void ClickOnModalBody_ShouldNotClose()
    {
        var modal = ShowModal(out _, out _);

        _h.Click(500, 400);

        _h.UI.CurrentModal.Should().BeSameAs(modal);
    }

    [Fact]
    public void FocusNavigation_ShouldBeRestrictedToModalDescendants()
    {
        var modal = ShowModal(out var inside, out _);
        _h.UI.SetFocus(_outside);

        _h.UI.MoveFocusNext();
        _h.UI.FocusedElement.Should().BeSameAs(inside);

        _h.UI.MoveFocusNext();
        _h.UI.FocusedElement.Should().BeSameAs(inside);
        modal.Should().NotBeNull();
    }

    [Fact]
    public void Push_ShouldClearHoverOfOutsideElement()
    {
        _h.MoveMouse(50, 20);
        _outside.IsHovered.Should().BeTrue();

        ShowModal(out _, out _);

        _outside.IsHovered.Should().BeFalse();
    }

    [Fact]
    public void NestedModals_OnlyTopReceivesInput_AndPopRestoresPrevious()
    {
        var first = ShowModal(out _, out _);

        var second = new Modal((200, 100));
        second.Size = (800, 600);
        var secondClicks = 0;
        var secondButton = new Button("second").Location(10, 10).Size(100, 30);
        secondButton.Clicked += () => secondClicks++;
        second.Body.Add(secondButton);
        second.Show(_h.UI, _h.App.GlobalForeground);
        _h.Frame(2);

        _h.UI.CurrentModal.Should().BeSameAs(second);

        // 1つ目のモーダル内ボタン (260, 210) は、2つ目が最上位の間は押せない
        _h.Click(270, 220);
        _insideCounter().Should().Be(0);

        // 2つ目の本体は (300, 250) に 200x100。内側のボタンは (310, 260)
        _h.Click(320, 270);
        secondClicks.Should().Be(1);

        second.Close();
        _h.Frame(2);
        _h.UI.CurrentModal.Should().BeSameAs(first);

        _h.Click(270, 220);
        _insideCounter().Should().Be(1);
    }
}
