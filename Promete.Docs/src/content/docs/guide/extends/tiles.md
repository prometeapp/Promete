---
title: カスタムタイルの自作
description: Prometeで独自のタイル（ITile実装）を作成し、Tilemapで利用する方法を解説します。
sidebar:
  order: 3
---

PrometeのTilemapでは、標準のTile以外にも独自のタイル（`ITile`実装）を利用できます。
ここではカスタムタイルの作り方とTilemapでの利用例を解説します。

## テクスチャを描画するタイル

`ITile` を実装し、`GetTexture` で描画するテクスチャを返します。
`Destroy` はタイルが不要になったときに呼ばれるので、保持しているリソースを破棄します。

```csharp
using Promete;
using Promete.Graphics;
using Promete.Nodes;

public class MyTile(Texture2D texture) : ITile
{
    public Texture2D GetTexture(Tilemap map, VectorInt tileLocation) => texture;

    public void Destroy() { }
}
```

`GetTexture` には、描画対象の `Tilemap` とタイル座標が渡されます。
座標に応じて返すテクスチャを変えれば、隣接タイルに合わせた見た目の切り替えなどができます。

## テクスチャ以外を描画するタイル `v2.3~`

`ITile.Collect` をオーバーライドすると、タイル自身が描画コマンドをキューに積めます。
図形など、テクスチャ以外のものを描画したいときに使います。

この場合、`GetTexture` の実装は不要です。

```csharp
using System.Drawing;
using Promete;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

public class BoxTile : ITile
{
    public void Collect(RenderCommandQueue queue, Tilemap map, VectorInt tileLocation, Color tint)
    {
        var origin = (Vector)(tileLocation * map.TileSize);
        var size = (Vector)map.TileSize;

        queue.Enqueue(new DrawPrimitiveCommand
        {
            WorldVertices =
            [
                RenderingHelper.Transform(origin, map),
                RenderingHelper.Transform(origin + (size.X, 0), map),
                RenderingHelper.Transform(origin + size, map),
                RenderingHelper.Transform(origin + (0, size.Y), map),
            ],
            ShapeType = ShapeType.Polygon,
            Color = tint,
        });
    }

    public void Destroy() { }
}
```

`Collect` の既定実装は、`GetTexture` が返すテクスチャを描画するコマンドを積みます。
そのため、`v2.2` 以前から使っているタイルは、書き換えなしでそのまま動きます。

:::note
`Collect` をオーバーライドしたタイルで `GetTexture` を呼ぶと、`NotSupportedException` になります。
:::

## Tilemapでの利用

```csharp
var tilemap = new Tilemap((16, 16));
tilemap.SetTile(1, 1, new MyTile(texture));
tilemap.SetTile(2, 1, new BoxTile(), Color.Red);
```

## ノート

- カスタムタイルはTilemapの`SetTile`で自由に配置できます
- `SetTile` の色引数は、`Collect` の `tint` として渡されます
- 標準のTileクラスを継承して拡張することも可能です
