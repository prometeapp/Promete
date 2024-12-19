using FluentAssertions;
using Silk.NET.Maths;

namespace Promete.Test;

public class VectorTests
{
    [Fact]
    public void Constructor()
    {
        var v = new Vector(1, 2);

        v.X.Should().Be(1);
        v.Y.Should().Be(2);
    }

    [Fact]
    public void Equality()
    {
        var v1 = new Vector(1, 2);
        var v2 = new Vector(1, 2);
        var v3 = new Vector(2, 1);

        v1.Should().Be(v2);
        v1.Should().NotBe(v3);
    }

    [Fact]
    public void Addition()
    {
        var v1 = new Vector(1, 2);
        var v2 = new Vector(3, 4);
        var v3 = v1 + v2;

        v3.X.Should().Be(4);
        v3.Y.Should().Be(6);
    }

    [Fact]
    public void Subtraction()
    {
        var v1 = new Vector(1, 2);
        var v2 = new Vector(3, 3);
        var v3 = v1 - v2;

        v3.X.Should().Be(-2);
        v3.Y.Should().Be(-1);
    }

    [Fact]
    public void Multiplication()
    {
        var v1 = new Vector(1, 2);
        var v2 = v1 * 2;

        v2.X.Should().Be(2);
        v2.Y.Should().Be(4);
    }

    [Fact]
    public void Division()
    {
        var v1 = new Vector(2, 4);
        var v2 = v1 / 2;

        v2.X.Should().Be(1);
        v2.Y.Should().Be(2);
    }

    [Fact]
    public void Deconstruct()
    {
        var v = new Vector(1, 2);
        var (x, y) = v;

        x.Should().Be(1);
        y.Should().Be(2);
    }

    [Fact]
    public void ToStringTest()
    {
        var v = new Vector(1, 2);

        v.ToString().Should().Be("(1, 2)");
    }

    [Fact]
    public void ConstantsAreAllValid()
    {
        Vector.Zero.Should().Be(new Vector(0, 0));
        Vector.One.Should().Be(new Vector(1, 1));
        Vector.Up.Should().Be(new Vector(0, -1));
        Vector.Down.Should().Be(new Vector(0, 1));
        Vector.Left.Should().Be(new Vector(-1, 0));
        Vector.Right.Should().Be(new Vector(1, 0));
    }

    [Fact]
    public void Angle()
    {
        var v1 = new Vector(1, 1);
        var v2 = new Vector(1, 5);

        v1.Angle().Should().Be(45 * MathF.PI / 180);
        v1.Angle(v2).Should().Be(90 * MathF.PI / 180);
    }

    [Fact]
    public void Distance()
    {
        var v1 = new Vector(1, 1);
        var v2 = new Vector(1, 5);

        v1.Distance(v2).Should().Be(4);
    }

    [Fact]
    public void Dot()
    {
        var v1 = new Vector(2, 5);
        var v2 = new Vector(5, 5);

        v1.Dot(v2).Should().Be(35);
    }

    [Fact]
    public void Magnitude()
    {
        var v = new Vector(3, 4);

        v.Magnitude.Should().Be(5);
    }

    [Fact]
    public void In()
    {
        var rect = new Rect(0, 0, 10, 10);
        var v1 = new Vector(5, 5);
        var v2 = new Vector(15, 15);
        var v3 = new Vector(-5, -5);
        var v4 = new Vector(9, 9);
        var v5 = new Vector(10, 10);

        v1.In(rect).Should().BeTrue();
        v2.In(rect).Should().BeFalse();
        v3.In(rect).Should().BeFalse();
        v4.In(rect).Should().BeTrue();
        v5.In(rect).Should().BeFalse();
    }

    [Fact]
    public void ToNumerics()
    {
        var v = new Vector(1, 2);
        var n = v.ToNumerics();

        n.X.Should().Be(1);
        n.Y.Should().Be(2);
    }
}