---
title: カスタムタイルの自作
description: Prometeで独自のタイル（ITile実装）を作成し、Tilemapで利用する方法を解説します。
sidebar:
  order: 3
---

PrometeのTilemapでは、標準のTile以外にも独自のタイル（`ITile`実装）を利用できます。
ここではカスタムタイルの作り方とTilemapでの利用例を解説します。

タイルは `ITile` インターフェースを実装して作成します。
必要に応じて描画や当たり判定などのプロパティ・メソッドを追加できます。

```csharp
using Promete.Graphics;

public class MyTile : ITile
{
    public void Draw(Tilemap tilemap, int x, int y)
    {
        // タイルの描画処理
    }

    // 必要に応じてプロパティやメソッドを追加
}
```

## 主なAPI

- `Draw(Tilemap tilemap, int x, int y)`<br/>タイルの描画処理を記述

## サンプル：Tilemapでの利用

```csharp
var tilemap = new Tilemap(10, 10);
tilemap.SetTile(1, 1, new MyTile());
```

## 注意点

- カスタムタイルはTilemapの`SetTile`で自由に配置できます
- 必要に応じて状態や見た目を動的に変更できます
- 標準のTileクラスを継承して拡張することも可能です
