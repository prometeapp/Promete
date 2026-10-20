using FluentAssertions;
using Promete.Headless;
using Promete.Input;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class UIElementTests : IDisposable
{
    private readonly PrometeApp _app;

    public UIElementTests()
    {
        _app = PrometeApp
            .Create()
            .Use<Keyboard>()
            .Use<Mouse>()
            .Use<UIManager>()
            .BuildWithHeadless();
        _app.OnStart();
    }

    public void Dispose()
    {
        _app.OnDestroy();
    }

    [Fact]
    public void HitTest_ShouldDetectPointInside()
    {
        var button = new Button("テスト");
        button.Location = (100, 100);
        button.Size = (200, 50);

        // 内側
        button.HitTest((150, 120)).Should().BeTrue();
        // 左上端
        button.HitTest((100, 100)).Should().BeTrue();

        // 外側
        button.HitTest((99, 100)).Should().BeFalse();
        button.HitTest((100, 99)).Should().BeFalse();
        button.HitTest((300, 100)).Should().BeFalse();
        button.HitTest((100, 150)).Should().BeFalse();
    }

    [Fact]
    public void State_ShouldTriggerDirtyFlag()
    {
        var button = new Button("テスト");

        button.State = UIElementState.Hovered;
        button.State.Should().Be(UIElementState.Hovered);
    }

    [Fact]
    public void IsHovered_ShouldReflectState()
    {
        var button = new Button("テスト");

        button.State = UIElementState.Hovered | UIElementState.Focused;

        button.IsHovered.Should().BeTrue();
        button.IsFocused.Should().BeTrue();
        button.IsPressed.Should().BeFalse();
    }

    [Fact]
    public void Events_ShouldFire()
    {
        var element = new Button("テスト");
        var pointerEntered = false;
        var pointerLeft = false;
        var gotFocus = false;
        var lostFocus = false;
        var clicked = false;

        element.PointerEntered += () => pointerEntered = true;
        element.PointerLeft += () => pointerLeft = true;
        element.GotFocus += () => gotFocus = true;
        element.LostFocus += () => lostFocus = true;
        element.Clicked += () => clicked = true;

        element.RaisePointerEntered();
        element.RaisePointerLeft();
        element.RaiseGotFocus();
        element.RaiseLostFocus();
        element.RaiseClicked();

        pointerEntered.Should().BeTrue();
        pointerLeft.Should().BeTrue();
        gotFocus.Should().BeTrue();
        lostFocus.Should().BeTrue();
        clicked.Should().BeTrue();
    }

    [Fact]
    public void GetHitRect_ShouldReturnCorrectRect()
    {
        var element = new Button("テスト");
        element.Location = (50, 100);
        element.Size = (200, 80);

        var rect = element.GetHitRect();

        rect.Left.Should().Be(50);
        rect.Top.Should().Be(100);
        rect.Width.Should().Be(200);
        rect.Height.Should().Be(80);
    }

    [Fact]
    public void Style_ShouldBeReappliedWhenSizeChanges()
    {
        var style = new SizeRecordingPanelStyle();
        var panel = new Panel(style);
        panel.Location = (300, 300);
        panel.Size = (100, 50);
        _app.GlobalForeground.Add(panel);
        _app.OnUpdate();
        style.AppliedSizes.Last().Should().Be(new VectorInt(100, 50));

        panel.Size = (200, 80);
        _app.OnUpdate();

        style.AppliedSizes.Last().Should().Be(new VectorInt(200, 80));
    }

    [Fact]
    public void Style_ShouldNotBeReapplied_WhenNothingChanged()
    {
        var style = new SizeRecordingPanelStyle();
        var panel = new Panel(style);
        panel.Location = (300, 300);
        panel.Size = (100, 50);
        _app.GlobalForeground.Add(panel);
        _app.OnUpdate();
        var count = style.AppliedSizes.Count;

        _app.OnUpdate();
        _app.OnUpdate();

        style.AppliedSizes.Count.Should().Be(count);
    }

    [Fact]
    public void AutoSize_ShouldShrinkWhenContentShrinks_EvenWithBackground()
    {
        var panel = new Panel { AutoSize = true };
        panel.Location = (300, 300);
        var child = new Button("a");
        child.Size = (100, 50);
        panel.Add(child);
        _app.GlobalForeground.Add(panel);
        _app.OnUpdate();
        panel.Size.Should().Be(new VectorInt(100, 50));

        child.Size = (40, 20);
        _app.OnUpdate();
        _app.OnUpdate();

        panel.Size.Should().Be(new VectorInt(40, 20));
    }

    private sealed class SizeRecordingPanelStyle : IUIStyle<Panel>
    {
        public List<VectorInt> AppliedSizes { get; } = [];

        public void Apply(Panel element, UIElementState state) => AppliedSizes.Add(element.Size);
    }
}
