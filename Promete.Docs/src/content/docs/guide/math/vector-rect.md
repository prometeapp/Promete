---
title: VectorとRect
description: PrometeのVector/VectorInt/Rect/RectIntの基本・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

Prometeでは、2D座標や矩形などを扱うために `Vector`（実数）, `VectorInt`（整数）, `Rect`（実数矩形）, `RectInt`（整数矩形）を用意しています。
ゲームやUIの座標計算、当たり判定、移動処理など幅広く利用できます。

## 基本の使い方

```csharp
// 実数ベクトル
var v = new Vector(10.5f, 20.0f);

// 整数ベクトル
var vi = new VectorInt(10, 20);

// 実数矩形
var r = new Rect((10, 20), (100, 50));

// 整数矩形
var ri = new RectInt((10, 20), (100, 50));
```

## 主なAPI

- `v.X`, `v.Y`<br/>座標値
- `v.Magnitude`<br/>長さ
- `v.Normalized`<br/>単位ベクトル
- `Vector.Angle(from, to)`<br/>2点間の角度（ラジアン）
- `Vector.Distance(from, to)`<br/>2点間の距離
- `Vector.Dot(v1, v2)`<br/>内積
- `v.In(rect)`<br/>矩形内判定
- `Rect.Intersect(rect)`<br/>矩形同士の交差判定
- `Rect.Translate(offset)`<br/>矩形の平行移動

## タプルとの相互変換

`Vector`/`VectorInt`/`Rect`/`RectInt` は、タプルとの相互変換（暗黙的キャスト）が可能です。
これにより、タプルから直接ベクトルや矩形を生成したり、逆にベクトルや矩形をタプルとして扱うことができます。

```csharp
// Vectorをタプルから生成
Vector v = (10.5f, 20.0f);

// Vectorの分解
var (vx, vy) = v; // vx, vy: float

// Rectをタプルから生成
Rect r = (10f, 20f, 100f, 50f);

// Rectの分解
var (pos, rect) = r; // pos: Vector, rect: Vector
var (rx, ry, rw, rh) = r; // rx, ry, rw, rh: float
```

## サンプル：当たり判定

```csharp
var player = new Rect((x, y), (w, h));
var enemy = new Rect((ex, ey), (ew, eh));
if (player.Intersect(enemy))
{
    // 当たり判定成立
}
```
