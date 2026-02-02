using System.Drawing;
using FluentAssertions;

namespace Promete.Test;

public class RandomExtension
{
    [Fact]
    public void NextColor()
    {
        var random = new Random(30);

        var color = random.NextColor();
        color.Should().Be(Color.FromArgb(255, 102, 158, 188));
    }

    [Fact]
    public void NextVector()
    {
        var random = new Random(30);

        var vector = random.NextVector(100, 100);
        vector.Should().Be(new Vector(39, 61));
    }

    [Fact]
    public void NextVectorInt()
    {
        var random = new Random(30);

        var vector = random.NextVectorInt(100, 100);
        vector.Should().Be(new VectorInt(39, 61));
    }

    [Fact]
    public void NextVectorFloat()
    {
        var random = new Random(30);

        var vector = random.NextVectorFloat();
        vector.Should().Be(new Vector(0.3990027f, 0.6198839f));
    }

    [Fact]
    public void NextVectorWithVectorParam()
    {
        var random = new Random(30);
        var max = new Vector(100, 100);

        var vector = random.NextVector(max);
        vector.Should().Be(new Vector(39, 61));
    }

    [Fact]
    public void NextVectorIntWithVectorIntParam()
    {
        var random = new Random(30);
        var max = new VectorInt(100, 100);

        var vector = random.NextVectorInt(max);
        vector.Should().Be(new VectorInt(39, 61));
    }

    [Fact]
    public void NextVectorFloatWithVectorParam()
    {
        var random = new Random(30);
        var max = new Vector(100, 100);

        var vector = random.NextVectorFloat(max);
        vector.X.Should().BeApproximately(39.90027f, 0.001f);
        vector.Y.Should().BeApproximately(61.98839f, 0.001f);
    }

    [Fact]
    public void NextVectorWithFractionalMax()
    {
        var random = new Random(30);
        var max = new Vector(100.9f, 100.9f);

        var vector = random.NextVector(max);
        vector.Should().Be(new Vector(39, 61));
    }

    [Fact]
    public void NextVectorFloatPreservesFractionalMax()
    {
        var random = new Random(30);
        var max = new Vector(50.5f, 50.5f);

        var vector = random.NextVectorFloat(max);
        vector.X.Should().BeApproximately(20.14964f, 0.001f);
        vector.Y.Should().BeApproximately(31.30414f, 0.001f);
    }
}
