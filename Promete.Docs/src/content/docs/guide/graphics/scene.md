---
title: シーン
description: Prometeにおけるシーンの概念と使い方を解説します。
sidebar:
  order: 1
---

シーンは、ゲームの画面を表す単位です。多くのゲームは、タイトル画面、ゲーム画面、設定画面、エンディングなど、複数のシーンを持ちます。

Prometeでは、`Scene`抽象クラスを継承してシーンを作成します。各シーンには独自のライフサイクルがあり、適切なタイミングで初期化、更新、破棄が行われます。

## シーンの基本構造

### シーンの定義

シーンを作成するには、`Scene`クラスを継承します：

```csharp title="GameScene.cs"
using Promete;
using Promete.Input;

public class GameScene(Keyboard keyboard) : Scene
{
    public override void OnStart()
    {
        // シーンが開始されたときの処理
        Console.WriteLine("ゲームシーンが開始されました");
    }

    public override void OnUpdate()
    {
        // フレーム毎の更新処理
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }

    public override void OnDestroy()
    {
        // シーンが破棄されるときの処理
        Console.WriteLine("ゲームシーンが終了されました");
    }
}
```

### ライフサイクルメソッド

シーンには以下のライフサイクルメソッドがあります：

| メソッド名 | 呼び出しタイミング | 用途 |
|------------|-------------------|------|
| `OnStart()` | シーンが開始されたとき | 初期化処理、リソース読み込み |
| `OnUpdate()` | フレーム毎 | ゲームロジック、入力処理 |
| `OnDestroy()` | シーンが破棄されるとき | リソース解放、クリーンアップ |
| `OnPause()` | シーンが一時停止されるとき | 一時停止時の処理 |
| `OnResume()` | シーンが再開されるとき | 再開時の処理 |

## シーンでの依存性注入

Prometeでは、シーンのコンストラクタで依存性注入を利用できます：

```csharp title="ExampleScene.cs"
using Promete;
using Promete.Audio;
using Promete.Input;

public class ExampleScene(
    Keyboard keyboard,
    Mouse mouse,
    ConsoleLayer console,
    AudioPlayer audio) : Scene
{
    public override void OnStart()
    {
        console.Print("シーンが開始されました");
        // 各種プラグインやサービスを利用可能
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyDown)
        {
            console.Print("スペースキーが押されました");
        }

        if (mouse[MouseButtonType.Left].IsButtonDown)
        {
            console.Print($"マウス位置: {mouse.Position}");
        }
    }
}
```

## ノードの管理

シーンには`Root`プロパティがあり、このコンテナにノードを追加することで画面に要素を表示できます：

```csharp title="SpriteScene.cs"
using Promete;
using Promete.Graphics;
using Promete.Nodes;

public class SpriteScene : Scene
{
    private Texture2D _texture;
    private Sprite _sprite;

    public override void OnStart()
    {
        // テクスチャを読み込み
        _texture = Window.TextureFactory.Load("assets/player.png");

        // スプライトを作成
        _sprite = new Sprite(_texture)
            .Location(100, 100)
            .Scale(2.0f);

        // ルートコンテナに追加
        Root.Add(_sprite);
    }

    public override void OnDestroy()
    {
        // リソースを適切に解放
        _texture?.Dispose();
        _sprite?.Destroy();
    }
}
```

## シーンの切り替え

### 基本的な切り替え

`App.LoadScene<T>()`を使用してシーンを切り替えます：

```csharp
// メインシーンに切り替え
App.LoadScene<MainScene>();

// ゲームシーンに切り替え
App.LoadScene<GameScene>();
```

### シーンスタックの利用

一時的に他のシーンを表示したい場合は、プッシュ/ポップを使用します：

```csharp
// 現在のシーンを保持したまま設定画面を開く
App.PushScene<SettingsScene>();

// 設定画面を閉じて元のシーンに戻る
App.PopScene();
```

## AppとWindowへのアクセス

シーン内部では、`App`プロパティと`Window`プロパティを使用してアプリケーションとウィンドウにアクセスできます：

```csharp
public override void OnStart()
{
    // ウィンドウタイトルを変更
    Window.Title = "ゲーム画面";

    // ウィンドウサイズを取得
    var size = Window.Size;
    console.Print($"ウィンドウサイズ: {size.X} x {size.Y}");
}
```
