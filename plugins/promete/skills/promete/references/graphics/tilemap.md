---
title: Tilemap
description: タイルベースの2Dマップを効率的に表示するTilemapノードの使用方法について解説します。
sidebar:
  order: 6
---

`Tilemap`は、タイルベースの2Dマップを効率的に表示するためのノードです。グリッド状にタイルを配置してマップを作成する機能を提供します。

## 作成

タイルのサイズを指定して生成します。
```csharp title="基本的なタイルマップの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // 16x16ピクセルのタイルサイズでタイルマップを作成
        var tilemap = new Tilemap(tileSize: (16, 16));

        // テクスチャからタイルを作成
        var grassTexture = App.TextureFactory.Load("assets/grass.png");
        var grassTile = new Tile(grassTexture);

        // タイルを配置
        tilemap.SetTile(0, 0, grassTile);
        tilemap.SetTile(1, 0, grassTile);
        tilemap.SetTile(0, 1, grassTile);

        Root.Add(tilemap);
    }
}
```

## インデクサーを使った、タイルの取得/設定

```csharp title="インデクサーを使った操作"
var tilemap = new Tilemap((16, 16));
var tile = new Tile(texture);

// タイルの設定
tilemap[0, 0] = tile;
tilemap[1, 1] = tile;

// Vector型でも指定可能
tilemap[(2, 2)] = tile;

// タイルの取得
var tileAt = tilemap[0, 0];

// タイルの削除
tilemap[0, 0] = null;
```

### 線・矩形の描画

直線・矩形状にタイルを設置するメソッドがあります。

```csharp title="線の描画"
var tilemap = new Tilemap((16, 16));
var wallTile = new Tile(wallTexture);

// 線の描画
tilemap.Line(0, 0, 10, 10, wallTile);

// 矩形の描画
tilemap.Fill(5, 5, 15, 10, wallTile);
```

## タイルの種類

### 基本的なタイル

```csharp title="基本的なタイルの作成"
// 単一テクスチャのタイル
var texture = App.TextureFactory.Load("assets/stone.png");
var stoneTile = new Tile(texture);

// タイルを配置
tilemap.SetTile(x, y, stoneTile);
```

### アニメーションタイル

```csharp title="アニメーションタイルの作成"
// アニメーション用テクスチャを読み込み
var frame1 = App.TextureFactory.Load("assets/water_1.png");
var frame2 = App.TextureFactory.Load("assets/water_2.png");
var frame3 = App.TextureFactory.Load("assets/water_3.png");

// アニメーションタイルを作成（0.5秒間隔でアニメーション）
var waterTile = new Tile([frame1, frame2, frame3], 0.5f);

// アニメーションタイルを配置
tilemap.SetTile(x, y, waterTile);
```

### 色付きタイル

タイルに色を重ねて表示できます：

```csharp title="タイルの色付け"
var baseTile = new Tile(texture);

// 赤みがかったタイル
tilemap.SetTile(x, y, baseTile, Color.Red);

// 半透明のタイル
tilemap.SetTile(x, y, baseTile, Color.FromArgb(128, 255, 255, 255));

// デフォルト色を設定
tilemap.DefaultColor = Color.LightBlue;
tilemap.SetTile(x, y, baseTile); // デフォルト色が適用される
```

## タイルマップのプロパティ

### タイルサイズ

```csharp title="タイルサイズの設定"
// 作成時に指定
var tilemap = new Tilemap(tileSize: (32, 32));

// 後から変更
tilemap.TileSize = (16, 16);
```

### レンダリングモード

タイルマップの描画方法を制御できます。この設定は基本的に弄る必要がありません。

:::tip[`auto`を設定した場合の挙動]
登録済みのタイル数が、画面に映る範囲のマス数以下の場合、 `RenderAll`が選択されます。逆に多い場合は`Scan`が選択されます。 `v2.3.1~`

タイルがまばらで広い範囲に散らばっているマップでも、タイル数で判断するため、無駄に `Scan` が選ばれることはありません。
`RenderAll` が選ばれた場合は、画面外のタイルも描画されます。
:::

```csharp title="レンダリングモードの設定"
// 自動最適化（デフォルト）
tilemap.RenderingMode = TilemapRenderingMode.Auto;

// 全タイルを常に描画
tilemap.RenderingMode = TilemapRenderingMode.RenderAll;

// 画面内のタイルのみ描画
tilemap.RenderingMode = TilemapRenderingMode.Scan;
```

`Scan` は、タイルマップや親ノードが回転・反転 (負のスケール) していても、画面内のタイルを正しく判定します `v2.3.1~`。
`FrameBuffer` の中に置いた場合も同様です。回転している場合は、画面外のタイルを多少含めて描画します。

:::note
`v2.3` 以前は、回転や反転を考慮していませんでした。
回転・反転したタイルマップで、タイルが描画されなかったり、画面端で欠けたりすることがありました。
:::
