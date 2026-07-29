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

        sprite.TopLeft.Should().Be((0, 0));
        sprite.TopRight.Should().Be((64, 0));
        sprite.BottomRight.Should().Be((64, 32));
        sprite.BottomLeft.Should().Be((0, 32));
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

        sprite.TopLeft.Should().Be((10, 20));
        sprite.TopRight.Should().Be((110, 20));
        sprite.BottomRight.Should().Be((100, 120));
        sprite.BottomLeft.Should().Be((0, 100));
    }
}
