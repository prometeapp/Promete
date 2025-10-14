---
title: プラグインシステム
description: Prometeのプラグインシステムと依存性注入の仕組み
sidebar:
  order: 2
---

Prometeは**DIコンテナをベースとしたプラグインシステム**を採用しています。このシステムにより、必要な機能のみを選択してゲームに組み込むことができ、環境の差異に対応したり、不要な機能を削減して最適化を図ることができます。

## プラグインの登録

アプリケーション初期化時に、`Use<T>()`メソッドでプラグインを登録します。

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

## プラグインの取得

### コンストラクタ注入

シーンおよびプラグインのコンストラクタでは、登録されたプラグインを引数として受け取ることができます。DIコンテナが自動的に解決してインスタンスを注入します。

**C# 12のプライマリコンストラクタ**を活用すると、簡潔な記法でプラグインを利用できます。

### 基本的な使用方法

```csharp
// プラグインを受け取るシーンクラス
public class MainScene(Keyboard keyboard, Mouse mouse, AudioPlayer audio) : Scene
{
    private readonly IAudioSource _clickSfx = new WaveAudioSource("assets/click.wav");

    public override void OnStart()
    {
        // keyboard, mouse, audio が自動的に利用可能
        if (keyboard.Enter.IsKeyDown)
        {
            audio.PlayAsync(_clickSfx);
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

### メソッドを用いた取得（安全）

実行時にプラグインのインスタンスを取得することもできます。

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

### メソッドを用いた取得（例外スローあり）
`TryGetPlugin`ではなく、存在しない場合に例外をスローする`GetPlugin`も利用できます。

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
