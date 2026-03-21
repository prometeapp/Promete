using System;
using System.Collections.Generic;
using System.Drawing;
using Promete.Graphics;
using Promete.Nodes.Renderer;
using Promete.Nodes.Renderer.Commands;

namespace Promete.Nodes;

public class Tilemap(
    VectorInt tileSize,
    Color? defaultColor = default,
    TilemapRenderingMode renderingMode = TilemapRenderingMode.Auto) : Node
{
    private readonly Dictionary<VectorInt, (ITile tile, Color? color)> _tiles = [];

    /// <summary>
    /// グリッドのサイズを取得または設定します。
    /// </summary>
    public VectorInt TileSize { get; set; } = tileSize;

    /// <summary>
    /// タイルのデフォルト色を取得または設定します。
    /// </summary>
    public Color? DefaultColor { get; set; } = defaultColor;

    /// <summary>
    /// タイルマップのレンダリングモードを取得または設定します。
    /// </summary>
    public TilemapRenderingMode RenderingMode { get; set; } = renderingMode;

    /// <summary>
    /// タイル情報を辞書形式で取得します。
    /// </summary>
    public IReadOnlyDictionary<VectorInt, (ITile tile, Color? color)> Tiles => _tiles.AsReadOnly();

    /// <summary>
    /// 指定した位置のタイルを取得または設定します。
    /// </summary>
    public ITile? this[int x, int y]
    {
        get => GetTileAt(x, y);
        set => SetTile(x, y, value);
    }

    /// <summary>
    /// 指定した位置のタイルを取得または設定します。
    /// </summary>
    public ITile? this[VectorInt point]
    {
        get => GetTileAt(point);
        set => SetTile(point, value);
    }

    internal override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        var mode = RenderingMode == TilemapRenderingMode.Auto
            ? GetPreferredMode(ctx)
            : RenderingMode;
        if (mode == TilemapRenderingMode.Scan)
            ScanAndCollect(queue, ctx);
        else
            FullCollect(queue);
    }

    private TilemapRenderingMode GetPreferredMode(RenderContext ctx)
    {
        var tileSize = TileSize * AbsoluteScale;
        var (ww, wh) = ctx.WindowSize;
        var maxTilesX = ww / tileSize.X + 2;
        var maxTilesY = wh / tileSize.Y + 2;
        var maxTilesInWindow = maxTilesX * maxTilesY;
        return maxTilesInWindow < Tiles.Count ? TilemapRenderingMode.Scan : TilemapRenderingMode.RenderAll;
    }

    private void ScanAndCollect(RenderCommandQueue queue, RenderContext ctx)
    {
        var tileSize = TileSize * AbsoluteScale;
        var (ww, wh) = ctx.WindowSize;
        var maxTilesX = ww / tileSize.X + 2;
        var maxTilesY = wh / tileSize.Y + 2;

        var tl = -AbsoluteLocation / tileSize;
        if (tl.X < 0) tl.X--;
        if (tl.Y < 0) tl.Y--;
        var (tx, ty) = (VectorInt)tl;

        var window = PrometeApp.Current.Window;
        for (var y = ty; y < ty + maxTilesY; y++)
        for (var x = tx; x < tx + maxTilesX; x++)
        {
            var offset = (x, y) * TileSize;
            var tile = this[x, y];
            if (tile == null) continue;

            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tile.GetTexture(this, (x, y), window),
                ModelMatrix = ModelMatrix,
                TintColor = GetTileColorAt(x, y).GetValueOrDefault(Color.White),
                Width = TileSize.X,
                Height = TileSize.Y,
                Pivot = offset,
            });
        }
    }

    private void FullCollect(RenderCommandQueue queue)
    {
        var window = PrometeApp.Current.Window;
        foreach (var (tileLocation, (tile, color)) in Tiles)
        {
            var offset = tileLocation * TileSize;
            queue.Enqueue(new DrawTextureCommand
            {
                Texture = tile.GetTexture(this, tileLocation, window),
                ModelMatrix = ModelMatrix,
                TintColor = color.GetValueOrDefault(Color.White),
                Width = TileSize.X,
                Height = TileSize.Y,
                Pivot = offset,
            });
        }
    }

    protected override void OnDestroy()
    {
        Clear();
    }

    /// <summary>
    /// 指定した位置のタイルを取得します。
    /// </summary>
    public ITile? GetTileAt(VectorInt point)
    {
        return _tiles.ContainsKey(point) ? _tiles[point].tile : default;
    }

    /// <summary>
    /// 指定した位置のタイルを取得します。
    /// </summary>
    public ITile? GetTileAt(int x, int y)
    {
        return GetTileAt((x, y));
    }

    /// <summary>
    /// 指定した位置のタイルの色を取得します。
    /// </summary>
    public Color? GetTileColorAt(VectorInt point)
    {
        return _tiles.ContainsKey(point) ? _tiles[point].color : default;
    }

    /// <summary>
    /// 指定した位置のタイルの色を取得します。
    /// </summary>
    public Color? GetTileColorAt(int x, int y)
    {
        return GetTileColorAt((x, y));
    }

    /// <summary>
    /// 指定した位置にタイルを設定します。
    /// </summary>
    public void SetTile(VectorInt point, ITile? tile, Color? color = null)
    {
        if (tile == null)
            _tiles.Remove(point);
        else
            _tiles[point] = (tile, color ?? DefaultColor);
    }

    /// <summary>
    /// 指定した位置にタイルを設定します。
    /// </summary>
    public void SetTile(int x, int y, ITile? tile, Color? color = null)
    {
        SetTile((x, y), tile, color);
    }

    /// <summary>
    /// 全てのタイルを削除します。
    /// </summary>
    public void Clear()
    {
        _tiles.Clear();
    }

    /// <summary>
    /// 指定したタイルで線を描画します。
    /// </summary>
    public void Line(int x1, int y1, int x2, int y2, ITile tile)
    {
        var steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
        // 左上から右下に描くよう正規化する
        if (steep)
        {
            (x1, y1) = (y1, x1);
            (x2, y2) = (y2, x2);
        }

        if (x1 > x2)
        {
            (x1, x2) = (x2, x1);
            (y1, y2) = (y2, y1);
        }

        var deltaX = x2 - x1;
        var deltaY = Math.Abs(y2 - y1);
        var error = deltaX / 2;
        int ystep;
        var y = y1;
        if (y1 < y2)
            ystep = 1;
        else
            ystep = -1;

        for (var x = x1; x <= x2; x++)
        {
            if (steep)
                this[y, x] = tile;
            else
                this[x, y] = tile;

            error -= deltaY;
            if (error < 0)
            {
                y += ystep;
                error += deltaX;
            }
        }
    }

    /// <summary>
    /// 指定した長方形を指定したタイルで塗りつぶします。
    /// </summary>
    public void Fill(int x1, int y1, int width, int height, ITile tile)
    {
        for (var y = y1; y < y1 + height; y++)
        for (var x = x1; x < x1 + width; x++)
            this[x, y] = tile;
    }

    /// <summary>
    /// 指定したタイルで線を描画します。
    /// </summary>
    public void Line(VectorInt start, VectorInt end, ITile tile)
    {
        Line(start.X, start.Y, end.X, end.Y, tile);
    }

    /// <summary>
    /// 指定した長方形を指定したタイルで塗りつぶします。
    /// </summary>
    public void Fill(VectorInt position, VectorInt size, ITile tile)
    {
        Fill(position.X, position.Y, size.X, size.Y, tile);
    }

    /// <summary>
    /// タイルマップの列挙子を取得します。
    /// </summary>
    public IEnumerator<(VectorInt loc, ITile tile, Color? color)> GetEnumerator()
    {
        foreach (var t in _tiles)
            yield return (t.Key, t.Value.tile, t.Value.color);
    }
}
