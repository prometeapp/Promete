using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;

namespace Promete.Nodes;

public class Tilemap(
    VectorInt tileSize,
    Color? defaultColor = null,
    TilemapRenderingMode renderingMode = TilemapRenderingMode.Auto
) : Node
{
    private readonly Dictionary<VectorInt, (ITile tile, Color? color)> _tiles = [];

    private int _minTileX = int.MaxValue;
    private int _maxTileX = int.MinValue;
    private int _minTileY = int.MaxValue;
    private int _maxTileY = int.MinValue;

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

    public override void Collect(RenderCommandQueue queue, RenderContext ctx)
    {
        var mode =
            RenderingMode == TilemapRenderingMode.Auto ? GetPreferredMode(ctx) : RenderingMode;
        if (mode == TilemapRenderingMode.Scan)
            ScanAndCollect(queue, ctx);
        else
            FullCollect(queue);
    }

    private TilemapRenderingMode GetPreferredMode(RenderContext ctx)
    {
        if (_tiles.Count == 0)
            return TilemapRenderingMode.RenderAll;

        // 行列が潰れていて何も映らない場合は、Scan で何も描かない
        if (!TryGetVisibleTileRange(ctx, out var min, out var max))
            return TilemapRenderingMode.Scan;

        // タイルマップの空間的広がり（タイル単位）
        var mapW = _maxTileX - _minTileX + 1;
        var mapH = _maxTileY - _minTileY + 1;

        // ビューポートに収まるタイル数（ScanAndCollect と同じ範囲）
        var viewW = max.X - min.X + 1;
        var viewH = max.Y - min.Y + 1;

        // マップ全体がビューポートに収まる場合のみ RenderAll、それ以外は Scan
        return (mapW <= viewW && mapH <= viewH)
            ? TilemapRenderingMode.RenderAll
            : TilemapRenderingMode.Scan;
    }

    private void ScanAndCollect(RenderCommandQueue queue, RenderContext ctx)
    {
        if (!TryGetVisibleTileRange(ctx, out var min, out var max))
            return;

        // 登録済みタイルの範囲に絞る。極端に縮小された場合でも、ループが膨らまない。
        var x0 = Math.Max(min.X, _minTileX);
        var x1 = Math.Min(max.X, _maxTileX);
        var y0 = Math.Max(min.Y, _minTileY);
        var y1 = Math.Min(max.Y, _maxTileY);

        for (var y = y0; y <= y1; y++)
        for (var x = x0; x <= x1; x++)
        {
            if (!_tiles.TryGetValue((x, y), out var entry))
                continue;

            entry.tile.Collect(queue, this, (x, y), entry.color.GetValueOrDefault(Color.White));
        }
    }

    /// <summary>
    /// 画面に映りうるタイルの範囲（タイル座標、両端を含む）を求めます。
    /// 反転や回転、親の変形があっても求められます。回転時は、画面外のタイルを多少含みます。
    /// </summary>
    /// <returns>行列が逆変換できず、何も映らない場合は <c>false</c>。</returns>
    private bool TryGetVisibleTileRange(RenderContext ctx, out VectorInt min, out VectorInt max)
    {
        min = max = default;

        if (TileSize.X <= 0 || TileSize.Y <= 0 || !Matrix4x4.Invert(ModelMatrix, out var inverse))
            return false;

        // 画面の四隅をローカル座標へ戻し、その外接矩形を求める
        var (ww, wh) = ctx.WindowSize;
        Span<Vector2> corners = [new(0, 0), new(ww, 0), new(0, wh), new(ww, wh)];

        var lo = new Vector2(float.MaxValue);
        var hi = new Vector2(float.MinValue);
        foreach (var corner in corners)
        {
            var p = Vector2.Transform(corner, inverse);
            lo = Vector2.Min(lo, p);
            hi = Vector2.Max(hi, p);
        }

        // 端数やピクセルスナップの誤差のため、上下左右に 1 タイルずつ余白を取る
        min = (ToTileIndex(lo.X, TileSize.X) - 1, ToTileIndex(lo.Y, TileSize.Y) - 1);
        max = (ToTileIndex(hi.X, TileSize.X) + 1, ToTileIndex(hi.Y, TileSize.Y) + 1);
        return true;
    }

    private static int ToTileIndex(float position, int tileSize)
    {
        // int の範囲を超えるとキャストが不定になるため、先に丸める
        return (int)Math.Clamp(MathF.Floor(position / tileSize), -1e9f, 1e9f);
    }

    private void FullCollect(RenderCommandQueue queue)
    {
        foreach (var (tileLocation, (tile, color)) in Tiles)
            tile.Collect(queue, this, tileLocation, color.GetValueOrDefault(Color.White));
    }

    protected override void OnDestroy()
    {
        Clear();
    }

    /// <summary>
    /// 指定した位置のタイルを取得します。
    /// </summary>
    /// <returns></returns>
    public ITile? GetTileAt(VectorInt point)
    {
        return _tiles.TryGetValue(point, out var tile) ? tile.tile : null;
    }

    /// <summary>
    /// 指定した位置のタイルを取得します。
    /// </summary>
    /// <returns></returns>
    public ITile? GetTileAt(int x, int y)
    {
        return GetTileAt((x, y));
    }

    /// <summary>
    /// 指定した位置のタイルの色を取得します。
    /// </summary>
    /// <returns></returns>
    public Color? GetTileColorAt(VectorInt point)
    {
        return _tiles.ContainsKey(point) ? _tiles[point].color : default;
    }

    /// <summary>
    /// 指定した位置のタイルの色を取得します。
    /// </summary>
    /// <returns></returns>
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
        {
            _tiles.Remove(point);

            // バウンディングボックスの縮小は O(n) スキャンが必要なため省略。
            // 保守的に大きめに保つことで Scan が過剰選択されることがあるが常に安全。
        }
        else
        {
            _tiles[point] = (tile, color ?? DefaultColor);
            if (point.X < _minTileX)
                _minTileX = point.X;
            if (point.X > _maxTileX)
                _maxTileX = point.X;
            if (point.Y < _minTileY)
                _minTileY = point.Y;
            if (point.Y > _maxTileY)
                _maxTileY = point.Y;
        }
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
        _minTileX = int.MaxValue;
        _maxTileX = int.MinValue;
        _minTileY = int.MaxValue;
        _maxTileY = int.MinValue;
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
    /// <returns></returns>
    public IEnumerator<(VectorInt loc, ITile tile, Color? color)> GetEnumerator()
    {
        foreach (var t in _tiles)
            yield return (t.Key, t.Value.tile, t.Value.color);
    }
}
