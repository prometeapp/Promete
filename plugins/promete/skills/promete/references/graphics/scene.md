---
title: シーン
description: Prometeにおけるシーンの概念と使い方を解説します。
sidebar:
  order: 1
---

シーンは、ゲームの画面を表す単位です。多くのゲームは、タイトル画面、ゲーム画面、設定画面、エンディングなど、複数のシーンを持ちます。

Prometeでは、`Scene`抽象クラスを継承してシーンを作成します。各シーンには独自のライフサイクルがあり、適切なタイミングで初期化、更新、破棄が行われます。

## シーンの定義

`Scene`クラスを継承してシーンを定義します。

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

## ライフサイクルメソッド

シーンには特定のタイミングで呼び出されるメソッド（ライフサイクルメソッド）があります。

| メソッド名 | 呼び出しタイミング | 用途 |
|------------|-------------------|------|
| `OnStart()` | シーンが開始されたとき | 初期化処理、リソース読み込み |
| `OnUpdate()` | フレーム毎 | ゲームロジック、入力処理 |
| `OnDestroy()` | シーンが破棄されるとき | リソース解放、クリーンアップ |
| `OnPause()` | シーンが一時停止されるとき | 一時停止時の処理 |
| `OnResume()` | シーンが再開されるとき | 再開時の処理 |

## シーンでの依存性注入

[プラグインシステム](/guide/manual/plugin-system)でも触れていますが、登録したプラグインはコンストラクタ経由でシーンに注入されます。

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

シーンには`Root`プロパティがあり、このコンテナにノードを追加することで画面に要素を表示できます。

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
        _texture = App.TextureFactory.Load("assets/player.png");

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

`App.LoadScene<T>()` で他のシーンに切り替えます。

```csharp
// メインシーンに切り替え
App.LoadScene<MainScene>();

// ゲームシーンに切り替え
App.LoadScene<GameScene>();
```

### シーンスタックの利用

シーンスタック機能を使うと、開いたシーンを閉じて元のシーンに戻る、という操作を実現できます。

`PushScene<T>()`で新しいシーンをスタックに積み、`PopScene()`で現在のシーンを閉じて前のシーンに戻ります。

```csharp
// 現在のシーンを保持したまま設定画面を開く
App.PushScene<SettingsScene>();

// 設定画面を閉じて元のシーンに戻る
App.PopScene();
```

## 別のプロジェクトにあるシーンを使う

Prometeは、起動するプロジェクトにあるシーンを自動で登録します。ゲームを複数のプロジェクトに分けて、起動するプロジェクトとは別のプロジェクト (ライブラリ) にシーンを置いた場合は、そのままでは登録されません。

`UseScenesFrom` で、シーンのあるプロジェクトを指定してください。型引数には、そのプロジェクトにある型を1つ渡します。指定したプロジェクトのシーンは、すべて登録されます。

```csharp title="Program.cs"
var app = PrometeApp.Create()
    // ContentLib プロジェクトのシーンを登録する
    .UseScenesFrom<ContentLib.TitleScene>()
    .BuildWithOpenGLDesktop();

app.Run<ContentLib.TitleScene>();
```

`UseScenesFrom(typeof(TitleScene).Assembly)` のように、アセンブリを直接渡すこともできます。

シーンのあるプロジェクトが複数ある場合は、`UseScenesFrom` をプロジェクトの数だけ呼びます。

## 読み込めないシーンの警告 `v2.3~`

読み込もうとしたシーンが登録されていないと、実行時にエラーになります。よくある原因はビルド時に警告として表示されるので、見かけたら次のように直してください。

- **`PROMETE0005`**: 読み込もうとしたシーンが、自動登録されない形になっています。メッセージに理由が書かれています。
  - `[IgnoredScene]` が付いている、抽象クラスである、公開コンストラクタが無い、`private` なネスト型である、などが該当します。
- **`PROMETE0003`**: 別のプロジェクトにあるシーンを読み込もうとしていますが、そのプロジェクトが `UseScenesFrom` で指定されていません。[別のプロジェクトにあるシーンを使う](#別のプロジェクトにあるシーンを使う)を参考に、`UseScenesFrom` を追加してください。
- **`PROMETE0004`**: 参照しているプロジェクトにシーンがありますが、`UseScenesFrom` で指定されていません。これは警告ではなく情報メッセージです。そのプロジェクトのシーンを使わないなら、無視してかまいません。

## App・View・Timeへのアクセス

シーン内部では、`App`・`View`・`Time` の3つのプロパティでそれぞれのインスタンスにアクセスできます。

| プロパティ | 型 | 用途 |
|---|---|---|
| `App` | `PrometeApp` | シーン遷移、テクスチャファクトリーなど |
| `View` | `IGameView` | ウィンドウのサイズ・タイトル・表示状態 |
| `Time` | `ITimeProvider` | フレーム時間・FPS・タイムスケール |

```csharp
public override void OnStart()
{
    // ウィンドウタイトルを変更
    View.Title = "ゲーム画面";

    // ウィンドウサイズを取得
    var size = View.Size;
    console.Print($"ウィンドウサイズ: {size.X} x {size.Y}");
}

public override void OnUpdate()
{
    // デルタタイムを取得
    var dt = Time.DeltaTime;
}
```
