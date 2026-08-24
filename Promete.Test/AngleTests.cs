using FluentAssertions;

namespace Promete.Test;

public class AngleTests
{
    [Fact]
    public void FromDegrees()
    {
        var angle = Angle.FromDegrees(90);
        angle.ToDegrees().Should().Be(90);
        angle.ToRadians().Should().BeApproximately(MathF.PI / 2, 1e-6f);
    }

    [Fact]
    public void FromRadians()
    {
        var angle = Angle.FromRadians(MathF.PI);
        angle.ToRadians().Should().BeApproximately(MathF.PI, 1e-6f);
        angle.ToDegrees().Should().BeApproximately(180, 1e-4f);
    }

    [Fact]
    public void Zero()
    {
        Angle.Zero.ToDegrees().Should().Be(0);
        Angle.Zero.ToRadians().Should().Be(0);
    }

    [Fact]
    public void Addition()
    {
        var a = Angle.FromDegrees(30);
        var b = Angle.FromDegrees(60);
        (a + b).ToDegrees().Should().Be(90);
    }

    [Fact]
    public void Subtraction()
    {
        var a = Angle.FromDegrees(90);
        var b = Angle.FromDegrees(30);
        (a - b).ToDegrees().Should().Be(60);
    }

    [Fact]
    public void Negation()
    {
        var a = Angle.FromDegrees(45);
        (-a).ToDegrees().Should().Be(-45);
    }

    [Fact]
    public void MultiplyByScalar()
    {
        var a = Angle.FromDegrees(45);
        (a * 2).ToDegrees().Should().Be(90);
        (2 * a).ToDegrees().Should().Be(90);
    }

    [Fact]
    public void DivideByScalar()
    {
        var a = Angle.FromDegrees(90);
        (a / 2).ToDegrees().Should().Be(45);
    }

    [Fact]
    public void Modulo()
    {
        var a = Angle.FromDegrees(450);
        (a % 360f).ToDegrees().Should().Be(90);
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
        angle.ToRadians().Should().BeApproximately(original, 1e-6f);
    }
}
