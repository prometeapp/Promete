using Promete.Elements.Renderer.GL.Helper;
using Promete.Windowing;

namespace Promete.Elements.Renderer.GL;

public class GLTilemapRenderer(IWindow window, GLTextureRendererHelper helper) : ElementRendererBase
{
	public override void Render(ElementBase element)
	{
		var tilemap = (Tilemap)element;
		// TODO Render
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
		return maxTilesInWindow < tilemap.TilesCount ? TilemapRenderingMode.Scan : TilemapRenderingMode.RenderAll;
	}
}
