using FluentAssertions;

namespace Promete.Test;

public class RectIntTests
{
    [Fact]
    public void IntersectShouldBeTrueWhenOverlapping()
    {
        var rect1 = new RectInt(0, 0, 10, 10);
        var rect2 = new RectInt(5, 5, 10, 10);

        rect1.Intersect(rect2).Should().BeTrue();
        rect2.Intersect(rect1).Should().BeTrue();
    }

    [Fact]
    public void IntersectShouldBeFalseWhenNotOverlapping()
    {
        var rect1 = new RectInt(0, 0, 10, 10);
        var rect2 = new RectInt(20, 20, 10, 10);

        rect1.Intersect(rect2).Should().BeFalse();
        rect2.Intersect(rect1).Should().BeFalse();
    }

    [Fact]
    public void IntersectShouldNotBeTruthyWhenOneSideIsZeroSize()
    {
        var rect1 = new RectInt(0, 0, 10, 10);
        var rect2 = new RectInt(5, 5, 0, 0);

        rect1.Intersect(rect2).Should().BeFalse();
        rect2.Intersect(rect1).Should().BeFalse();
    }

    [Fact]
    public void IntersectShouldNotBeTruthyWhenBothAreZeroSizeAtSameLocation()
    {
        var rect1 = new RectInt(5, 5, 0, 0);
        var rect2 = new RectInt(5, 5, 0, 0);

        rect1.Intersect(rect2).Should().BeFalse();
    }
}
