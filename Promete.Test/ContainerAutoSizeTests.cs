using FluentAssertions;
using Promete.Nodes;

namespace Promete.Test;

public class ContainerAutoSizeTests
{
    private static Sprite CreateSprite(int width, int height, float x = 0, float y = 0) =>
        new() { Size = (width, height), Location = (x, y) };

    private static Container CreateAuto(params Node[] children)
    {
        var container = new Container { AutoSize = true };
        container.AddRange(children);
        return container;
    }

    [Fact]
    public void AutoSize_ShouldBeDisabledByDefault()
    {
        // Arrange
        var container = new Container { CreateSprite(10, 10) };

        // Assert
        container.AutoSize.Should().BeFalse();
        container.Size.Should().Be(new VectorInt(0, 0));
    }

    [Fact]
    public void AutoSize_WithoutChildren_ShouldBeZero()
    {
        // Arrange
        var container = new Container { AutoSize = true };

        // Assert
        container.Size.Should().Be(new VectorInt(0, 0));
    }

    [Fact]
    public void AutoSize_ShouldFitChildrenBounds()
    {
        // Arrange
        var container = new Container { AutoSize = true };

        // Act
        container.Add(CreateSprite(10, 20, 5, 5));
        container.Add(CreateSprite(30, 10, 20, 0));

        // Assert
        container.Size.Should().Be(new VectorInt(50, 25));
    }

    [Fact]
    public void AutoSize_ShouldFollowChildChanges()
    {
        // Arrange
        var sprite = CreateSprite(10, 10);
        var container = CreateAuto(sprite);
        container.Size.Should().Be(new VectorInt(10, 10));

        // Act & Assert
        sprite.Location = (30, 0);
        container.Size.Should().Be(new VectorInt(40, 10));

        sprite.Size = (5, 5);
        container.Size.Should().Be(new VectorInt(35, 5));

        sprite.Scale = (2, 2);
        container.Size.Should().Be(new VectorInt(40, 10));
    }

    [Fact]
    public void AutoSize_ShouldFollowChildRemoval()
    {
        // Arrange
        var a = CreateSprite(10, 10);
        var b = CreateSprite(10, 10, 50, 0);
        var container = CreateAuto(a, b);
        container.Size.Should().Be(new VectorInt(60, 10));

        // Act
        container.Remove(b);

        // Assert
        container.Size.Should().Be(new VectorInt(10, 10));
    }

    [Fact]
    public void AutoSize_ShouldConsiderPivot()
    {
        // Arrange
        var sprite = CreateSprite(10, 10, 20, 20);
        sprite.Pivot = (0.5f, 0.5f);

        // Act
        var container = CreateAuto(sprite);

        // Assert
        container.Size.Should().Be(new VectorInt(25, 25));
    }

    [Fact]
    public void AutoSize_ShouldConsiderRotation()
    {
        // Arrange
        var sprite = CreateSprite(10, 20);
        sprite.Angle = Angle.FromDegrees(90);

        // Act
        var container = CreateAuto(sprite);

        // Assert
        // 90° 回転すると、(0,0)-(10,20) の矩形は x が -20..0 になる
        container.Size.Should().Be(new VectorInt(0, 10));
    }

    [Fact]
    public void AutoSize_ShouldUseBoundingBoxForDiagonalRotation()
    {
        // Arrange
        var sprite = CreateSprite(10, 10, 20, 0);
        sprite.Angle = Angle.FromDegrees(45);

        // Act
        var container = CreateAuto(sprite);

        // Assert
        // 45° 回転した 10x10 の矩形の外接は約 14.14 幅。原点 (20, 0) から右へ 約 +7.07、下へ 約 14.14
        container.Size.Should().Be(new VectorInt(28, 15));
    }

    [Fact]
    public void AutoSize_ShouldPropagateToAncestors()
    {
        // Arrange
        var sprite = CreateSprite(10, 10);
        var inner = CreateAuto(sprite);
        inner.Location = (5, 5);
        var outer = CreateAuto(inner);
        outer.Size.Should().Be(new VectorInt(15, 15));

        // Act
        sprite.Size = (30, 40);

        // Assert
        outer.Size.Should().Be(new VectorInt(35, 45));
    }

    [Fact]
    public void AutoSize_WhenDisabled_ShouldKeepManualSize()
    {
        // Arrange
        var container = CreateAuto(CreateSprite(10, 10));

        // Act
        container.AutoSize = false;
        container.Size = (100, 100);
        container.Add(CreateSprite(500, 500));

        // Assert
        container.Size.Should().Be(new VectorInt(100, 100));
    }

    [Fact]
    public void AutoSize_ShouldIgnoreManualSize()
    {
        // Arrange
        var container = CreateAuto(CreateSprite(10, 10));

        // Act
        container.Width = 999;

        // Assert
        container.Size.Should().Be(new VectorInt(10, 10));
    }
}
