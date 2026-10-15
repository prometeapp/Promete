using FluentAssertions;
using Promete.Nodes;

namespace Promete.Test;

public class StackContainerTests
{
    private static Sprite CreateSprite(int width, int height) => new() { Size = (width, height) };

    [Fact]
    public void Vertical_ShouldStackChildrenWithGap()
    {
        // Arrange
        var a = CreateSprite(10, 20);
        var b = CreateSprite(30, 10);
        var c = CreateSprite(20, 5);

        // Act
        var stack = new StackContainer(StackDirection.Vertical, gap: 3) { a, b, c };

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        stack.Size.Should().Be(new VectorInt(30, 20 + 3 + 10 + 3 + 5));
        a.Location.Should().Be(new Vector(0, 0));
        b.Location.Should().Be(new Vector(0, 23));
        c.Location.Should().Be(new Vector(0, 36));
    }

    [Fact]
    public void Horizontal_ShouldStackChildrenWithGap()
    {
        // Arrange
        var a = CreateSprite(10, 20);
        var b = CreateSprite(30, 10);

        // Act
        var stack = new StackContainer(StackDirection.Horizontal, gap: 5) { a, b };

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        stack.Size.Should().Be(new VectorInt(45, 20));
        a.Location.Should().Be(new Vector(0, 0));
        b.Location.Should().Be(new Vector(15, 0));
    }

    [Fact]
    public void Padding_ShouldOffsetChildrenAndGrowSize()
    {
        // Arrange
        var a = CreateSprite(10, 10);
        var b = CreateSprite(10, 10);

        // Act
        var stack = new StackContainer(
            StackDirection.Vertical,
            gap: 2,
            padding: new Padding(1, 2, 3, 4)
        )
        {
            a,
            b,
        };

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        a.Location.Should().Be(new Vector(1, 2));
        b.Location.Should().Be(new Vector(1, 14));
        stack.Size.Should().Be(new VectorInt(1 + 10 + 3, 2 + 10 + 2 + 10 + 4));
    }

    [Theory]
    [InlineData(StackAlignment.Start, 0)]
    [InlineData(StackAlignment.Center, 10)]
    [InlineData(StackAlignment.End, 20)]
    public void Align_ShouldAlignOnCrossAxis(StackAlignment align, float expectedX)
    {
        // Arrange
        var wide = CreateSprite(30, 10);
        var narrow = CreateSprite(10, 10);

        // Act
        var stack = new StackContainer(StackDirection.Vertical, align: align) { wide, narrow };

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        narrow.Location.X.Should().Be(expectedX);
    }

    [Fact]
    public void Layout_ShouldConsiderPivotAndRotation()
    {
        // Arrange
        var pivoted = CreateSprite(10, 10);
        pivoted.Pivot = (0.5f, 0.5f);
        var rotated = CreateSprite(10, 20);
        rotated.Angle = Angle.FromDegrees(90);

        // Act
        var stack = new StackContainer(StackDirection.Horizontal) { pivoted, rotated };

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        // 外接矩形の左上が (0,0), (10,0) に来るよう配置される
        pivoted.Location.Should().Be(new Vector(5, 5));
        rotated.Location.X.Should().BeApproximately(30, 0.001f);
        rotated.Location.Y.Should().BeApproximately(0, 0.001f);
        stack.Size.Should().Be(new VectorInt(30, 10));
    }

    [Fact]
    public void Layout_ShouldFollowChildChanges()
    {
        // Arrange
        var a = CreateSprite(10, 10);
        var b = CreateSprite(10, 10);
        var stack = new StackContainer(StackDirection.Vertical, gap: 1) { a, b };
        _ = stack.Size;
        b.Location.Y.Should().Be(11);

        // Act & Assert
        a.Size = (10, 30);
        _ = stack.Size;
        b.Location.Y.Should().Be(31);
        stack.Size.Should().Be(new VectorInt(10, 41));

        stack.Remove(a);
        _ = stack.Size;
        b.Location.Y.Should().Be(0);
        stack.Size.Should().Be(new VectorInt(10, 10));
    }

    [Fact]
    public void Layout_ShouldFollowPropertyChanges()
    {
        // Arrange
        var a = CreateSprite(10, 10);
        var b = CreateSprite(10, 10);
        var stack = new StackContainer { a, b };

        // Act
        stack.Direction = StackDirection.Horizontal;
        stack.Gap = 4;

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        b.Location.Should().Be(new Vector(14, 0));
        stack.Size.Should().Be(new VectorInt(24, 10));
    }

    [Fact]
    public void Layout_ShouldPropagateToNestedStacks()
    {
        // Arrange
        var sprite = CreateSprite(10, 10);
        var inner = new StackContainer(StackDirection.Horizontal) { sprite };
        var outer = new StackContainer(StackDirection.Vertical, gap: 2)
        {
            CreateSprite(5, 5),
            inner,
        };
        outer.Size.Should().Be(new VectorInt(10, 17));

        // Act
        sprite.Size = (40, 20);

        // Assert
        outer.Size.Should().Be(new VectorInt(40, 27));
        inner.Location.Should().Be(new Vector(0, 7));
    }

    [Fact]
    public void AutoSize_ShouldAlwaysBeTrue()
    {
        // Arrange
        var stack = new StackContainer();

        // Act
        stack.AutoSize = false;
        stack.Size = (100, 100);

        // Assert
        _ = stack.Size; // 配置は Size の読み取り・Update・描画前に反映される
        stack.AutoSize.Should().BeTrue();
        stack.Size.Should().Be(new VectorInt(0, 0));
    }
}
