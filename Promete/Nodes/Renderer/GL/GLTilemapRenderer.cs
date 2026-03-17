using Promete.Nodes.Renderer.Commands;
using Promete.Windowing;

namespace Promete.Nodes.Renderer.GL;

public class GLTilemapRenderer(IWindow window) : NodeRendererBase
{
    public override void Collect(Node node, RenderCommandQueue queue)
    {
        var tilemap = (Tilemap)node;
        var mode = tilemap.RenderingMode == TilemapRenderingMode.Auto
            ? GetPrefferedMode(tilemap)
            : tilemap.RenderingMode;
        if (mode == TilemapRenderingMode.Scan) ScanAndCollect(tilemap, queue);
        else FullCollect(tilemap, queue);
    }

    private TilemapRenderingMode GetPrefferedMode(Tilemap tilemap)
    {
        var tileSize = tilemap.TileSize * tilemap.AbsoluteScale;
        var (ww, wh) = window.Size;
        var maxTilesX = ww / tileSize.X + 2;
        var maxTilesY = wh / tileSize.Y + 2;
        var maxTilesInWindow = maxTilesX * maxTilesY;
        return maxTilesInWindow < tilemap.Tiles.Count ? TilemapRenderingMode.Scan : TilemapRenderingMode.RenderAll;
    }

    private void ScanAndCollect(Tilemap tilemap, RenderCommandQueue queue)
    {
        var tileSize = tilemap.TileSize * tilemap.AbsoluteScale;
        var (ww, wh) = window.Size;
        var maxTilesX = ww / tileSize.X + 2;
        var maxTilesY = wh / tileSize.Y + 2;

        var tl = -tilemap.AbsoluteLocation / tileSize;
        if (tl.X < 0) tl.X--;
        if (tl.Y < 0) tl.Y--;
        var (tx, ty) = (VectorInt)tl;

        for (var y = ty; y < ty + maxTilesY; y++)
        for (var x = tx; x < tx + maxTilesX; x++)
        {
            var offset = (x, y) * tilemap.TileSize;
            var tile = tilemap[x, y];
            if (tile == null) continue;

            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tile.GetTexture(tilemap, (x, y), window),
                ModelMatrix = tilemap.ModelMatrix,
                TintColor = tilemap.GetTileColorAt(x, y).GetValueOrDefault(System.Drawing.Color.White),
                Width = tilemap.TileSize.X,
                Height = tilemap.TileSize.Y,
                Pivot = offset,
            });
        }
    }

    private void FullCollect(Tilemap tilemap, RenderCommandQueue queue)
    {
        foreach (var (tileLocation, (tile, color)) in tilemap.Tiles)
        {
            var offset = tileLocation * tilemap.TileSize;
            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tile.GetTexture(tilemap, tileLocation, window),
                ModelMatrix = tilemap.ModelMatrix,
                TintColor = color.GetValueOrDefault(System.Drawing.Color.White),
                Width = tilemap.TileSize.X,
                Height = tilemap.TileSize.Y,
                Pivot = offset,
            });
        }
    }
}
