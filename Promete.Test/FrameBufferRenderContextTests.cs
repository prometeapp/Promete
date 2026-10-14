using System.Drawing;
using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Nodes;

namespace Promete.Test;

/// <summary>
/// <see cref="FrameBuffer"/> への描画時に使われる <see cref="RenderContext"/> のテスト。
/// </summary>
public class FrameBufferRenderContextTests
{
    private static readonly VectorInt TileSize = (16, 16);

    [Fact]
    public void CreateRenderContext_ShouldUseFrameBufferSize()
    {
        var ctx = FrameBuffer.CreateRenderContext((320, 240));

        ctx.WindowSize.Should().Be(new VectorInt(320, 240));
        ctx.WindowScale.Should().Be(1f);
        ctx.ActualWidth.Should().Be(320);
        ctx.ActualHeight.Should().Be(240);
    }

    [Fact]
    public void Tilemap_CulledWithFrameBufferContext_ShouldFollowFrameBufferSize()
    {
        var tile = new RecordingTile();
        var map = new Tilemap(TileSize, renderingMode: TilemapRenderingMode.Scan);
        map.SetTile((5, 5), tile);
        map.SetTile((30, 5), tile);
        map.BeforeRender();

        map.Collect(new RenderCommandQueue(), FrameBuffer.CreateRenderContext((160, 120)));

        tile.Collected.Should().Contain(new VectorInt(5, 5));
        tile.Collected.Should().NotContain(new VectorInt(30, 5));
    }

    private sealed class RecordingTile : ITile
    {
        public List<VectorInt> Collected { get; } = [];

        public void Collect(
            RenderCommandQueue queue,
            Tilemap map,
            VectorInt tileLocation,
            Color tint
        )
        {
            Collected.Add(tileLocation);
        }

        public void Destroy() { }
    }
}
