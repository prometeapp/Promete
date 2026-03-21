using FluentAssertions;

namespace Promete.Test;

public class AngleTests
{
    [Fact]
    public void FromDegrees()
    {
        var angle = Angle.FromDegrees(90);
        angle.Degrees.Should().Be(90);
        angle.Radians.Should().BeApproximately(MathF.PI / 2, 1e-6f);
    }

    [Fact]
    public void FromRadians()
    {
        var angle = Angle.FromRadians(MathF.PI);
        angle.Radians.Should().BeApproximately(MathF.PI, 1e-6f);
        angle.Degrees.Should().BeApproximately(180, 1e-4f);
    }

    [Fact]
    public void Zero()
    {
        Angle.Zero.Degrees.Should().Be(0);
        Angle.Zero.Radians.Should().Be(0);
    }

    [Fact]
    public void ImplicitConversionFromFloat()
    {
        Angle angle = 45f;
        angle.Degrees.Should().Be(45);
    }

    [Fact]
    public void ImplicitConversionToFloat()
    {
        var angle = Angle.FromDegrees(90);
        float value = angle;
        value.Should().Be(90);
    }

    [Fact]
    public void Addition()
    {
        var a = Angle.FromDegrees(30);
        var b = Angle.FromDegrees(60);
        (a + b).Degrees.Should().Be(90);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Angle.FromDegrees(90);
        var b = Angle.FromDegrees(30);
        (a - b).Degrees.Should().Be(60);
    }

    [Fact]
    public void Negation()
    {
        var a = Angle.FromDegrees(45);
        (-a).Degrees.Should().Be(-45);
    }

    [Fact]
    public void MultiplyByScalar()
    {
        var a = Angle.FromDegrees(45);
        (a * 2).Degrees.Should().Be(90);
        (2 * a).Degrees.Should().Be(90);
    }

    [Fact]
    public void DivideByScalar()
    {
        var a = Angle.FromDegrees(90);
        (a / 2).Degrees.Should().Be(45);
    }

    [Fact]
    public void Modulo()
    {
        var a = Angle.FromDegrees(450);
        (a % 360f).Degrees.Should().Be(90);
    }

    [Fact]
    public void Equality()
    {
        var a = Angle.FromDegrees(90);
        var b = Angle.FromDegrees(90);
        var c = Angle.FromDegrees(45);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals((object)b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCodeConsistency()
    {
        var a = Angle.FromDegrees(90);
        var b = Angle.FromDegrees(90);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToStringFormat()
    {
        var a = Angle.FromDegrees(45);
        a.ToString().Should().Be("45°");
    }

    [Fact]
    public void RadiansRoundTrip()
    {
        var original = MathF.PI / 3;
        var angle = Angle.FromRadians(original);
        angle.Radians.Should().BeApproximately(original, 1e-6f);
    }
}
