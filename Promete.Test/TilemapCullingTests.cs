using System.Drawing;
using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Nodes;

namespace Promete.Test;

/// <summary>
/// <see cref="Tilemap"/> の Scan モードによる、画面外タイルのカリングのテスト。
/// </summary>
public class TilemapCullingTests
{
    private static readonly VectorInt TileSize = (16, 16);
    private static readonly VectorInt Viewport = (640, 480);

    [Fact]
    public void Scan_WithoutTransform_ShouldCollectOnlyVisibleTiles()
    {
        var tile = new RecordingTile();
        var map = CreateMap(tile, (0, 0), (39, 29), (100, 100), (-5, 0), (0, -5));

        var collected = CollectLocations(map, map);

        collected.Should().Contain([(0, 0), (39, 29)]);
        collected.Should().NotContain([(100, 100), (-5, 0), (0, -5)]);
    }

    [Fact]
    public void Scan_UnderFlippedParent_ShouldCollectVisibleTiles()
    {
        // FrameBuffer の子コンテナと同じ構成。Y 方向が反転する
        var tile = new RecordingTile();
        var map = CreateMap(tile, (0, 0), (0, 29), (0, -5), (0, 40));
        var root = new Container { Location = (0, Viewport.Y), Scale = (1, -1) };
        root.Add(map);

        var collected = CollectLocations(root, map);

        collected.Should().Contain([(0, 0), (0, 29)]);
        collected.Should().NotContain([(0, -5), (0, 40)]);
    }

    [Fact]
    public void Scan_WithRotation_ShouldCollectTilesInRotatedView()
    {
        // 時計回り 90°: ローカル (x, y) は画面 (-y, x) へ移る
        var tile = new RecordingTile();
        var map = CreateMap(tile, (10, -10), (10, 20), (-40, -10));
        map.Angle = Angle.FromDegrees(90);

        var collected = CollectLocations(map, map);

        collected.Should().Contain([(10, -10)]);
        collected.Should().NotContain([(10, 20), (-40, -10)]);
    }

    [Fact]
    public void Auto_WithLargeMapUnderFlippedParent_ShouldCollectVisibleTilesOnly()
    {
        var tile = new RecordingTile();
        var map = new Tilemap(TileSize);
        map.Fill(0, 0, 100, 100, tile);
        var root = new Container { Location = (0, Viewport.Y), Scale = (1, -1) };
        root.Add(map);

        var collected = CollectLocations(root, map);

        collected.Should().Contain([(5, 0), (5, 29)]);
        collected.Should().NotContain([(5, 60)]);
        collected.Count.Should().BeLessThan(100 * 100);
    }

    [Fact]
    public void Scan_WithSingularMatrix_ShouldCollectNothing()
    {
        var tile = new RecordingTile();
        var map = CreateMap(tile, (0, 0));
        map.Scale = (0, 0);

        var collected = CollectLocations(map, map);

        collected.Should().BeEmpty();
    }

    private static Tilemap CreateMap(ITile tile, params VectorInt[] locations)
    {
        var map = new Tilemap(TileSize, renderingMode: TilemapRenderingMode.Scan);
        foreach (var location in locations)
            map.SetTile(location, tile);
        return map;
    }

    private static List<VectorInt> CollectLocations(Node root, Tilemap map)
    {
        var tile = (RecordingTile)map.Tiles.First().Value.tile;
        tile.Collected.Clear();

        root.BeforeRender();
        map.Collect(new RenderCommandQueue(), new RenderContext { WindowSize = Viewport });
        return [.. tile.Collected];
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
