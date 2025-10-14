---
title: PrometeApp
description: Prometeアプリケーションのコアクラスの使い方と機能
sidebar:
  order: 0
---

`PrometeApp`は、Prometeアプリケーションの中心となるクラスです。アプリケーションの初期化、プラグインの管理、シーンの制御などを担当します。

## アプリケーションの作成

`PrometeApp`は`PrometeApp.Create()`メソッドを使用してビルダーパターンで作成します。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop();
```

### ビルダーパターンの流れ

1. **Create()** - ビルダーインスタンスを作成
2. **Use&lt;T&gt;()** - プラグインを追加（必要な数だけチェーン）
3. **BuildWith...()** - プラットフォーム固有のバックエンドでビルド

## アプリケーションの実行

アプリケーションをビルドした後、`Run<T>()`メソッドでシーンを指定して実行します。

```csharp
// 基本的な実行
return app.Run<MainScene>();

// ウィンドウオプション付きで実行
return app.Run<MainScene>(WindowOptions.Default with
{
    Title = "My Game",
    Size = (800, 600),
    Mode = WindowMode.Resizable,
});
```

`Run`メソッドは、アプリケーションが終了するまでブロックし、終了ステータスコード（int）を返します。

## プラグインの管理

### プラグインの追加

アプリケーション構築時に`Use<T>()`メソッドでプラグインを追加します。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()           // キーボード入力
    .Use<Mouse>()             // マウス入力
    .Use<Gamepads>()          // ゲームパッド入力
    .Use<ConsoleLayer>()      // デバッグ用テキスト表示
    .Use<CoroutineManager>()  // コルーチン管理
    .Use<AudioPlayer>()       // オーディオ再生
    .BuildWithOpenGLDesktop();
```

### プラグインの取得

実行時にプラグインのインスタンスを取得できます。

```csharp
// プラグインを取得（存在しない場合は例外）
var keyboard = app.GetPlugin<Keyboard>();

// プラグインの取得を試行（安全）
if (app.TryGetPlugin<Mouse>(out var mouse))
{
    // マウスプラグインが利用可能
    var position = mouse.Position;
}
```

## シーン管理

### シーンの切り替え

アプリケーション実行中にシーンを切り替えることができます。

```csharp
public class GameScene : Scene
{
    private readonly Keyboard _keyboard;

    public GameScene(Keyboard keyboard)
    {
        _keyboard = keyboard;
    }

    public override void OnUpdate()
    {
        if (_keyboard.Escape.IsKeyDown)
        {
            // メニューシーンに切り替え
            App.LoadScene<MenuScene>();
        }
    }
}
```

### シーンスタック

シーンをスタック形式で管理することも可能です。

```csharp
// 現在のシーンの上にポーズメニューを表示
App.PushScene<PauseMenuScene>();

// 前のシーンに戻る
App.PopScene();
```

## グローバル描画レイヤー

ここに追加したノードは、すべてのシーンの前面または背面に描画され、シーンの切り替えによって削除されません。

Fpsカウンターや、ゲームを通じて常に表示するUIなどに便利ですが、乱用しないよう効果的に使用してください。

```csharp
// 背景レイヤー（すべてのシーンの背面に描画）
app.GlobalBackground.Add(new Sprite(backgroundTexture));

// 前景レイヤー（すべてのシーンの前面に描画）
app.GlobalForeground.Add(new Text("FPS: 60").Location(10, 10));
```

## アプリケーションの設定

### 背景色の設定

```csharp
app.BackgroundColor = Color.DarkBlue;
```

### アプリケーションの終了

```csharp
// 正常終了
app.Exit();

// エラーコード付きで終了
app.Exit(1);
```

## スレッドセーフティ

Prometeは基本的にシングルスレッドで動作します。メインスレッド以外からPrometeのAPIを呼び出す場合は、`NextFrame`メソッドを使用してメインスレッドに処理を移譲します。

指定した関数は次のフレームの描画前にエンジンによって呼び出されます。よって、次のフレームに処理を遅らせるためにも使用できます。

```csharp
// 別スレッドからの処理をメインスレッドで実行
Task.Run(() =>
{
    // バックグラウンド処理
    var result = HeavyCalculation();

    // メインスレッドで結果を反映
    app.NextFrame(() =>
    {
        updateText.Content = $"Result: {result}";
    });
});
```

## 現在のアプリケーションインスタンス

静的プロパティを使用して、現在実行中のアプリケーションインスタンスにアクセスできます。

```csharp
var currentApp = PrometeApp.Current;
```

:::caution[シングルインスタンス]
`PrometeApp`は同時に1つのインスタンスしか実行できません。複数のインスタンスを作成しようとすると例外が発生します。
:::

## シーンの自動登録

`PrometeApp`は、エントリアセンブリ内のすべての`Scene`派生クラスを自動的に検出し、DIコンテナに登録します。

```csharp
// これらのクラスは自動的に登録される
public class MainScene : Scene { }
public class GameScene : Scene { }
public class MenuScene : Scene { }

// IgnoredSceneAttributeを使用して登録を除外
[IgnoredScene]
public class DebugScene : Scene { }
```
