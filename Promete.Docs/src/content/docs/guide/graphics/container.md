---
title: Container
description: 複数のノードをグループ化して管理するContainerノードの使用方法について解説します。
sidebar:
  order: 9
---

`Container`は、複数のノードをグループ化して管理するためのノードです。階層構造を形成し、子ノードの変形（位置、回転、スケール）を一括で制御できます。ゲーム内のオブジェクト、UI要素、エフェクトなどの管理に不可欠なノードです。

## コンテナの作成と子ノードの追加

```csharp title="基本的なコンテナの使用"
public class GameScene : Scene
{
    public override void OnStart()
    {
        // コンテナを作成
        var gameObjectGroup = new Container();

        // 子ノードを追加
        var sprite = new Sprite(playerTexture);
        var healthBar = new Text("HP: 100", Font.GetDefault(), Color.Red);

        gameObjectGroup.Add(sprite);
        gameObjectGroup.Add(healthBar);

        // 複数のノードを一度に追加
        gameObjectGroup.AddRange(sprite, healthBar);

        // シーンに追加
        Root.Add(gameObjectGroup);
    }
}
```

## 階層構造と変形の継承

### 親子関係

子ノードは親ノードの変形（位置、回転、スケール）を継承します：

```csharp title="変形の継承"
// 親コンテナ
var parent = new Container()
    .Location(200, 100)
    .Scale(2.0f)
    .Angle(30);

// 子ノード（親からの相対座標で配置）
var child1 = new Sprite(texture)
    .Location(50, 0);    // 親から右に50ピクセル

var child2 = new Sprite(texture)
    .Location(0, 50);    // 親から下に50ピクセル

parent.AddRange(child1, child2);
Root.Add(parent);

// 結果:
// child1の実際の位置: (200 + 50*2, 100 + 0*2) = (300, 100)
// child2の実際の位置: (200 + 0*2, 100 + 50*2) = (200, 200)
// 両方とも2倍のスケールで30度回転
```

### 絶対座標と相対座標

```csharp title="座標系の理解"
var container = new Container().Location(100, 100);
var child = new Sprite(texture).Location(50, 50);

container.Add(child);

// 相対座標（コンテナ内での位置）
var relativePos = child.Location; // (50, 50)

// 絶対座標（画面上の実際の位置）
var absolutePos = child.AbsoluteLocation; // (150, 150)
```

## コレクション操作

### IEnumerableサポート

ContainerはIEnumerableを実装しているため、様々なコレクション操作が可能です。

```csharp title="コレクション操作"
var container = new Container();

// ノードを追加
container.AddRange(sprite1, sprite2, sprite3);

// foreach文で反復処理
foreach (var child in container)
{
    child.IsVisible = true;
}

// LINQを使用
var sprites = container.OfType<Sprite>().ToList();
var visibleNodes = container.Where(n => n.IsVisible).ToList();

// インデックスでアクセス
var firstChild = container[0];
var childCount = container.Count;

// 特定のノードの存在確認
bool hasSprite = container.Contains(sprite1);
```

### ノードの管理

```csharp title="ノード管理の例"
var container = new Container();

// 追加
container.Add(newNode);
container.Insert(0, firstNode); // 先頭に挿入

// 削除
container.Remove(specificNode);
container.RemoveAt(0); // インデックスで削除
container.Clear(); // 全削除

// 検索
var nodeByName = container.FirstOrDefault(n => n.Name == "PlayerSprite");
var allSprites = container.OfType<Sprite>();
```
