using FluentAssertions;
using Promete.Nodes;
using Promete.UI.Styles;

namespace Promete.Test.UI;

public class CircleShapeTests
{
    [Fact]
    public void Filled_ShouldBuildOneRowPerScanline_AndNarrowTowardEdges()
    {
        var circle = CircleShape.Filled(8, 8, 8, System.Drawing.Color.White);

        circle.Count.Should().Be(16);

        var top = (Shape)circle[0];
        var middle = (Shape)circle[8];
        var topWidth = top.Vertices[2].X - top.Vertices[0].X;
        var middleWidth = middle.Vertices[2].X - middle.Vertices[0].X;
        topWidth.Should().BeLessThan(middleWidth);
        middleWidth.Should().BeInRange(14, 16);
    }

    [Fact]
    public void Filled_ShouldStayWithinBounds()
    {
        var circle = CircleShape.Filled(8, 8, 8, System.Drawing.Color.White);

        for (var i = 0; i < circle.Count; i++)
        {
            var shape = (Shape)circle[i];
            shape.Vertices.Min(v => v.X).Should().BeGreaterThanOrEqualTo(0);
            shape.Vertices.Max(v => v.X).Should().BeLessThanOrEqualTo(16);
        }
    }

    [Fact]
    public void Filled_ShouldBeEmpty_WhenRadiusIsZero()
    {
        CircleShape.Filled(8, 8, 0, System.Drawing.Color.White).Count.Should().Be(0);
    }
}
