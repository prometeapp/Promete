---
title: プラグインシステム
description: Prometeのプラグインシステムと依存性注入の仕組み
sidebar:
  order: 2
---

Prometeは**DIコンテナを用いたプラグインシステム**を採用しています。このシステムにより、必要な機能のみを選択してゲームに組み込むことができ、環境の差異に対応したり、不要な機能を削減して最適化を図ることができます。

## プラグインシステムの基本概念

### 目的と利点

プラグインシステムは以下の利点を提供します：

- **モジュラー設計**: 必要な機能のみを選択して追加
- **環境差異の吸収**: PC、モバイル、Webなど異なるプラットフォームに対応
- **パフォーマンス最適化**: 不要な機能を除外して軽量化
- **拡張性**: 独自のプラグインを簡単に作成
- **テスタビリティ**: DIによる疎結合で単体テストが容易

### DIコンテナとの統合

PrometeはDIコンテナを採用しており、各シーンに必要に応じて機能を注入できます。

不要な機能を除外することで、ゲームのパフォーマンスやソースコードの可読性を向上させることができます。

```csharp
// プラグインの登録
var app = PrometeApp.Create()
    .Use<Keyboard>()        // キーボード入力プラグイン
    .Use<ConsoleLayer>()    // デバッグコンソールプラグイン
    .BuildWithOpenGLDesktop();

// シーンでのプラグイン使用（コンストラクタインジェクション）
public class GameScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    // keyboard と console が自動的に注入される
}
```

## プラグインの登録

### 基本的な登録方法

`Use<T>()`メソッドでプラグインを登録します。

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()           // キーボード入力
    .Use<Mouse>()             // マウス入力
    .Use<Gamepads>()          // ゲームパッド入力
    .Use<ConsoleLayer>()      // デバッグコンソール
    .Use<CoroutineManager>()  // コルーチン管理
    .Use<AudioPlayer>()       // オーディオ再生
    .BuildWithOpenGLDesktop();
```

### インターフェースと実装の分離

インターフェースと具体的な実装を分けて登録することもできます。

例えば、クロスプラットフォーム展開するゲームにおいて、特定のプラットフォームに依存しないインターフェースを定義し、各プラットフォームごとに実装を提供することが可能です。

```csharp
var app = PrometeApp.Create()
    .Use<IMyService, MyServiceImpl>()  // インターフェースと実装を指定
    .BuildWithOpenGLDesktop();
```

## プライマリコンストラクタの活用

Prometeでは**C# 12のプライマリコンストラクタ**を活用したシンプルな依存性注入を推奨しています。

### 基本的な使用方法

```csharp
// プラグインを受け取るシーンクラス
public class MainScene(Keyboard keyboard, Mouse mouse, AudioPlayer audio) : Scene
{
    public override void OnStart()
    {
        // keyboard, mouse, audio が自動的に利用可能
        if (keyboard.Enter.IsKeyDown)
        {
            audio.PlayAsync("assets/click.wav");
        }
    }

    public override void OnUpdate()
    {
        // マウス位置の表示
        var mousePos = mouse.Position;

        // キーボード入力の処理
        if (keyboard.Escape.IsKeyDown)
        {
            App.Exit();
        }
    }
}
```

## プラグインの取得

実行時にプラグインのインスタンスを取得することもできます。

### 安全な取得

```csharp
public class DynamicScene : Scene
{
    public override void OnStart()
    {
        // プラグインの存在確認と取得
        if (App.TryGetPlugin<Keyboard>(out var keyboard))
        {
            Console.WriteLine("キーボードプラグインが利用可能");
            // keyboard を使用した処理
        }

        if (App.TryGetPlugin<Mouse>(out var mouse))
        {
            Console.WriteLine("マウスプラグインが利用可能");
            // mouse を使用した処理
        }
    }
}
```

### 直接取得

```csharp
public class DirectAccessScene : Scene
{
    public override void OnStart()
    {
        // プラグインの直接取得（存在しない場合は例外）
        try
        {
            var audio = App.GetPlugin<AudioPlayer>();
            var src = new WaveAudioSource("assets/welcome.wav");
            audio.PlayOneShot(src);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("AudioPlayerプラグインが登録されていません");
        }
    }
}
```

## カスタムプラグインの作成

独自のプラグインを作成することもできます。機能の集約や、シーンをまたいで共有するデータマネージャーなどは、プラグイン化がおすすめです。

```csharp
// ゲーム設定を管理するプラグイン
public class GameSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsFullScreen { get; set; } = false;
    public string PlayerName { get; set; } = "Player";

    public void SaveToFile(string path)
    {
        // 設定をファイルに保存
    }

    public void LoadFromFile(string path)
    {
        // ファイルから設定を読み込み
    }
}

// プラグインの登録
var app = PrometeApp.Create()
    .Use<GameSettings>()
    .Use<Keyboard>()
    .BuildWithOpenGLDesktop();

// シーンでの使用
public class SettingsScene(GameSettings settings, Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        if (keyboard.F11.IsKeyDown)
        {
            settings.IsFullScreen = !settings.IsFullScreen;
            Window.IsFullScreen = settings.IsFullScreen;
        }
    }
}
```
