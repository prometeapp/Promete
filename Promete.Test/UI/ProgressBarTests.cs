using FluentAssertions;
using Promete.Nodes;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class ProgressBarTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var bar = new ProgressBar();

        bar.Value.Should().Be(0f);
        bar.Minimum.Should().Be(0f);
        bar.Maximum.Should().Be(1f);
        bar.Fraction.Should().Be(0f);
        bar.IsFocusable.Should().BeFalse();
        bar.Size.Should().Be(new VectorInt(200, 16));
    }

    [Fact]
    public void Value_ShouldBeClampedToRange()
    {
        var bar = new ProgressBar(0, 10);

        bar.Value = 25;
        bar.Value.Should().Be(10f);

        bar.Value = -5;
        bar.Value.Should().Be(0f);
    }

    [Fact]
    public void Fraction_ShouldReflectRange()
    {
        var bar = new ProgressBar(10, 30, 15);

        bar.Fraction.Should().BeApproximately(0.25f, 1e-5f);

        bar.Value = 30;
        bar.Fraction.Should().Be(1f);
    }

    [Fact]
    public void Fraction_ShouldBeZero_WhenRangeIsEmpty()
    {
        var bar = new ProgressBar(5, 5, 5);

        bar.Fraction.Should().Be(0f);
    }

    [Fact]
    public void ChangingRange_ShouldReclampValue()
    {
        var bar = new ProgressBar(0, 100, 80);

        bar.Maximum = 50;

        bar.Value.Should().Be(50f);
        bar.Fraction.Should().Be(1f);
    }

    [Fact]
    public void Style_ShouldBeReappliedWhenValueChanges()
    {
        using var h = new UITestHarness();
        var style = new FractionRecordingStyle();
        var bar = new ProgressBar(style: style);
        bar.Location = (300, 300);
        h.App.GlobalForeground.Add(bar);
        h.Frame(2);
        var count = style.Fractions.Count;

        bar.Value = 0.5f;
        h.Frame(2);

        style.Fractions.Count.Should().BeGreaterThan(count);
        style.Fractions.Last().Should().Be(0.5f);
    }

    [Fact]
    public void Style_ShouldNotBeReapplied_WhenValueIsUnchanged()
    {
        using var h = new UITestHarness();
        var style = new FractionRecordingStyle();
        var bar = new ProgressBar(0, 1, 0.5f, style);
        bar.Location = (300, 300);
        h.App.GlobalForeground.Add(bar);
        h.Frame(2);
        var count = style.Fractions.Count;

        bar.Value = 0.5f;
        h.Frame(2);

        style.Fractions.Count.Should().Be(count);
    }

    [Fact]
    public void DefaultStyle_ShouldFillProportionally()
    {
        using var h = new UITestHarness();
        var bar = new ProgressBar(0, 1, 0.5f);
        bar.Location = (300, 300);
        bar.Size = (102, 16);
        h.App.GlobalForeground.Add(bar);

        h.Frame(2);

        // 枠線 (1px) を除いた幅 100 の半分
        var background = (Container)bar[0];
        var fill = background[1];
        fill.Should().BeOfType<Shape>();
        ((Shape)fill).Vertices[2].X.Should().Be(51);
    }

    private sealed class FractionRecordingStyle : IUIStyle<ProgressBar>
    {
        public List<float> Fractions { get; } = [];

        public void Apply(ProgressBar element, UIElementState state) =>
            Fractions.Add(element.Fraction);
    }
}
