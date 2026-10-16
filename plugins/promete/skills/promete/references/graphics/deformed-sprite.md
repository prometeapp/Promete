---
title: DeformedSprite
description: 4つの頂点を自由に指定してテクスチャを変形して描画する、DeformedSpriteノードの使用方法について解説します。
sidebar:
  order: 5
---

`DeformedSprite` (v2.4~) は、4つの頂点を自由に指定して、テクスチャを変形させて描画するノードです。台形にして奥行きを出したり、頂点を動かして揺れやゆがみを表現したりできます。

## 作成

テクスチャと、テクスチャの **左上・右上・左下・右下** に対応する4つの頂点を渡して作成します。

```csharp title="DeformedSpriteの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        var texture = App.TextureFactory.Load("assets/player.png");

        // テクスチャ、左上、右上、左下、右下
        var ds = new DeformedSprite(texture, (0, 0), (32, 4), (4, 48), (32, 64))
            .Location(100, 100);

        Root.Add(ds);
    }
}
```

頂点は、ノードの `Location` を原点とした相対座標です。上の例では、左上の頂点が画面の (100, 100) に描画されます。

頂点を省略すると、テクスチャと同じ大きさの長方形になります。

```csharp
var ds = new DeformedSprite(texture);
```

## 頂点の変更

頂点は、作成した後でもいつでも変更できます。

```csharp title="頂点の変更"
// 1つずつ変更する
ds.TopLeft = (10, -5);
ds.BottomRight = (40, 70);

// 4つまとめて変更する
ds.SetVertices((0, 0), (32, 0), (0, 32), (32, 32));
```

毎フレーム頂点を書き換えれば、アニメーションになります。

```csharp title="右上の頂点を揺らす"
public override void OnUpdate()
{
    var offset = MathF.Sin(Time.TotalTime * 4) * 8;
    _ds.TopRight = (32 + offset, 0);
}
```

## Size と Pivot

`DeformedSprite` の `Size` と `Pivot` は、頂点から自動で計算されます。値を代入しても無視されます。

- **Size**: 4つの頂点をちょうど囲む長方形の大きさ (端数は切り上げ)
- **Pivot**: その長方形の中で、頂点の原点 (`Location`) がどこにあるか

| 頂点の原点の位置 | Pivot |
|------------------|-------|
| 長方形の左上と同じ | `(0, 0)` |
| 長方形の内側 (例: 中心) | 正の値 (例: `(0.5, 0.5)`) |
| 長方形の左上より外側 | 負の値 |

```csharp title="原点を中心に置く"
// 原点が長方形の中心にあるので、Size は (64, 64)、Pivot は (0.5, 0.5)
var ds = new DeformedSprite(texture, (-32, -32), (32, -32), (-32, 32), (32, 32));
```

`Pivot` がこのように決まるので、回転や拡大縮小は常に頂点の原点を中心に行われます。回転させたいときは、回転の中心にしたい点を原点として頂点を指定してください。

`Size` は頂点を動かすたびに更新されるため、[コンテナ](./container)の `AutoSize` にも反映されます。

## テクスチャの貼られ方

4つの頂点が凸四角形 (すべての内角が180°未満) をなすときは、**射影補間**でテクスチャが貼られます。台形にすると、平らな板を斜めから見たように、奥行きのある見た目になります。

頂点が凹四角形になる場合や、辺どうしが交差する場合は、四角形を2つの三角形に分けて、それぞれにテクスチャを貼ります (アフィン補間)。このとき、分割した対角線のところでテクスチャが折れて見えることがあります。

:::tip
きれいに表示したい場合は、頂点が凸四角形になる範囲で動かしてください。
:::

## その他のプロパティ

`TintColor` で色調を変更できます。スプライトシートのセル (`LoadSpriteSheet` で読み込んだテクスチャ) も、そのセルの範囲だけが変形して描画されます。

```csharp
var cells = App.TextureFactory.LoadSpriteSheet("assets/icons.png", 3, 1, (32, 32));
var ds = new DeformedSprite(cells[1], (0, 0), (64, 8), (0, 64), (64, 56), Color.Cyan);
```

`Container` の子や `FrameBuffer` の子としても使えます。親の位置・回転・拡大縮小が反映されます。

## カスタムシェーダー

`Material` を設定すると、カスタムシェーダーで描画できます。シェーダーには次の値が渡されます。

- 頂点属性 `location = 0`: `vec2` 頂点のワールド座標
- 頂点属性 `location = 1`: `vec3` 射影補間用の `(u * q, v * q, q)`。フラグメントシェーダーで `xy / z` を計算すると UV になります
- `uProjection`: 投影行列 (`uModel` は単位行列)
- `uTexture0`: テクスチャ
- `uTintColor`: 色調

詳しくは[シェーダーとマテリアル](./shader)を参照してください。

## サンプルプログラム

`Promete.Example` の `graphics/deformedSprite.demo` で、頂点がランダムに動く様子を確認できます。
