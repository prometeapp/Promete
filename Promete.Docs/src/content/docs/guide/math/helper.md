---
title: 数学ヘルパー
description: PrometeのMathHelperによる便利な数学関数・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 2
---

`MathHelper` は、ゲーム開発でよく使う補間・イージング・角度変換などの便利な数学関数をまとめた静的クラスです。

## 基本の使い方

```csharp
// 線形補間
float v = MathHelper.Lerp(0.5f, 0, 100); // 50

// イージング
float e = MathHelper.EaseInOut(0.3f, 0, 1);

// 角度変換
float rad = MathHelper.ToRadian(90); // π/2
float deg = MathHelper.ToDegree(MathF.PI); // 180
```

## 主なAPI

- `MathHelper.Lerp(time, start, end)`<br/>線形補間
- `MathHelper.EaseInOut(time, start, end)`<br/>加減速イージング
- `MathHelper.EaseIn(time, start, end)`<br/>加速イージング
- `MathHelper.EaseOut(time, start, end)`<br/>減速イージング
- `MathHelper.Lerp(time, Vector, Vector)`<br/>ベクトルの線形補間
- `MathHelper.EaseInOut(time, Vector, Vector)`<br/>ベクトルの加減速イージング
- `MathHelper.EaseIn(time, Vector, Vector)`<br/>ベクトルの加速イージング
- `MathHelper.EaseOut(time, Vector, Vector)`<br/>ベクトルの減速イージング
- `MathHelper.ToRadian(degree)`<br/>度→ラジアン変換
- `MathHelper.ToDegree(radian)`<br/>ラジアン→度変換

## サンプル：オブジェクトの移動

```csharp
// 0.0～1.0のtimeで座標を補間
var pos = MathHelper.Lerp(time, startPos, endPos);
```

## 注意点

- timeは0.0～1.0の範囲で指定します
- ベクトル補間は各成分ごとに計算されます
- 角度変換は度・ラジアンの相互変換に使えます
