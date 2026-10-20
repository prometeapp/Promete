using FluentAssertions;
using Promete.UI;
using Promete.UI.Elements;

namespace Promete.Test.UI;

public class SpatialNavigationTests
{
    private static Button At(float x, float y)
    {
        var button = new Button("b");
        button.Location = (x, y);
        button.Size = (40, 20);
        return button;
    }

    [Fact]
    public void FindSpatialNeighbor_ShouldPickNearestInDirection()
    {
        var center = At(100, 100);
        var right = At(200, 100);
        var farRight = At(300, 100);
        var below = At(100, 200);
        var all = new[] { center, right, farRight, below };

        UIManager.FindSpatialNeighbor(center, all, 1, 0).Should().BeSameAs(right);
        UIManager.FindSpatialNeighbor(center, all, 0, 1).Should().BeSameAs(below);
    }

    [Fact]
    public void FindSpatialNeighbor_ShouldReturnNull_WhenNothingInDirection()
    {
        var center = At(100, 100);
        var right = At(200, 100);
        var all = new[] { center, right };

        UIManager.FindSpatialNeighbor(center, all, -1, 0).Should().BeNull();
        UIManager.FindSpatialNeighbor(center, all, 0, -1).Should().BeNull();
    }

    [Fact]
    public void FindSpatialNeighbor_ShouldPreferAlignedElement()
    {
        var center = At(100, 100);
        var aligned = At(260, 100);
        var offset = At(180, 160);
        var all = new[] { center, aligned, offset };

        // offset は進行方向では近いが、直交方向のずれが重み付けされるため aligned が選ばれる
        UIManager.FindSpatialNeighbor(center, all, 1, 0).Should().BeSameAs(aligned);
    }

    [Fact]
    public void FindSpatialNeighbor_ShouldIgnoreSelf()
    {
        var only = At(100, 100);

        UIManager.FindSpatialNeighbor(only, new[] { only }, 1, 0).Should().BeNull();
    }
}
