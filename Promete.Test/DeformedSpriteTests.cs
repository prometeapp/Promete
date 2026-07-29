using FluentAssertions;
using Promete.Graphics;
using Promete.Nodes;

namespace Promete.Test;

public class DeformedSpriteTests
{
    [Fact]
    public void Constructor_ShouldCreateTextureSizedQuad()
    {
        var texture = new Texture2D(1, (64, 32), _ => { });
        var sprite = new DeformedSprite(texture);

        sprite.TopLeft.X.Should().Be(0);
        sprite.TopLeft.Y.Should().Be(0);
        sprite.TopRight.X.Should().Be(64);
        sprite.TopRight.Y.Should().Be(0);
        sprite.BottomRight.X.Should().Be(64);
        sprite.BottomRight.Y.Should().Be(32);
        sprite.BottomLeft.X.Should().Be(0);
        sprite.BottomLeft.Y.Should().Be(32);
    }

    [Fact]
    public void Vertices_ShouldBeSetIndependently()
    {
        var sprite = new DeformedSprite
        {
            TopLeft = (10, 20),
            TopRight = (110, 20),
            BottomRight = (100, 120),
            BottomLeft = (0, 100),
        };

        sprite.TopLeft.X.Should().Be(10);
        sprite.TopLeft.Y.Should().Be(20);
        sprite.TopRight.X.Should().Be(110);
        sprite.TopRight.Y.Should().Be(20);
        sprite.BottomRight.X.Should().Be(100);
        sprite.BottomRight.Y.Should().Be(120);
        sprite.BottomLeft.X.Should().Be(0);
        sprite.BottomLeft.Y.Should().Be(100);
    }
}
