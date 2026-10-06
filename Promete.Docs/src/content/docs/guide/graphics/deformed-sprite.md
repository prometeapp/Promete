---
title: DeformedSprite
description: 4頂点を自由に設定してテクスチャを変形描画するDeformedSpriteノードの使用方法について解説します。
sidebar:
  order: 6
---

`DeformedSprite`は、テクスチャを四角形の4頂点へ貼り付けて描画するノードです。台形や斜めに変形した画像を表示する場合に使用できます。

## 作成

テクスチャを読み込み、4頂点を設定します。頂点の座標はすべて親要素からの相対座標です。

```csharp title="基本的なDeformedSpriteの作成"
var texture = Window.TextureFactory.Load("assets/image.png");
var sprite = new DeformedSprite(texture)
{
    TopLeft = (0, 0),
    TopRight = (160, 20),
    BottomRight = (160, 120),
    BottomLeft = (0, 100),
};

Root.Add(sprite);
```

頂点は左上、右上、右下、左下の順に設定します。テクスチャはこの4頂点に対応するUV座標で描画されます。

## プロパティ

### テクスチャ

`Texture`プロパティで表示するテクスチャを変更できます。`null`を設定すると描画されません。

### 色調

`TintColor`プロパティでテクスチャに色を重ねられます。

```csharp
sprite.TintColor = Color.FromArgb(200, 255, 200, 100);
```

### 親要素からの相対座標

`Container`へ追加した場合、頂点は親の位置・拡大率・回転の影響を受けます。

```csharp
var container = new Container().Location(100, 50);
container.Add(sprite);
Root.Add(container);
```
