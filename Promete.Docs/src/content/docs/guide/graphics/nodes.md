---
title: ノードとは？
description: Prometeの描画システムの基本となるノードの概念について解説します。
sidebar:
  order: 3
---

ノードは、Prometeの描画システムの基本単位です。画面に表示されるすべての要素（スプライト、テキスト、図形など）はノードとして表現され、階層構造を形成して管理されます。

## ノードの基本概念

### ノードとは

ノードは`Node`抽象クラスを基底とする描画要素で、以下のような特徴を持ちます：

- **位置、回転、スケールの情報を持つ**
- **個別に表示/非表示を制御できる**
- **描画順序をZIndexで制御できる**

### ノードの種類

Prometeには以下のノードが用意されています：

| ノード | 用途 |
|--------|------|
| `Sprite` | テクスチャ（画像）を表示 |
| `Text` | テキストを表示 |
| `Shape` | 図形（線、矩形、円など）を表示 |
| `Tilemap` | タイルマップを表示 |
| `NineSliceSprite` | 9スライス画像を表示 |
| `Container` | 他のノードをグループ化 |

## ノードの基本プロパティ

すべてのノードは以下の共通プロパティを持ちます：

```csharp title="ノードの基本プロパティ"
// 位置とトランスフォーム
node.Location = (100, 50);      // 位置
node.Scale = (2.0f, 1.5f);      // スケール（倍率）
node.Angle = 45.0f;             // 回転角度（度）
node.Pivot = (0.5f, 0.5f);      // 中心点（0-1の相対座標）

// サイズ
node.Size = (64, 64);           // サイズ
node.Width = 64;                // 幅（Size.Xと同じ）
node.Height = 64;               // 高さ（Size.Yと同じ）

// 表示制御
node.IsVisible = true;          // 表示/非表示
node.ZIndex = 1;                // 描画順序（大きいほど手前）

// その他
node.Name = "PlayerSprite";     // ノード名（デバッグ用）
```

### Setup API

ノードでは、メソッドチェーンを使って複数のプロパティを一度に設定できます：

```csharp title="メソッドチェーンの例"
var sprite = new Sprite(texture)
    .Location(100, 100)
    .Scale(2.0f, 2.0f)
    .Angle(45)
    .Pivot(0.5f, 0.5f)
    .ZIndex(10)
    .Name("RotatedSprite");

Root.Add(sprite);
```

## 階層構造とコンテナ

### 親子関係

[コンテナ](/guide/graphics/container)を用いると、ノードの階層構造を形成できます。子ノードは親ノードの変形（位置、回転、スケール）を継承します：

```csharp title="階層構造の例"
// 親コンテナを作成
var parent = new Container()
    .Location(200, 100)
    .Scale(2.0f)
    .Angle(30);

// 子ノードを追加
var child = new Sprite(texture)
    .Location(50, 0);  // 親からの相対位置

parent.Add(child);
Root.Add(parent);

// 結果: 子ノードは親の変形を継承
// 実際の位置: (200 + 50*2, 100 + 0*2) = (300, 100)
// 実際のスケール: 2.0倍
// 実際の角度: 30度
```

### Rootコンテナ

シーンの`Root`プロパティは特別なコンテナで、画面全体を表します：

```csharp title="Rootの使用例"
public override void OnStart()
{
    // Rootに直接追加
    Root.Add(new Sprite(backgroundTexture));

    // Rootは画面全体なので、(0,0)が左上角
    Root.Add(new Text("Hello, World!")
        .Location(10, 10));
}
```

## 座標系と変形

### 座標系

Prometeでは以下の座標系を使用します：

- **原点**: 左上角が(0, 0)
- **X軸**: 右方向が正
- **Y軸**: 下方向が正
- **角度**: 時計回りが正（0度が右方向）

### ピボット（中心点）

ピボットは回転とスケールの基準点を設定します：

```csharp title="ピボットの設定"
// 左上角を中心にする（デフォルト）
sprite.Pivot = (0, 0);

// 中央を中心にする
sprite.Pivot = (0.5f, 0.5f);

// 右下角を中心にする
sprite.Pivot = (1, 1);

// 回転してみる
sprite.Angle = 45; // ピボット位置を中心に回転
```

### 絶対座標の取得

親子関係を考慮した絶対座標を取得できます：

```csharp title="絶対座標の取得"
// 相対座標（親からの位置）
Vector relativePos = node.Location;

// 絶対座標（画面上の実際の位置）
Vector absolutePos = node.AbsoluteLocation;

// 絶対スケール
Vector absoluteScale = node.AbsoluteScale;

// 絶対角度
float absoluteAngle = node.AbsoluteAngle;
```
