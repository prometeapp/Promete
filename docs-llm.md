# Promete ゲームエンジン ドキュメント (LLM向け)

このドキュメントは、PrometeゲームエンジンのLLM向け包括的リファレンスです。

## 目次
1. [概要](#概要)
2. [クイックスタート](#クイックスタート)
3. [コアシステム](#コアシステム)
4. [ノードシステム](#ノードシステム)
5. [グラフィックス](#グラフィックス)
6. [入力システム](#入力システム)
7. [オーディオシステム](#オーディオシステム)
8. [テキストシステム](#テキストシステム)
9. [数学ユーティリティ](#数学ユーティリティ)
10. [その他の機能](#その他の機能)
11. [拡張性](#拡張性)
12. [APIリファレンス](#apiリファレンス)

---

## 概要

### Prometeとは

**Promete**は、.NET 8以降向けの2Dゲームエンジンです。名前はギリシャ神話の火の神「プロメテウス」に由来します。

#### 主な特徴

1. **シンプル**: 直感的なAPIで素早くゲーム開発を開始できます
2. **2D特化**: 2Dグラフィックスとゲーム開発に最適化
3. **クロスプラットフォーム**: Windows、macOS、Linux、モバイルをサポート
4. **拡張可能**: プラグインシステムによる柔軟な機能拡張
5. **DIベース**: Microsoft.Extensions.DependencyInjectionベースの設計

#### ライセンス
MIT License

#### 必要環境
- .NET 8.0以降
- OpenGLサポート（デスクトップ版）

---

## クイックスタート

### インストール

```bash
dotnet add package Promete
```

### 最小構成のゲーム

```csharp
using Promete;
using Promete.Windowing;
using Promete.GLDesktop;

// アプリケーションの作成
var app = PrometeApp.Create()
    .BuildWithOpenGLDesktop();

// メインシーンの実行
return app.Run<MainScene>();

// シーン定義
public class MainScene : Scene
{
    public override void OnStart()
    {
        // 初期化処理
    }

    public override void OnUpdate()
    {
        // 毎フレーム実行される処理
    }
}
```

### シーンなしでの起動

v1.1.0以降、シーンを指定せずにPrometeを起動できます：

```csharp
using Promete;
using Promete.GLDesktop;

var app = PrometeApp.Create()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop();

// シーンなしで起動
return app.Run();
```

### Hello World (ConsoleLayerを使用)

```csharp
using Promete;
using Promete.Windowing;
using Promete.GLDesktop;

var app = PrometeApp.Create()
    .Use<ConsoleLayer>()  // コンソール機能を有効化
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>();

public class MainScene(ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        console.Print("Hello, Promete!");
        console.Print("Press [ESC] to exit");
    }
}
```

---

## コアシステム

### PrometeApp

`PrometeApp`は、Prometeアプリケーションのエントリーポイントであり、シングルトンとして機能します。

#### 主なプロパティとメソッド

```csharp
// シングルトンインスタンスへのアクセス
PrometeApp app = PrometeApp.Current;

// ウィンドウへのアクセス
IWindow window = app.Window;

// グローバルコンテナ（シーン切り替えで破棄されない）
Container globalBg = app.GlobalBackground;   // 背景レイヤー
Container globalFg = app.GlobalForeground;   // 前景レイヤー

// 背景色
app.BackgroundColor = Color.Black;

// シーン管理
app.LoadScene<NewScene>();       // シーンを切り替え
app.PushScene<OverlayScene>();   // 現在のシーンの上に新しいシーンを積む
app.PopScene();                   // 最上位のシーンを削除

// プラグイン取得
Keyboard? keyboard = app.GetPlugin<Keyboard>();
bool hasKeyboard = app.TryGetPlugin<Keyboard>(out var kb);

// アプリケーション終了
app.Exit();

// 次のフレームでアクションを実行（イテレーション中のコレクション変更を防ぐ）
app.NextFrame(() => { /* 処理 */ });
```

#### アプリケーションビルダー

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()          // プラグイン登録
    .Use<Mouse>()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop(); // バックエンド選択
```

利用可能なバックエンド:
- `BuildWithOpenGLDesktop()` - デスクトップ向けOpenGL実装
- `BuildWithHeadless()` - ヘッドレスモード（テスト用）

### Scene (シーン)

`Scene`は、ゲームの各画面や状態を表す基底クラスです。

#### ライフサイクルメソッド

| メソッド | 実行タイミング | 用途 |
|---------|--------------|------|
| `OnStart()` | シーン開始時に1度だけ | 初期化、リソース読み込み |
| `OnUpdate()` | 毎フレーム | ゲームロジック、入力処理 |
| `OnDestroy()` | シーン終了時に1度だけ | リソース解放 |
| `OnPause()` | 別のシーンがPushされた時 | 一時停止処理 |
| `OnResume()` | PopSceneで再開した時 | 再開処理 |

#### シーンの基本構造

```csharp
public class GameScene : Scene
{
    public override void OnStart()
    {
        // Rootコンテナにノードを追加
        var sprite = new Sprite(texture);
        Root.Add(sprite);
    }

    public override void OnUpdate()
    {
        // 毎フレームの処理
        if (/* 条件 */)
        {
            App.LoadScene<NextScene>();
        }
    }

    public override void OnDestroy()
    {
        // クリーンアップ
    }
}
```

#### シーンのプロパティ

- `Root` - シーン全体のルートコンテナ（型: `Container`）
- `App` - PrometeAppインスタンスへの参照
- `Window` - ウィンドウへの参照

#### 依存性注入

シーンのコンストラクタでプラグインを受け取れます（C# 12のプライマリコンストラクタ推奨）：

```csharp
public class MainScene(Keyboard keyboard, Mouse mouse, AudioPlayer audio) : Scene
{
    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
            App.Exit();
    }
}
```

#### シーン管理

```csharp
// シーン切り替え（前のシーンは破棄）
App.LoadScene<TitleScene>();

// シーンスタック（現在のシーンを保持したまま新しいシーンを追加）
App.PushScene<PauseMenuScene>();  // ポーズメニューを重ねる
App.PopScene();                    // ポーズメニューを閉じる
```

### プラグインシステム

PrometeはMicrosoft.Extensions.DependencyInjectionをベースとしたDIコンテナを採用しています。

#### プラグインの登録

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()           // キーボード入力
    .Use<Mouse>()              // マウス入力
    .Use<Gamepads>()           // ゲームパッド入力
    .Use<ConsoleLayer>()       // デバッグコンソール
    .Use<CoroutineManager>()   // コルーチン管理
    .Use<AudioPlayer>()        // オーディオ再生
    .BuildWithOpenGLDesktop();
```

#### インターフェースと実装の分離

```csharp
var app = PrometeApp.Create()
    .Use<IMyService, MyServiceImpl>()  // インターフェースと実装を指定
    .BuildWithOpenGLDesktop();
```

#### プラグインの取得方法

**1. コンストラクタ注入（推奨）**

```csharp
public class MainScene(Keyboard keyboard, Mouse mouse, AudioPlayer audio) : Scene
{
    // keyboard, mouse, audio が自動的に注入される
}
```

**2. 安全な取得（TryGetPlugin）**

```csharp
if (App.TryGetPlugin<Keyboard>(out var keyboard))
{
    // キーボードプラグインが利用可能
}
```

**3. 直接取得（GetPlugin、例外あり）**

```csharp
try
{
    var audio = App.GetPlugin<AudioPlayer>();
}
catch (ArgumentException)
{
    // プラグインが登録されていない
}
```

#### カスタムプラグインの作成

```csharp
// ゲーム設定を管理するプラグイン
public class GameSettings
{
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsFullScreen { get; set; } = false;
    public string PlayerName { get; set; } = "Player";
}

// 登録
var app = PrometeApp.Create()
    .Use<GameSettings>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class SettingsScene(GameSettings settings) : Scene
{
    public override void OnUpdate()
    {
        // settings を利用
    }
}
```

### Window (ウィンドウ)

ウィンドウの制御とプロパティにアクセスできます。

```csharp
// シーン内からアクセス
var window = Window;  // または App.Window

// プロパティ
window.Title = "My Game";
window.Size = (1280, 720);
window.IsFullScreen = true;
window.VSync = true;

// イベント
window.FileDropped += (sender, e) =>
{
    foreach (var file in e.Files)
    {
        Console.WriteLine($"File dropped: {file}");
    }
};
```

---

## ノードシステム

### Node（ノード）の基本概念

`Node`は、Prometeにおける描画可能なすべての要素の基底クラスです。階層構造を形成し、親子関係による変形の継承をサポートします。

#### 座標系

- **原点**: 画面左上が (0, 0)
- **X軸**: 右方向が正
- **Y軸**: 下方向が正
- **角度**: 時計回りが正（度数 0-360°）

#### 標準ノードタイプ

| ノード | 説明 |
|--------|------|
| `Sprite` | テクスチャ画像を表示 |
| `Text` | テキストを表示 |
| `Shape` | 図形（矩形、円など）を描画 |
| `Tilemap` | タイルマップを描画 |
| `NineSliceSprite` | 9スライス画像を表示 |
| `Container` | 他のノードをグループ化 |

#### 共通プロパティ

```csharp
// 位置・変形
node.Location = (100, 200);      // 位置（Vector型）
node.Scale = (2.0f, 2.0f);       // スケール（Vector型）
node.Angle = 45;                 // 回転角度（度数 0-360°、float型）
node.Pivot = (0.5f, 0.5f);       // 回転・スケールの中心点（0-1の相対座標）

// サイズ
node.Size = (64, 64);            // サイズ（Vector型）
node.Width = 100;                // 幅
node.Height = 50;                // 高さ

// 表示制御
node.IsVisible = true;           // 表示/非表示
node.ZIndex = 10;                // 描画順序（大きいほど手前）

// 識別
node.Name = "Player";            // ノード名

// 親子関係
Node? parent = node.Parent;      // 親ノードへの参照

// 絶対座標（読み取り専用）
Vector absLocation = node.AbsoluteLocation;
Vector absScale = node.AbsoluteScale;
float absAngle = node.AbsoluteAngle;
```

#### Setup API（メソッドチェーン）

ノードの初期化を簡潔に記述できます：

```csharp
var sprite = new Sprite(texture)
    .Location(100, 200)
    .Scale(2.0f)
    .Angle(45)  // 45度
    .Pivot(0.5f, 0.5f)
    .ZIndex(10)
    .Visible(true);
```

#### 親子関係と変形の継承

子ノードは親ノードの変形（位置、回転、スケール）を継承します：

```csharp
var parent = new Container()
    .Location(200, 100)
    .Scale(2.0f)
    .Angle(30);  // 30度

var child = new Sprite(texture)
    .Location(50, 0);  // 親からの相対座標

parent.Add(child);

// childの実際の位置: 親の変形を適用した結果
// 絶対座標は child.AbsoluteLocation で取得可能

// 重要: ノードを自身の子として追加することはできません
// parent.Add(parent); // ArgumentException がスローされる
```

#### ライフサイクル

```csharp
public class CustomNode : Node
{
    public override void OnUpdate()
    {
        // 毎フレーム実行
    }

    public override void OnRender()
    {
        // レンダリング処理（高度な用途）
    }

    public override void OnDestroy()
    {
        // 破棄時のクリーンアップ
    }
}
```

### Sprite（スプライト）

テクスチャ画像を表示するノードです。

```csharp
// テクスチャの読み込み
var texture = TextureFactory.Load("assets/player.png");

// スプライトの作成
var sprite = new Sprite(texture)
    .Location(100, 100);

// シーンに追加
Root.Add(sprite);

// プロパティ
sprite.Texture = newTexture;       // テクスチャ変更
sprite.TintColor = Color.Red;      // 色合い変更
sprite.Width = 64;                 // サイズ変更
sprite.Height = 64;
sprite.ResetSize();                // 元のテクスチャサイズに戻す
```

#### TintColor

スプライトに色を重ねることができます：

```csharp
sprite.TintColor = Color.Red;           // 赤色を重ねる
sprite.TintColor = new Color(255, 0, 0, 128);  // 半透明の赤
```

### Text（テキスト）

テキストを表示するノードです。

```csharp
// デフォルトフォントでテキスト作成
var text = new Text("Hello, World!", Font.GetDefault(), Color.White)
    .Location(100, 100);

Root.Add(text);

// プロパティ
text.Content = "New Text";         // テキスト内容変更
text.Color = Color.Blue;           // 色変更
text.Font = myFont;                // フォント変更
```

### Shape（図形）

基本的な図形を描画するノードです。

```csharp
// 矩形
var rect = new Shape(ShapeType.Rectangle, (100, 50), Color.Blue)
    .Location(200, 200);

// 円
var circle = new Shape(ShapeType.Ellipse, (50, 50), Color.Green)
    .Location(300, 300);

Root.Add(rect);
Root.Add(circle);
```

利用可能な図形タイプ:
- `ShapeType.Rectangle` - 矩形
- `ShapeType.Ellipse` - 楕円・円
- `ShapeType.Triangle` - 三角形

### Container（コンテナ）

複数のノードをグループ化して管理します。

```csharp
// コンテナの作成
var group = new Container();

// 子ノードの追加
group.Add(sprite1);
group.Add(sprite2);
group.AddRange(text1, text2, shape1);

// シーンに追加
Root.Add(group);

// コレクション操作
int count = group.Count;
Node firstChild = group[0];
bool hasNode = group.Contains(sprite1);

// 反復処理
foreach (var child in group)
{
    child.IsVisible = true;
}

// LINQ使用
var sprites = group.OfType<Sprite>().ToList();
var visibleNodes = group.Where(n => n.IsVisible);

// 削除
group.Remove(sprite1);
group.RemoveAt(0);
group.Clear();
```

#### 階層構造と変形の継承

```csharp
var parent = new Container()
    .Location(100, 100)
    .Scale(2.0f);

var child = new Sprite(texture)
    .Location(50, 50);  // 親からの相対座標

parent.Add(child);

// 相対座標
var relativePos = child.Location;          // (50, 50)

// 絶対座標（画面上の実際の位置）
var absolutePos = child.AbsoluteLocation;  // (200, 200) = (100 + 50*2, 100 + 50*2)
```

### Tilemap（タイルマップ）

タイルベースのマップを描画します。

```csharp
// タイルマップの作成
var tilemap = new Tilemap((32, 32));  // タイルサイズ

// タイルの設定
var grassTile = new Tile(grassTexture);
tilemap[0, 0] = grassTile;
tilemap[1, 0] = waterTile;

Root.Add(tilemap);
```

### NineSliceSprite（9スライススプライト）

伸縮可能なUI要素を作成します（角は変形せず、辺と中心のみ伸縮）。

```csharp
var texture9 = new Texture9Sliced(texture, 10, 10, 10, 10);
var panel = new NineSliceSprite(texture9)
    .Size(200, 100);  // 任意のサイズに伸縮

Root.Add(panel);
```

---

## グラフィックス

### Texture2D（テクスチャ）

画像データを表します。

```csharp
// テクスチャの読み込み
Texture2D texture = TextureFactory.Load("assets/image.png");

// プロパティ
int width = texture.Width;
int height = texture.Height;
Vector size = texture.Size;

// 破棄
texture.Dispose();
```

### TextureFactory（テクスチャファクトリ）

テクスチャの読み込みを管理します。

```csharp
// ファイルから読み込み
var texture = TextureFactory.Load("assets/player.png");

// ストリームから読み込み
using var stream = File.OpenRead("assets/enemy.png");
var texture2 = TextureFactory.Load(stream);
```

### FrameBuffer（フレームバッファ）

オフスクリーンレンダリングを実現します。

```csharp
// フレームバッファの作成
var fb = new FrameBuffer((800, 600));

// フレームバッファに描画
fb.Begin();
// ここでの描画はフレームバッファに対して行われる
sprite.Render(/* ... */);
fb.End();

// フレームバッファの内容をテクスチャとして取得
Texture2D resultTexture = fb.Texture;

// スプライトとして表示
var resultSprite = new Sprite(resultTexture);
Root.Add(resultSprite);
```

---

## 入力システム

### Keyboard（キーボード）

キーボード入力を処理します。

```csharp
// プラグイン登録
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class GameScene(Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        // キーの状態チェック
        if (keyboard.Up.IsKeyDown)      // このフレームで押された
            MoveUp();

        if (keyboard.Enter.IsPressed)   // 押されている
            Charging();

        if (keyboard.Space.IsKeyUp)     // このフレームで離された
            Jump();

        // 全押下キーの取得
        foreach (var key in keyboard.AllPressedKeys)
        {
            Console.WriteLine($"Pressed: {key}");
        }

        // 文字列入力の取得
        if (keyboard.HasChar())
        {
            string input = keyboard.GetString();
            Console.WriteLine($"Input: {input}");
        }
    }
}
```

#### Key プロパティ

各キーは以下のプロパティを持ちます：

```csharp
Key key = keyboard.A;

bool isPressed = key.IsPressed;         // 押されている
bool isKeyDown = key.IsKeyDown;         // このフレームで押された
bool isKeyUp = key.IsKeyUp;             // このフレームで離された
int frameCount = key.ElapsedFrameCount; // 押されてからのフレーム数
float time = key.ElapsedTime;           // 押されてからの時間（秒）
```

#### 主なキー

```csharp
keyboard.Up, Down, Left, Right  // 矢印キー
keyboard.Enter, Space, Escape   // 特殊キー
keyboard.A - Z                  // アルファベット
keyboard.D0 - D9                // 数字キー
keyboard.F1 - F12               // ファンクションキー
keyboard.LeftShift, LeftControl, LeftAlt
```

#### API

```csharp
// キーの列挙
IEnumerable<KeyCode> allKeys = keyboard.AllKeyCodes;
IEnumerable<KeyCode> pressed = keyboard.AllPressedKeys;
IEnumerable<KeyCode> down = keyboard.AllDownKeys;
IEnumerable<KeyCode> up = keyboard.AllUpKeys;

// 文字入力
string text = keyboard.GetString();  // バッファをクリア
char c = keyboard.GetChar();         // 1文字取得
bool hasInput = keyboard.HasChar();  // 入力があるか

// 仮想キーボード（モバイル）
keyboard.OpenVirtualKeyboard();
keyboard.CloseVirtualKeyboard();
```

### Mouse（マウス）

マウス入力を処理します。

```csharp
// プラグイン登録
var app = PrometeApp.Create()
    .Use<Mouse>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class GameScene(Mouse mouse) : Scene
{
    public override void OnUpdate()
    {
        // マウス位置（VectorInt型）
        VectorInt position = mouse.Position;

        // ボタンの状態（インデクサ経由でアクセス）
        if (mouse[MouseButtonType.Left].IsButtonDown)     // クリックした瞬間
            OnClick(mouse.Position);

        if (mouse[MouseButtonType.Right].IsPressed)       // 押されている
            OnRightDrag(mouse.Position);

        if (mouse[MouseButtonType.Middle].IsButtonUp)     // 離した瞬間
            OnMiddleRelease();

        // ホイール（Vector型）
        Vector scroll = mouse.Scroll;
    }
}
```

#### MouseButton プロパティ

```csharp
MouseButton btn = mouse.Left;

bool isPressed = btn.IsPressed;         // 押されている
bool isDown = btn.IsButtonDown;         // このフレームで押された
bool isUp = btn.IsButtonUp;             // このフレームで離された
int frameCount = btn.ElapsedFrameCount; // 押されてからのフレーム数
float time = btn.ElapsedTime;           // 押されてからの時間
```

#### API

```csharp
VectorInt pos = mouse.Position;   // マウス座標（VectorInt型）
Vector scroll = mouse.Scroll;     // ホイールの変化量（Vector型）

MouseButton left = mouse[MouseButtonType.Left];    // 左ボタン
MouseButton right = mouse[MouseButtonType.Right];  // 右ボタン
MouseButton middle = mouse[MouseButtonType.Middle]; // 中ボタン
```

### Gamepads（ゲームパッド）

ゲームパッド入力を処理します。

```csharp
// プラグイン登録
var app = PrometeApp.Create()
    .Use<Gamepads>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class GameScene(Gamepads gamepads) : Scene
{
    public override void OnUpdate()
    {
        // 接続されているゲームパッドの取得
        foreach (var gamepad in gamepads.ConnectedGamepads)
        {
            // ボタンの状態
            if (gamepad.A.IsButtonDown)
                Jump();

            // スティック
            Vector leftStick = gamepad.LeftStick;
            Vector rightStick = gamepad.RightStick;

            // トリガー
            float leftTrigger = gamepad.LeftTrigger;
            float rightTrigger = gamepad.RightTrigger;
        }

        // 最初のゲームパッド（ショートカット）
        if (gamepads.FirstOrDefault() is { } pad)
        {
            if (pad.Start.IsButtonDown)
                PauseGame();
        }
    }
}
```

---

## オーディオシステム

### AudioPlayer（オーディオプレイヤー）

音声再生を管理します。

```csharp
// AudioPlayerの作成
var audio = new AudioPlayer();

// 音源の読み込み
var bgm = new VorbisAudioSource("assets/bgm.ogg");
var sfx = new WaveAudioSource("assets/click.wav");

// BGM再生（ループあり）
audio.Play(bgm, loopStartSample: 0);

// 効果音再生（ワンショット）
audio.PlayOneShot(sfx);

// 制御
audio.Pause();         // 一時停止
audio.Resume();        // 再開
audio.Stop();          // 停止
audio.Stop(2.0f);      // 2秒かけてフェードアウト

// プロパティ
audio.Gain = 0.8f;     // 音量（0.0 - 1.0）
audio.Pan = -0.5f;     // パン（-1.0 - 1.0）
audio.Pitch = 1.2f;    // ピッチ倍率

// 状態
bool playing = audio.IsPlaying;
bool pausing = audio.IsPausing;
int time = audio.Time;           // 再生位置（ミリ秒）
int length = audio.Length;       // 長さ（ミリ秒）
```

#### イベント

```csharp
audio.StartPlaying += (sender, e) =>
{
    Console.WriteLine("再生開始");
};

audio.StopPlaying += (sender, e) =>
{
    Console.WriteLine("停止");
};

audio.Loop += (sender, e) =>
{
    Console.WriteLine("ループ");
};

audio.FinishPlaying += (sender, e) =>
{
    Console.WriteLine("再生終了");
};
```

#### 対応フォーマット

- WAV (WaveAudioSource)
- Ogg Vorbis (VorbisAudioSource)

---

## テキストシステム

### ConsoleLayer（コンソールレイヤー）

デバッグや簡易UIに使用できるコンソール表示機能です。

```csharp
// プラグイン登録
var app = PrometeApp.Create()
    .Use<ConsoleLayer>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class DebugScene(ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        console.Print("Promete Debug Console");
        console.Print("Press [F1] for help");
    }

    public override void OnUpdate()
    {
        console.Clear();  // 画面クリア
        console.Print($"FPS: {Window.Fps}");
        console.Print($"Frame: {Window.TotalFrame}");

        // 複数行出力
        console.Print("Line 1\nLine 2\nLine 3");
    }
}
```

#### API

```csharp
console.Print(object obj);           // テキスト出力
console.Clear();                     // 画面クリア
VectorInt cursor = console.Cursor;   // カーソル位置
console.Font = myFont;               // フォント変更
console.TextColor = Color.Green;     // 文字色変更
```

### Font（フォント）

フォント管理を行います。

```csharp
// デフォルトフォント
Font defaultFont = Font.GetDefault();

// カスタムフォントの読み込み
Font customFont = Font.Load("assets/myfont.ttf", 24);

// テキストノードで使用
var text = new Text("Hello", customFont, Color.White);
```

---

## 数学ユーティリティ

### Vector / VectorInt

2D座標やベクトルを表します。

```csharp
// 実数ベクトル
Vector v = new Vector(10.5f, 20.0f);
Vector v2 = (10.5f, 20.0f);  // タプルから暗黙的変換

float x = v.X;
float y = v.Y;
float length = v.Magnitude;        // 長さ
Vector normalized = v.Normalized;  // 単位ベクトル

// 演算
Vector sum = v1 + v2;
Vector diff = v1 - v2;
Vector scaled = v * 2.0f;
Vector divided = v / 2.0f;

// 静的メソッド
float angle = Vector.Angle(from, to);    // 角度（ラジアン）
float distance = Vector.Distance(v1, v2); // 距離
float dot = Vector.Dot(v1, v2);          // 内積

// インスタンスメソッド
float angleToTarget = v.Angle(target);
float distToTarget = v.Distance(target);
bool inRect = v.In(rect);                // 矩形内判定

// タプル分解
var (vx, vy) = v;

// 定数
Vector.Zero     // (0, 0)
Vector.One      // (1, 1)
Vector.Left     // (-1, 0)
Vector.Up       // (0, -1)
Vector.Right    // (1, 0)
Vector.Down     // (0, 1)
```

```csharp
// 整数ベクトル
VectorInt vi = new VectorInt(10, 20);
VectorInt vi2 = (10, 20);  // タプルから暗黙的変換

int x = vi.X;
int y = vi.Y;

// 演算（Vectorと同様）
VectorInt sum = vi1 + vi2;
```

### Rect / RectInt

矩形を表します。

```csharp
// 実数矩形
Rect rect = new Rect((10f, 20f), (100f, 50f));  // (位置, サイズ)
Rect rect2 = (10f, 20f, 100f, 50f);  // タプルから

Vector location = rect.Location;
Vector size = rect.Size;
float left = rect.Left;
float top = rect.Top;
float right = rect.Right;
float bottom = rect.Bottom;

// 判定
bool intersects = rect1.Intersect(rect2);  // 交差判定

// 変形
Rect moved = rect.Translate((10, 10));     // 平行移動

// タプル分解
var (pos, size) = rect;
var (x, y, w, h) = rect;
```

```csharp
// 整数矩形
RectInt ri = new RectInt((10, 20), (100, 50));
RectInt ri2 = (10, 20, 100, 50);
```

### MathHelper

数学ヘルパー関数を提供します。

```csharp
// 線形補間
float lerp = MathHelper.Lerp(0.5f, start, end);

// 角度変換
float radians = MathHelper.ToRadian(180);  // 180度をラジアンに
float degrees = MathHelper.ToDegree(MathF.PI);  // ラジアンを度に

// イージング関数
float easeIn = MathHelper.EaseIn(0.5f, start, end);
float easeOut = MathHelper.EaseOut(0.5f, start, end);
float easeInOut = MathHelper.EaseInOut(0.5f, start, end);
```

---

## その他の機能

### Coroutine（コルーチン）

Unity風のコルーチンシステムです。

```csharp
// プラグイン登録
var app = PrometeApp.Create()
    .Use<CoroutineManager>()
    .BuildWithOpenGLDesktop();

// シーンで使用
public class GameScene(CoroutineManager coroutine) : Scene
{
    public override void OnStart()
    {
        // コルーチンの開始
        coroutine.Start(FadeInEffect());
    }

    private IEnumerator FadeInEffect()
    {
        float alpha = 0;
        while (alpha < 1)
        {
            alpha += 0.01f;
            sprite.TintColor = new Color(255, 255, 255, (byte)(alpha * 255));
            yield return null;  // 次のフレームまで待機
        }
    }

    private IEnumerator WaitAndAction()
    {
        // 3秒待つ
        yield return new WaitForSeconds(3.0f);
        Console.WriteLine("3秒経過");

        // 条件を満たすまで待つ
        yield return new WaitUntil(() => playerHealth <= 0);
        GameOver();

        // Taskの完了を待つ
        yield return new WaitForTask(LoadDataAsync());
    }
}
```

#### Yield Instructions

- `yield return null` - 次のフレームまで待機
- `new WaitForSeconds(seconds)` - 指定秒数待機
- `new WaitUntil(predicate)` - 条件を満たすまで待機
- `new WaitForTask(task)` - Taskの完了を待機
- `new WaitUntilNextFrame()` - 明示的な次フレーム待機

### 拡張メソッド

便利な拡張メソッドが多数提供されています。

```csharp
// Vector拡張
Vector rotated = vector.Rotate(angle);
Vector rounded = vector.Round();

// Random拡張
Random random = new Random();
int dice = random.Next(1, 7);          // 1-6のランダム
bool coinFlip = random.NextBool();     // true/false
float chance = random.NextFloat();     // 0.0-1.0
```

---

## 拡張性

### カスタムノード

独自のノードを作成できます。

```csharp
public class CustomNode : Node
{
    public override void OnUpdate()
    {
        // 毎フレームの更新処理
        Location += (1, 0);  // 右に移動
    }

    public override void OnRender()
    {
        // カスタム描画処理（高度な用途）
    }

    public override void OnDestroy()
    {
        // クリーンアップ
    }
}
```

### カスタムレンダラー

ノードの描画方法をカスタマイズできます（高度）。

### カスタムオーディオソース

独自の音源フォーマットをサポートできます。

```csharp
public class CustomAudioSource : IAudioSource
{
    public int SampleRate { get; }
    public int Channels { get; }
    public int Bits { get; }
    public int? Samples { get; }

    public (int size, bool isFinished) FillSamples(Span<short> buffer, int offset)
    {
        // サンプルデータを buffer に書き込む
        // 戻り値: (書き込んだサンプル数, 終端に達したか)
    }
}
```

### カスタムバックエンド

独自の描画バックエンドを実装できます（非常に高度）。

---

## APIリファレンス

### 主要クラス一覧

#### コア
- `PrometeApp` - アプリケーションのエントリーポイント
- `Scene` - ゲームシーンの基底クラス
- `IWindow` - ウィンドウインターフェース

#### ノード
- `Node` - 全ノードの基底クラス
- `Sprite` - 画像表示
- `Text` - テキスト表示
- `Shape` - 図形描画
- `Tilemap` - タイルマップ
- `NineSliceSprite` - 9スライス画像
- `Container` - ノードコンテナ

#### グラフィックス
- `Texture2D` - テクスチャデータ
- `TextureFactory` - テクスチャ読み込み
- `FrameBuffer` - オフスクリーンバッファ
- `ITile` - タイルインターフェース

#### 入力
- `Keyboard` - キーボード入力
- `Mouse` - マウス入力
- `Gamepads` - ゲームパッド入力
- `Key` - キー状態
- `MouseButton` - マウスボタン状態

#### オーディオ
- `AudioPlayer` - オーディオ再生
- `IAudioSource` - 音源インターフェース
- `VorbisAudioSource` - Ogg Vorbis音源
- `WaveAudioSource` - WAV音源

#### テキスト
- `ConsoleLayer` - コンソール表示
- `Font` - フォント管理

#### 数学
- `Vector` - 2D実数ベクトル
- `VectorInt` - 2D整数ベクトル
- `Rect` - 実数矩形
- `RectInt` - 整数矩形
- `MathHelper` - 数学ヘルパー

#### その他
- `CoroutineManager` - コルーチン管理
- `WaitForSeconds` - 秒数待機
- `WaitUntil` - 条件待機
- `WaitForTask` - Task待機

### 重要なメソッド

```csharp
// PrometeApp
int Run<TScene>() where TScene : Scene  // シーンを指定して実行
int Run()                                // シーンなしで実行（v1.1.0以降）
void LoadScene<T>() where T : Scene
void PushScene<T>() where T : Scene
bool PopScene()
T GetPlugin<T>() where T : class
bool TryGetPlugin<T>(out T? plugin) where T : class
void Exit(int status = 0)
void NextFrame(Action action)

// Scene
virtual void OnStart()
virtual void OnUpdate()
virtual void OnDestroy()
virtual void OnPause()
virtual void OnResume()

// Node
void Add(Node child)      // ContainableNode経由
void Remove(Node child)   // ContainableNode経由
void Destroy()

// Container
void Add(Node node)
void AddRange(params Node[] nodes)
void Remove(Node node)
void RemoveAt(int index)
void Clear()
bool Contains(Node node)

// AudioPlayer
void Play(IAudioSource source, int? loop = null)
void PlayOneShot(IAudioSource source, float gain = 1, float pitch = 1, float pan = 0)
void Stop(float fadeTime = 0)
void Pause()
void Resume()

// Keyboard
string GetString()
char GetChar()
bool HasChar()
void OpenVirtualKeyboard()
void CloseVirtualKeyboard()

// TextureFactory
static Texture2D Load(string path)
static Texture2D Load(Stream stream)

// CoroutineManager
Coroutine Start(IEnumerator routine)
void Stop(Coroutine coroutine)
void StopAll()
```

### 設計パターンとベストプラクティス

1. **プライマリコンストラクタの活用**: C# 12以降では、シーンでプライマリコンストラクタを使用してプラグインを受け取る
2. **Setup APIの使用**: ノードの初期化は Setup API でメソッドチェーン
3. **Rootコンテナの活用**: シーンのノードは Root に追加
4. **リソース管理**: IDisposable なリソースは適切に破棄
5. **コルーチンの活用**: 時間のかかる処理や待機処理はコルーチンで実装
6. **プラグインシステム**: 機能はプラグインとして登録し、DIで注入
7. **階層構造**: 関連するノードは Container でグループ化

---

## サンプルコード

### 完全なゲームの例

```csharp
using Promete;
using Promete.Windowing;
using Promete.Graphics;
using Promete.Input;
using Promete.Audio;
using Promete.Nodes;
using Promete.GLDesktop;

// アプリケーション初期化
var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<ConsoleLayer>()
    .Use<AudioPlayer>()
    .BuildWithOpenGLDesktop();

// ウィンドウ設定
app.Window.Title = "My Game";
app.Window.Size = (1280, 720);

// ゲーム開始
return app.Run<TitleScene>();

// タイトルシーン
public class TitleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        console.Print("=== MY GAME ===");
        console.Print("Press [ENTER] to start");
    }

    public override void OnUpdate()
    {
        if (keyboard.Enter.IsKeyDown)
        {
            App.LoadScene<GameScene>();
        }
    }
}

// ゲームシーン
public class GameScene(Keyboard keyboard, Mouse mouse, AudioPlayer audio) : Scene
{
    private Sprite? _player;
    private readonly IAudioSource _bgm = new VorbisAudioSource("assets/bgm.ogg");

    public override void OnStart()
    {
        // プレイヤー作成
        var playerTexture = TextureFactory.Load("assets/player.png");
        _player = new Sprite(playerTexture)
            .Location(100, 100)
            .Scale(2.0f)
            .Pivot(0.5f, 0.5f);

        Root.Add(_player);

        // BGM再生
        audio.Play(_bgm, loop: 0);
    }

    public override void OnUpdate()
    {
        if (_player == null) return;

        // 移動
        var velocity = Vector.Zero;
        if (keyboard.Up.IsPressed) velocity += Vector.Up;
        if (keyboard.Down.IsPressed) velocity += Vector.Down;
        if (keyboard.Left.IsPressed) velocity += Vector.Left;
        if (keyboard.Right.IsPressed) velocity += Vector.Right;

        if (velocity != Vector.Zero)
        {
            _player.Location += velocity.Normalized * 5;
        }

        // マウスを向く
        var angle = _player.Location.Angle(mouse.Position);
        _player.Angle = angle;

        // ESCで終了
        if (keyboard.Escape.IsKeyDown)
        {
            App.Exit();
        }
    }

    public override void OnDestroy()
    {
        audio.Stop();
    }
}
```

---

以上が、Prometeゲームエンジンの包括的なドキュメントです。このドキュメントには、基本概念、主要機能、API、サンプルコードなど、Prometeを使用したゲーム開発に必要な情報が含まれています。
