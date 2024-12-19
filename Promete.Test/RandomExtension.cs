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
}
