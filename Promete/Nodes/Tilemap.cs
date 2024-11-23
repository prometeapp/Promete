using System;
using System.Collections.Generic;
using System.Drawing;
using Promete.Graphics;

namespace Promete.Nodes;

public class Tilemap(VectorInt tileSize, Color? defaultColor = default, TilemapRenderingMode renderingMode = TilemapRenderingMode.Auto) : Node
{
	/// <summary>
	/// Get or set size of grid.
	/// </summary>
	public VectorInt TileSize { get; set; } = tileSize;

	/// <summary>
	/// Get or set default tint color of tiles.
	/// </summary>
	public Color? DefaultColor { get; set; } = defaultColor;

	public TilemapRenderingMode RenderingMode { get; set; } = renderingMode;

	public IReadOnlyDictionary<VectorInt, (ITile tile, Color? color)> Tiles => _tiles.AsReadOnly();

	private readonly Dictionary<VectorInt, (ITile tile, Color? color)> _tiles = new();

	/// <summary>
	/// Get or set the tile at the specified position.
	/// </summary>
	public ITile? this[int x, int y]
	{
		get => GetTileAt(x, y);
		set => SetTile(x, y, value);
	}

	/// <summary>
	/// Get or set the tile at the specified position.
	/// </summary>
	public ITile? this[VectorInt point]
	{
		get => GetTileAt(point);
		set => SetTile(point, value);
	}

	protected override void OnDestroy()
	{
		Clear();
	}

	/// <summary>
	///  Get the tile at the specified position.
	/// </summary>
	public ITile? GetTileAt(VectorInt point) => _tiles.ContainsKey(point) ? _tiles[point].tile : default;

	/// <summary>
	///  Get the tile at the specified position.
	/// </summary>
	public ITile? GetTileAt(int x, int y) => GetTileAt((x, y));

	/// <summary>
	/// Get color of the tile at the specified position.
	/// </summary>
	public Color? GetTileColorAt(VectorInt point) => _tiles.ContainsKey(point) ? _tiles[point].color : default;

	/// <summary>
	/// Get color of the tile at the specified position.
	/// </summary>
	public Color? GetTileColorAt(int x, int y) => GetTileColorAt((x, y));

	/// <summary>
	/// Set the tile at the specified position.
	/// </summary>
	public void SetTile(VectorInt point, ITile? tile, Color? color = null)
	{
		if (tile == null)
			_tiles.Remove(point);
		else
			_tiles[point] = (tile, color ?? DefaultColor);
	}

	/// <summary>
	/// Set the tile at the specified position.
	/// </summary>
	public void SetTile(int x, int y, ITile? tile, Color? color = null) => SetTile((x, y), tile, color);

	/// <summary>
	/// Remove all tiles.
	/// </summary>
	public void Clear()
	{
		_tiles.Clear();
	}

	/// <summary>
	/// Draw a line with specified tile.
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
	/// Fill the specified rectangle with the specified tile.
	/// </summary>
	public void Fill(int x1, int y1, int width, int height, ITile tile)
	{
		for (var y = y1; y < y1 + height; y++)
		for (var x = x1; x < x1 + width; x++)
			this[x, y] = tile;
	}

	/// <summary>
	/// Draw a line with specified tile.
	/// </summary>
	public void Line(VectorInt start, VectorInt end, ITile tile)
		=> Line(start.X, start.Y, end.X, end.Y, tile);

	/// <summary>
	/// Fill the specified rectangle with the specified tile.
	/// </summary>
	public void Fill(VectorInt position, VectorInt size, ITile tile)
		=> Fill(position.X, position.Y, size.X, size.Y, tile);

	public IEnumerator<(VectorInt loc, ITile tile, Color? color)> GetEnumerator()
	{
		foreach (var t in _tiles)
			yield return (t.Key, t.Value.tile, t.Value.color);
	}
}
