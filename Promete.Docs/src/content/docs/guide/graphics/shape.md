---
title: Shape
description: 基本的な図形（線、矩形、三角形など）を描画するShapeノードの使用方法について解説します。
sidebar:
  order: 7
---

`Shape`は、線、矩形、三角形、ピクセルなどの基本的な図形を描画するためのノードです。デバッグ用の可視化、UI要素、シンプルなゲームオブジェクト、エフェクトなど、様々な用途で使用できます。

## 図形の作成

`Shape`クラスの静的メソッドを使用して各種図形を作成できます：

```csharp title="基本的な図形の作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // ピクセル（点）
        var pixel = Shape.CreatePixel(100, 100, Color.Red);

        // 線
        var line = Shape.CreateLine(50, 50, 150, 100, Color.Blue);

        // 矩形
        var rect = Shape.CreateRect(200, 50, 300, 150, Color.Green);

        // 三角形
        var triangle = Shape.CreateTriangle(400, 50, 450, 150, 500, 100, Color.Yellow);

        Root.AddRange(pixel, line, rect, triangle);
    }
}
```

## 図形の種類

### ピクセル

単一のピクセル（点）を描画します：

```csharp title="ピクセルの作成"
// 座標指定
var pixel1 = Shape.CreatePixel(100, 100, Color.Red);

// Vector指定
var pixel2 = Shape.CreatePixel((150, 150), Color.Blue);

Root.AddRange(pixel1, pixel2);
```

### 線

2点間を結ぶ直線を描画します：

```csharp title="線の作成"
// 基本的な線
var line1 = Shape.CreateLine(0, 0, 100, 100, Color.White);

// 太い線
var line2 = Shape.CreateLine(0, 50, 100, 150, Color.Red, lineWidth: 3);

// Vector型で指定
var start = (50, 200);
var end = (150, 250);
var line3 = Shape.CreateLine(start, end, Color.Green, 2);

Root.AddRange(line1, line2, line3);
```

### 矩形

四角形を描画します。塗りつぶしと枠線の両方を設定できます：

```csharp title="矩形の作成"
// 塗りつぶしのみ
var filledRect = Shape.CreateRect(50, 50, 150, 100, Color.Blue);

// 枠線のみ
var outlineRect = Shape.CreateRect(200, 50, 300, 100, Color.Transparent,
    lineWidth: 2, lineColor: Color.Red);

// 塗りつぶし + 枠線
var combinedRect = Shape.CreateRect(350, 50, 450, 100, Color.Yellow,
    lineWidth: 3, lineColor: Color.Black);

Root.AddRange(filledRect, outlineRect, combinedRect);
```

### 三角形

3つの頂点で構成される三角形を描画します：

```csharp title="三角形の作成"
// 座標指定
var triangle1 = Shape.CreateTriangle(
    100, 150,  // 頂点1
    150, 250,  // 頂点2
    50, 250,   // 頂点3
    Color.Purple
);

// Vector指定 + 枠線
var triangle2 = Shape.CreateTriangle(
    (250, 150),
    (300, 250),
    (200, 250),
    Color.Orange,
    lineWidth: 2,
    lineColor: Color.Black
);

Root.AddRange(triangle1, triangle2);
```

## 注意点
現状、OpenGLバックエンドにおいて `lineWidth` を2以上に設定しても、常に1px幅で描画されます。
