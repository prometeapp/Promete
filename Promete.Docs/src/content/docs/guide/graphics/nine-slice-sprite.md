---
title: NineSliceSprite
description: 9スライス方式でリサイズ可能なUI要素を表示するNineSliceSpriteノードの使用方法について解説します。
sidebar:
  order: 8
---

`NineSliceSprite`は、以下のように9スライス方式でテクスチャを表示するノードです。この方式により、角や境界線が不自然に引き伸ばされることなく、画像を任意のサイズに伸縮できます。主にボタンなどのUI部品に使用されます。

```
┌─────┬─────────┬─────┐
│ TL  │   TC    │ TR  │ ← 上部（Top）
├─────┼─────────┼─────┤
│ ML  │   MC    │ MR  │ ← 中央（Middle）
├─────┼─────────┼─────┤
│ BL  │   BC    │ BR  │ ← 下部（Bottom）
└─────┴─────────┴─────┘
  ↑       ↑       ↑
 左部   中央部   右部
(Left) (Center) (Right)
```

- **角（TL, TR, BL, BR）**: そのまま（リサイズしない）
- **水平方向の辺（TC, BC）**: 水平方向のみ拡縮
- **垂直方向の辺（ML, MR）**: 垂直方向のみ拡縮
- **中央（MC）**: 水平・垂直の両方向に拡縮

## 作成
テクスチャを読み込み、NineSliceSpriteノードのコンストラクタに渡します。

```csharp title="9スライステクスチャの作成"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // 9スライステクスチャを読み込み
        // 左16px、上16px、右16px、下16pxをマージンとして指定
        var nineSliceTexture = Window.TextureFactory.Load9Sliced(
            "assets/ui/button.png",
            left: 16,
            top: 16,
            right: 16,
            bottom: 16
        );

        // NineSliceSpriteを作成
        var button = new NineSliceSprite(nineSliceTexture)
            .Location(100, 100)
            .Size(200, 80);

        Root.Add(button);
    }
}
```

## 基本的なプロパティ

### テクスチャ
```csharp title="テクスチャの設定"
var sprite = new NineSliceSprite(defaultTexture);

// テクスチャを変更
sprite.Texture = hoveredTexture;

// 状態に応じたテクスチャ切り替え
sprite.Texture = isPressed ? pressedTexture :
                 isHovered ? hoveredTexture :
                 defaultTexture;
```

### 色調（TintColor）
```csharp title="色調の設定"
// デフォルト（元の色）
sprite.TintColor = Color.White;

// 色付け
sprite.TintColor = Color.LightBlue;

// 半透明
sprite.TintColor = Color.FromArgb(128, 255, 255, 255);
```

### サイズ
```csharp title="サイズの設定"
// 固定サイズ
sprite.Size = (300, 150);

// 幅と高さを個別に設定
sprite.Width = 400;
sprite.Height = 100;

// 動的サイズ変更
sprite.Size = (200 + (int)(50 * Math.Sin(time)), 100);
```

