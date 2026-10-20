using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class TabBarTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly TabBar _bar;
    private readonly Container _pageA = new();
    private readonly Container _pageB = new();
    private readonly Container _pageC = new();
    private readonly Button _buttonA = new Button("a").Location(10, 10).Size(80, 30);
    private readonly Button _buttonB = new Button("b").Location(10, 10).Size(80, 30);
    private int _clicksA;
    private int _clicksB;

    public TabBarTests()
    {
        _buttonA.Clicked += () => _clicksA++;
        _buttonB.Clicked += () => _clicksB++;
        _pageA.Add(_buttonA);
        _pageB.Add(_buttonB);

        _bar = new TabBar().Location(100, 100).Size(400, 300);
        _bar.AddTab("A", _pageA).AddTab("B", _pageB).AddTab("C", _pageC);
        _h.App.GlobalForeground.Add(_bar);
        _h.Frame(3);
    }

    public void Dispose() => _h.Dispose();

    private void ClickTab(int index)
    {
        var center = _bar.GetTabButton(index).GetHitRect().Center;
        _h.Click((int)center.X, (int)center.Y);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var bar = new TabBar();

        bar.SelectedIndex.Should().Be(-1);
        bar.SelectedPage.Should().BeNull();
        bar.TabCount.Should().Be(0);
        bar.IsFocusable.Should().BeFalse();
    }

    [Fact]
    public void FirstTab_ShouldBeSelectedAutomatically_AndOthersHidden()
    {
        _bar.SelectedIndex.Should().Be(0);
        _bar.SelectedPage.Should().BeSameAs(_pageA);
        _pageA.IsVisible.Should().BeTrue();
        _pageB.IsVisible.Should().BeFalse();
        _pageC.IsVisible.Should().BeFalse();
        _bar.GetTabButton(0).IsSelected.Should().BeTrue();
        _bar.GetTabButton(1).IsSelected.Should().BeFalse();
    }

    [Fact]
    public void SelectedIndex_ShouldSwitchPages_AndRaiseEvent()
    {
        var raised = new List<int>();
        _bar.SelectionChanged += raised.Add;

        _bar.SelectedIndex = 1;

        _pageA.IsVisible.Should().BeFalse();
        _pageB.IsVisible.Should().BeTrue();
        _bar.GetTabButton(1).IsSelected.Should().BeTrue();
        raised.Should().Equal(1);
    }

    [Fact]
    public void SelectedIndex_ShouldIgnoreOutOfRangeAndSameValue()
    {
        var raised = 0;
        _bar.SelectionChanged += _ => raised++;

        _bar.SelectedIndex = 99;
        _bar.SelectedIndex = -1;
        _bar.SelectedIndex = 0;

        _bar.SelectedIndex.Should().Be(0);
        raised.Should().Be(0);
    }

    [Fact]
    public void ClickingTab_ShouldSelectIt()
    {
        ClickTab(2);

        _bar.SelectedIndex.Should().Be(2);
        _pageC.IsVisible.Should().BeTrue();
    }

    [Fact]
    public void Keyboard_EnterOnFocusedTab_ShouldSelectIt()
    {
        _h.UI.SetFocus(_bar.GetTabButton(1));

        _h.PressKey(KeyCode.Enter);

        _bar.SelectedIndex.Should().Be(1);
    }

    [Fact]
    public void Tabs_ShouldBeLaidOutHorizontally_AboveThePages()
    {
        var first = _bar.GetTabButton(0);
        var second = _bar.GetTabButton(1);

        second.Location.X.Should().BeGreaterThan(first.Location.X);
        second.Location.Y.Should().Be(first.Location.Y);
        first.Size.X.Should().Be(first.Label.Size.X + 32);
        _pageA.Location.Y.Should().Be(_bar.Header.Size.Y + 8);
        _pageA.Location.X.Should().Be(8);
    }

    [Fact]
    public void ContentPadding_ShouldMovePages()
    {
        _bar.ContentPadding = new Padding(20, 30, 0, 0);
        _h.Frame(3);

        _pageA.Location.Should().Be(new Vector(20, _bar.Header.Size.Y + 30));
    }

    [Fact]
    public void StyleAndTabStyle_ShouldControlHeaderLayout()
    {
        _bar.Style = new DefaultTabBarStyle { HeaderGap = 20 };
        _bar.TabStyle = new DefaultTabButtonStyle { HorizontalPadding = 30, Height = 40 };
        _h.Frame(3);

        var first = _bar.GetTabButton(0);
        var second = _bar.GetTabButton(1);
        first.Size.Y.Should().Be(40);
        first.Size.X.Should().Be(first.Label.Size.X + 60);
        (second.Location.X - first.Location.X).Should().Be(first.Size.X + 20);
    }

    [Fact]
    public void HiddenPage_ShouldNotReceiveClicks_ButSelectedPageShould()
    {
        // 選択中のページ A のボタンは、ページの左上から (10, 10) の位置にある
        var origin = _pageA.AbsoluteLocation;
        _h.Click((int)origin.X + 50, (int)origin.Y + 25);
        _clicksA.Should().Be(1);
        _clicksB.Should().Be(0);

        _bar.SelectedIndex = 1;
        _h.Frame(2);
        _h.Click((int)origin.X + 50, (int)origin.Y + 25);

        _clicksA.Should().Be(1);
        _clicksB.Should().Be(1);
    }

    [Fact]
    public void FocusNavigation_ShouldSkipHiddenPages()
    {
        _h.UI.SetFocus(_bar.GetTabButton(2));

        _h.UI.MoveFocusNext();

        // 表示中のページ A のボタンへは進むが、非表示の B のボタンへは進まない
        _h.UI.FocusedElement.Should().BeSameAs(_buttonA);
        _h.UI.MoveFocusNext();
        _h.UI.FocusedElement.Should().BeSameAs(_bar.GetTabButton(0));
    }

    [Fact]
    public void SwitchingTab_ShouldMoveFocusToTab_WhenFocusWasInsideTheOldPage()
    {
        _h.UI.SetFocus(_buttonA);

        _bar.SelectedIndex = 1;

        _h.UI.FocusedElement.Should().BeSameAs(_bar.GetTabButton(1));
    }

    [Fact]
    public void RemoveTab_ShouldDetachPage_AndSelectNeighbor()
    {
        _bar.SelectedIndex = 1;
        var raised = new List<int>();
        _bar.SelectionChanged += raised.Add;

        _bar.RemoveTab(1);

        _bar.TabCount.Should().Be(2);
        _pageB.Parent.Should().BeNull();
        _pageB.IsVisible.Should().BeTrue();
        _bar.SelectedPage.Should().BeSameAs(_pageC);
        _pageC.IsVisible.Should().BeTrue();
        raised.Should().Equal(1);
    }

    [Fact]
    public void RemoveTab_BeforeSelected_ShouldKeepSelectedPage()
    {
        _bar.SelectedIndex = 2;

        _bar.RemoveTab(0);

        _bar.SelectedIndex.Should().Be(1);
        _bar.SelectedPage.Should().BeSameAs(_pageC);
    }

    [Fact]
    public void RemoveTab_LastTab_ShouldClearSelection()
    {
        var raised = new List<int>();
        _bar.SelectionChanged += raised.Add;

        _bar.RemoveTab(2);
        _bar.RemoveTab(1);
        _bar.RemoveTab(0);

        _bar.SelectedIndex.Should().Be(-1);
        _bar.SelectedPage.Should().BeNull();
        raised.Should().EndWith(-1);
    }
}
