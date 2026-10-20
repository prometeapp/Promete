using FluentAssertions;
using Promete.Nodes;
using Promete.UI.Elements;

namespace Promete.Test.UI;

/// <summary>
/// ScrollView の表示範囲外にある子要素が、ヒットテストの対象にならないことを検証します。
/// </summary>
public class ScrollViewHitTestTests : IDisposable
{
    private readonly UITestHarness _h = new();
    private readonly Button _visible;
    private readonly Button _hidden;
    private int _visibleClicks;
    private int _hiddenClicks;

    public ScrollViewHitTestTests()
    {
        // 表示範囲は (100, 100) から 200x100
        var scrollView = new ScrollView { ContentSize = (200, 400) };
        scrollView.Location(100, 100).Size(200, 100);

        _visible = new Button("visible").Location(0, 10).Size(100, 30);
        _visible.Clicked += () => _visibleClicks++;
        _hidden = new Button("hidden").Location(0, 150).Size(100, 30);
        _hidden.Clicked += () => _hiddenClicks++;
        scrollView.Content.Add(_visible);
        scrollView.Content.Add(_hidden);

        _h.App.GlobalForeground.Add(scrollView);
        _h.Frame(2);
    }

    public void Dispose() => _h.Dispose();

    [Fact]
    public void ChildInsideViewport_ShouldReceiveClicks()
    {
        _h.Click(150, 120);

        _visibleClicks.Should().Be(1);
    }

    [Fact]
    public void ChildOutsideViewport_ShouldNotReceiveClicks()
    {
        // 非表示の子は絶対座標 (100, 250) から 100x30 にある
        _h.Click(150, 265);

        _hiddenClicks.Should().Be(0);
    }
}
