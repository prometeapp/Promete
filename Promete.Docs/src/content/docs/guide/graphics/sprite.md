---
title: Sprite
description: テクスチャ（画像）を表示するSpriteノードの使用方法について解説します。
sidebar:
  order: 4
---

`Sprite`は、テクスチャ（画像）を画面に表示するための基本的なノードです。2Dゲームにおける最も重要な描画要素の一つで、キャラクター、背景、UI要素など、さまざまな画像を表示するために使用されます。

## 基本的な使用方法

### スプライトの作成

スプライトを作成するには、まずテクスチャを読み込み、それを`Sprite`コンストラクタに渡します：

```csharp title="基本的なスプライトの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // テクスチャを読み込み
        var texture = Window.TextureFactory.Load("assets/player.png");

        // スプライトを作成
        var sprite = new Sprite(texture);

        // シーンに追加
        Root.Add(sprite);
    }
}
```

## スプライトのプロパティ

### テクスチャ

スプライトに表示するテクスチャを設定・変更できます：

```csharp title="テクスチャの設定と変更"
var sprite = new Sprite();

// 後からテクスチャを設定
sprite.Texture = texture1;

// テクスチャを変更（アニメーションなどで使用）
sprite.Texture = texture2;

// テクスチャをクリア
sprite.Texture = null;
```

### 色調（TintColor）

スプライトに色を重ねて表示できます：

```csharp title="色調の設定"
// 白色（デフォルト、元の色をそのまま表示）
sprite.TintColor = Color.White;

// 赤みがかった色合い
sprite.TintColor = Color.Red;

// 半透明
sprite.TintColor = Color.FromArgb(128, 255, 255, 255);

// カスタムカラー
sprite.TintColor = Color.FromArgb(255, 200, 100, 50);
```

### サイズ

スプライトのサイズは、テクスチャのサイズをデフォルトとして使用されますが、カスタムサイズも設定できます：

```csharp title="サイズの制御"
// デフォルト: テクスチャのサイズを使用
var sprite = new Sprite(texture);

// カスタムサイズを設定
sprite.Size = (64, 64);

// 幅と高さを個別に設定
sprite.Width = 128;
sprite.Height = 96;

// テクスチャのサイズに戻す
sprite.ResetSize();
```

### Z順序と描画順序

スプライトの描画順序は`ZIndex`プロパティで制御できます：

```csharp title="描画順序の制御"
// 背景スプライト（奥）
var background = new Sprite(bgTexture)
    .ZIndex(0);

// キャラクタースプライト（中間）
var character = new Sprite(playerTexture)
    .ZIndex(10);

// UIスプライト（手前）
var ui = new Sprite(uiTexture)
    .ZIndex(100);

Root.AddRange(background, character, ui);
```
