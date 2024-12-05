---
title: シーン
description: シーンを管理する方法を解説します。
sidebar:
  order: 2
  badge:
    text: WIP
    variant: danger
---

シーンは、ゲームの画面を表す単位です。

多くのゲームは、タイトル、フィールド、戦闘シーン、メニュー、エンディングなど、複数のシーンを持ちます。
ゲームが複雑になるほど、シーンの管理が重要になります。

ここでは、シーンの基本的な使い方を説明します。

## シーンの定義
シーンは、`Scene` クラスを継承して定義します。

```csharp title=MainScene.cs
using Promete;

public class MainScene : Scene
{
    public override void OnStart()
    {
        // シーンが開始されたときに呼ばれる処理
    }

    public override void OnUpdate()
    {
        // フレーム更新時に呼ばれる処理
    }

    public override void OnDestroy()
    {
        // シーンが破棄されたときに呼ばれる処理
    }
}
```

シーンには、ライフサイクル メソッドという特定のタイミングで呼び出されるメソッドがあります。

| メソッド名 | 説明 |
| --- | --- |
| `OnStart` | シーンが開始されたときに呼ばれるメソッド |
| `OnUpdate` | フレーム更新時に呼ばれるメソッド |
| `OnDestroy` | シーンが破棄されたときに呼ばれるメソッド |
| `OnPause` | シーンが中断されたときに呼ばれるメソッド |
| `OnResume` | シーンが復帰されたときに呼ばれるメソッド |

たくさんありますが、`OnStart` はシーン開始時、`OnUpdate` はゲームループの記述、`OnDestroy` はシーン終了時によく使われます。

## ゲームループとは
ゲームは通常、1秒間に60回のフレーム更新を行います。
自機キャラを右に1px移動させる処理を `OnUpdate` に記述することで、理論上、1秒間に60px移動することができます。

```csharp title=例
public override void OnUpdate()
{
    // 自機キャラを右に1px移動
    player.Position.X += 1;
}
```

ゲームの作成においては、このようにフレーム更新時に行う処理を記述するのが一般的です。例えば、
1. 入力を受け取る
2. 方向キーが入力されていれば、自機を移動する
3. 敵キャラと自機キャラが衝突していれば、死亡判定を行う
といったロジックを書くことで、方向キーで自機を操作し、敵に当たれば死ぬ、といったゲームを作ることができます。

## PrometeAppおよびウィンドウを取得する
シーン内部では、`App`プロパティ、`Window` プロパティを使えます。これを用いてアプリケーションおよびウィンドウのインスタンスを取得できます。

```csharp
public override void OnStart()
{
    // ウィンドウのタイトルを変更
    Window.Title = "新しいタイトル";
}
```

各種プロパティについては、[PrometeApp](/guide/features/app)、[Window](/guide/features/window)を参照してください。


## シーンの切り替え
シーンを切り替えるには、`PrometeApp.LoadScene<T>` メソッドを使用します。

```cs
App.LoadScene<SubScene>();
```

## ライフサイクルおよびシーン スタック
シーンの詳しいライフサイクルおよび、スタック機能については、[シーンのライフサイクル](/guide/features/scene-lifecycle)を参照してください。
