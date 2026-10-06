---
title: Angle（角度）
description: PrometeのAngle型の基本・生成方法・演算・使用例を解説します。
sidebar:
  order: 3
---

Promete では、回転角度を表すために `Angle` 構造体を使用します。
ノードの `Angle` プロパティや `Vector.Angle()` メソッドなど、角度を扱うすべての場所で使われます。

## Angle型を生成する

`Angle.FromDegrees` `Angle.FromRadians` メソッドで、それぞれ角度とラジアンから生成できます。

`.Degrees` / `.Radians` 拡張プロパティを用いた糖衣構文でも記述でき、こちらのほうが簡潔になります。

```csharp
// 静的メソッド経由
var a1 = Angle.FromDegrees(90);
var a2 = Angle.FromRadians(MathF.PI);

// 糖衣構文
var a3 = 90.Degrees;
var a4 = (MathF.PI / 2).Radians;
```

0度を表す定数 `Angle.Zero` も用意されています。

## 値を取り出す

`ToDegrees()` / `ToRadians()` メソッドで数値として取り出せます。

```csharp
var angle = 90.Degrees;

float deg = angle.ToDegrees();  // 90.0
float rad = angle.ToRadians();  // π/2 ≈ 1.5708
```

## 演算

`Angle` どうしの加減算、スカラーとの乗除算、剰余演算が使えます。

```csharp
var a = (MathF.PI / 2).Radians; // 90度
var b = 45.Degrees; // 45度

Angle sum   = a + b;       // 135度
Angle diff  = a - b;       // 45度
Angle neg   = -a;          // -90度
Angle twice = a * 2.0f;    // 180度
Angle half  = a / 2.0f;    // 45度
Angle wrap  = a % 360f;    // 90度（360度で正規化する場合など）
```

## サンプル：毎フレーム回転させる

```csharp
private float _angle = 0;

public override void OnUpdate()
{
    _angle += Time.DeltaTime * 90; // 1秒で90度回転
    sprite.Angle = _angle.Degrees;
}
```

`float` の変数で角速度を計算し、代入時に `.Degrees` で `Angle` に変換するパターンがよく使われます。

## サンプル：マウスの方向を向く

`Vector.Angle()` は2点間の角度を `Angle` 型で返します。

```csharp
public override void OnUpdate()
{
    Angle direction = player.Location.Angle(mouse.Position);
    player.Angle = direction;
}
```

## ノート

- `Angle` 型への `float` / `int` の暗黙的な変換はありません。`.Degrees` / `.Radians` 拡張プロパティを使ってください
- 内部的には度数法（0〜360°）で値を保持しています。0を超えた値や負の値もそのまま保持されます（自動で正規化はされません）
- 正規化したい場合は `% 360f` 演算子を使います
