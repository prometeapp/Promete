using FluentAssertions;
using Promete.UI;

namespace Promete.Test.UI;

public class PopupPlacementTests
{
    private static readonly Vector View = (800, 600);

    private static Rect Anchor(float x, float y, float w = 100, float h = 30) => new(x, y, w, h);

    [Fact]
    public void Compute_ShouldPlaceBelowAnchor_WhenItFits()
    {
        var result = PopupPlacement.Compute(Anchor(100, 100), (120, 200), View);

        result.Side.Should().Be(PopupSide.Bottom);
        result.Location.Should().Be(new Vector(100, 130));
    }

    [Fact]
    public void Compute_ShouldFlipAbove_WhenBelowDoesNotFit()
    {
        var result = PopupPlacement.Compute(Anchor(100, 500), (120, 200), View);

        result.Side.Should().Be(PopupSide.Top);
        result.Location.Should().Be(new Vector(100, 300));
    }

    [Fact]
    public void Compute_ShouldChooseLargerSide_WhenNeitherFits()
    {
        // 下の空き 600-130=470 / 上の空き 100。どちらも 500 は入らない
        var result = PopupPlacement.Compute(Anchor(100, 100), (120, 500), View);

        result.Side.Should().Be(PopupSide.Bottom);
        result.AvailableSize.Y.Should().Be(470);
        // 画面内に収めるため、下端が 600 を超えない位置に補正される
        result.Location.Y.Should().Be(100);
    }

    [Fact]
    public void Compute_ShouldKeepPreferredSide_WhenSpaceIsEqual()
    {
        // 下の空き 285 / 上の空き 285 (どちらも 400 は入らない)
        var result = PopupPlacement.Compute(Anchor(100, 285), (50, 400), View);

        result.Side.Should().Be(PopupSide.Bottom);
    }

    [Fact]
    public void Compute_ShouldClampHorizontally_AtRightEdge()
    {
        var result = PopupPlacement.Compute(Anchor(750, 100, w: 40), (200, 100), View);

        result.Location.X.Should().Be(600);
    }

    [Fact]
    public void Compute_ShouldClampHorizontally_AtLeftEdge()
    {
        var result = PopupPlacement.Compute(
            Anchor(10, 100, w: 40),
            (200, 100),
            View,
            align: PopupAlign.End
        );

        result.Location.X.Should().Be(0);
    }

    [Fact]
    public void Compute_ShouldPinToZero_WhenPopupIsLargerThanView()
    {
        var result = PopupPlacement.Compute(Anchor(100, 100), (900, 100), View);

        result.Location.X.Should().Be(0);
    }

    [Theory]
    [InlineData(PopupAlign.Start, 100)]
    [InlineData(PopupAlign.Center, 70)]
    [InlineData(PopupAlign.End, 40)]
    public void Compute_ShouldAlignHorizontally_ForBelowAndAbove(PopupAlign align, float expectedX)
    {
        // アンカーの幅 100 / ポップアップの幅 160
        var result = PopupPlacement.Compute(Anchor(100, 100), (160, 50), View, align: align);

        result.Location.X.Should().Be(expectedX);
    }

    [Fact]
    public void Compute_ShouldApplyGap()
    {
        var below = PopupPlacement.Compute(Anchor(100, 100), (120, 100), View, gap: 4);
        var above = PopupPlacement.Compute(Anchor(100, 500), (120, 100), View, gap: 4);

        below.Location.Y.Should().Be(134);
        above.Location.Y.Should().Be(396);
    }

    [Fact]
    public void Compute_ShouldPlaceRight_WhenPreferredAndFits()
    {
        var result = PopupPlacement.Compute(
            Anchor(100, 100),
            (150, 80),
            View,
            preferred: PopupSide.Right
        );

        result.Side.Should().Be(PopupSide.Right);
        result.Location.Should().Be(new Vector(200, 100));
    }

    [Fact]
    public void Compute_ShouldFlipLeft_WhenRightDoesNotFit()
    {
        var result = PopupPlacement.Compute(
            Anchor(700, 100),
            (150, 80),
            View,
            preferred: PopupSide.Right
        );

        result.Side.Should().Be(PopupSide.Left);
        result.Location.Should().Be(new Vector(550, 100));
    }

    [Fact]
    public void Compute_ShouldClampVertically_ForSidePlacements()
    {
        var result = PopupPlacement.Compute(
            Anchor(100, 580, h: 20),
            (150, 200),
            View,
            preferred: PopupSide.Right
        );

        result.Location.Y.Should().Be(400);
    }

    [Fact]
    public void Compute_AvailableSize_ShouldReflectChosenSide()
    {
        var below = PopupPlacement.Compute(Anchor(100, 100), (120, 100), View);
        var above = PopupPlacement.Compute(Anchor(100, 500), (120, 200), View);

        below.AvailableSize.Should().Be(new Vector(800, 470));
        above.AvailableSize.Should().Be(new Vector(800, 500));
    }
}
