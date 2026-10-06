# Angle 型

v2 では角度を表す `Angle` 構造体が追加され、角度を扱う API の型が `float` から `Angle` に変わった。`float` / `int` との暗黙的な変換はないため、数値を直接渡すとコンパイルエラーになる。

## Angle を受け取る・返す API

- `Node.Angle`、`Node.AbsoluteAngle`、Setup API の `.Angle(...)`
- `Vector` / `VectorInt` の `.Angle()` メソッド、静的メソッド `Vector.Angle(from, to)` / `VectorInt.Angle(from, to)`
- `Vector.Rotate(...)`

## 生成

拡張プロパティ `.Degrees` / `.Radians` を使う (C# 14 の拡張プロパティなので `LangVersion` 14 以上が必要)。

```csharp
sprite.Angle = 90.Degrees;           // int から度数法
sprite.Angle = 90.0f.Degrees;        // float から度数法
sprite.Angle += 1.Degrees;
sprite.Angle = MathF.PI.Radians;     // float からラジアン

// 静的メソッドでも生成できる
sprite.Angle = Angle.FromDegrees(90);
sprite.Angle = Angle.FromRadians(MathF.PI / 2);
```

`.Degrees` / `.Radians` は `float` と `int` にだけ定義されている。`Math.Atan2` などが返す `double` には `(float)` へのキャストが必要。

```csharp
sprite.Angle = ((float)Math.Atan2(dy, dx)).Radians;
```

`float` で角度を管理しているコードは、代入時に変換する。

```csharp
// v1
float angle = 0;
angle += Window.DeltaTime * 90;
sprite.Angle = angle;

// v2
float angle = 0;
angle += Time.DeltaTime * 90;
sprite.Angle = angle.Degrees;
```

`Angle` 同士の `+` / `-`、`float` との `*` / `/`、`%` の演算子があるので、`float` を経由せずに書くこともできる。

```csharp
sprite.Angle += 90.Degrees * Time.DeltaTime;
```

## float への変換と比較

`ToDegrees()` / `ToRadians()` を使う。`Angle` には大小比較の演算子 (`<` `>` など) がないため、比較するときも `float` に変換する (`==` / `!=` はある)。

```csharp
float deg = sprite.Angle.ToDegrees();
float rad = sprite.Angle.ToRadians();

if (sprite.Angle.ToDegrees() > 180) { /* ... */ }
```

v1 で `MathHelper.ToRadian(sprite.Angle)` / `MathHelper.ToDegree(...)` を使って単位変換していた箇所は、これらのメソッドに置き換える。

## v1 の単位の違いに注意

v1 では API ごとに単位が違っていた。

- `Node.Angle`: 度数法
- `Vector.Angle()` の戻り値: ラジアン
- `Vector.Rotate(float)` の引数: ラジアン

v2 ではすべて `Angle` なので、変換せずにそのまま代入できる。

```csharp
// v1
float angle = player.Location.Angle(mouse.Position); // ラジアン
sprite.Angle = MathHelper.ToDegree(angle);

// v2
Angle angle = player.Location.Angle(mouse.Position);
sprite.Angle = angle;
```

`Vector.Rotate` を書き換えるときは、`.Degrees` ではなく `.Radians` を使う。

```csharp
// v1: v.Rotate(MathF.PI / 2)
v.Rotate((MathF.PI / 2).Radians);
```

v1 の `Vector.Angle()` の戻り値を `float` として計算に使っていた箇所は、その値がラジアン前提だったことを踏まえて `ToRadians()` で取り出す。
