using FluentAssertions;
using Promete.Input;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// 非表示のノードの子孫が、入力とフォーカスの対象にならないことを検証します。
/// </summary>
public class VisibilityTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Container _container;
    private readonly Button _inside;
    private readonly Button _visible;
    private int _insideClicks;

    public VisibilityTests()
    {
        _container = new Container();
        _inside = new Button("inside").Location(100, 100).Size(100, 30);
        _inside.Clicked += () => _insideClicks++;
        _container.Add(_inside);
        _visible = new Button("visible").Location(400, 100).Size(100, 30);

        _h.App.GlobalForeground.Add(_container);
        _h.App.GlobalForeground.Add(_visible);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    [Fact]
    public void VisibleContainer_ChildShouldReceiveClicks()
    {
        _h.Click(150, 115);

        _insideClicks.Should().Be(1);
    }

    [Fact]
    public void InvisibleContainer_ChildShouldNotReceiveClicks()
    {
        _container.IsVisible = false;
        _h.Frame(2);

        _h.Click(150, 115);

        _insideClicks.Should().Be(0);
    }

    [Fact]
    public void InvisibleContainer_ChildShouldBeSkippedByFocusNavigation()
    {
        _container.IsVisible = false;
        _h.Frame(2);
        _h.UI.SetFocus(_visible);

        _h.UI.MoveFocusNext();

        _h.UI.FocusedElement.Should().BeSameAs(_visible);
    }

    [Fact]
    public void InvisibleElement_ShouldHideItsChildrenToo()
    {
        var panel = new Panel().Location(300, 300).Size(200, 100);
        var child = new Button("child").Location(10, 10).Size(100, 30);
        var clicks = 0;
        child.Clicked += () => clicks++;
        panel.Add(child);
        _h.App.GlobalForeground.Add(panel);
        _h.Frame(2);
        panel.IsVisible = false;
        _h.Frame(2);

        _h.Click(320, 320);

        clicks.Should().Be(0);
    }

    [Fact]
    public void BecomingVisibleAgain_ShouldRestoreInput()
    {
        _container.IsVisible = false;
        _h.Frame(2);
        _container.IsVisible = true;
        _h.Frame(2);

        _h.Click(150, 115);

        _insideClicks.Should().Be(1);
    }
}
