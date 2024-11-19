using System.Collections.Generic;
using System.Drawing;
using Promete.Elements.Renderer.GL.Helper;
using Promete.Graphics;
using Promete.Windowing;

namespace Promete.Elements.Renderer.GL;

public class GLTilemapRenderer(IWindow window, GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var tilemap = (Tilemap)element;
		var mode = tilemap.RenderingMode == TilemapRenderingMode.Auto ? GetPrefferedMode(tilemap) : tilemap.RenderingMode;
		if (mode == TilemapRenderingMode.Scan) ScanAndRender(tilemap);
		else FullRender(tilemap);
	}

	private TilemapRenderingMode GetPrefferedMode(Tilemap tilemap)
	{
		var tileSize = tilemap.TileSize * tilemap.AbsoluteScale;
		// ウィンドウ内に存在し得る最大のタイル数を概算する
		var (ww, wh) = window.Size;
		var maxTilesX = ww / tileSize.X + 2;
		var maxTilesY = wh / tileSize.Y + 2;
		var maxTilesInWindow = maxTilesX * maxTilesY;
		// 存在しうるタイル数より実際のタイル数のほうが多い場合、画面を走査するほうがループ数を減らせる可能性がある
		return maxTilesInWindow < tilemap.Tiles.Count ? TilemapRenderingMode.Scan : TilemapRenderingMode.RenderAll;
	}

	private void ScanAndRender(Tilemap tilemap)
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
		{
			for (var x = tx; x < tx + maxTilesX; x++)
			{
				var offset = (x, y) * tilemap.TileSize;
				var tile = tilemap[x, y];
				if (tile == null) continue;

				helper.Draw(tile.GetTexture(tilemap, (x, y), window), tilemap, tilemap.GetTileColorAt(x, y), offset, tilemap.TileSize.X, tilemap.TileSize.Y);
			}
		}
	}

	private void FullRender(Tilemap tilemap)
	{
		foreach (var (tileLocation, (tile, color)) in tilemap.Tiles)
		{
			var offset = tileLocation * tilemap.TileSize;
			var texture = tile.GetTexture(tilemap, tileLocation, window);

			helper.Draw(texture, tilemap, color, offset, tilemap.TileSize.X, tilemap.TileSize.Y);
		}

		return;

		// カリング
		bool Filter(KeyValuePair<VectorInt, (ITile, Color?)> kv)
		{
			var (left, top) = tilemap.AbsoluteLocation + kv.Key * tilemap.TileSize * tilemap.AbsoluteScale;
			var right = left + tilemap.TileSize.X * tilemap.AbsoluteScale.X;
			var bottom = top + tilemap.TileSize.Y * tilemap.AbsoluteScale.Y;
			return left <= window.ActualWidth && top <= window.ActualHeight && right >= 0 && bottom >= 0;
		}
	}
}
