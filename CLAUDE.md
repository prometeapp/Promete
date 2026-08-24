# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。

## プロジェクト概要

**Promete** は .NET 10以降向けの2Dゲームエンジン。階層ノードシステムとDIベースのプラグインアーキテクチャを通じて、シンプルさ、拡張性、ピクセルパーフェクトな2Dグラフィックスを重視しています。

- **主要言語**: C# (.NET 10)
- **グラフィックスバックエンド**: OpenGL (Silk.NET経由)
- **アーキテクチャ**: DIコンテナベース (Microsoft.Extensions.DependencyInjection)
- **ライセンス**: MIT

## ビルドとテストコマンド

### ソリューション全体のビルド

```bash
dotnet build Promete.sln
```

### 特定のプロジェクトをビルド

```bash
# メインライブラリ
dotnet build Promete/Promete.csproj

# サンプルプロジェクト
dotnet build Promete.Example/Promete.Example.csproj
```

### サンプルの実行

```bash
dotnet run --project Promete.Example/
```

### テストの実行

```bash
dotnet test Promete.Test/Promete.Test.csproj
```

注意: テストは xUnit と FluentAssertions を使用しています。テストスイートは現在開発中です。

## プロジェクト構造

```
Promete/                    - メインゲームエンジンライブラリ
├── Audio/                  - オーディオ再生システム (OpenAL)
├── Coroutines/             - Unity風コルーチンシステム
├── Graphics/               - テクスチャ、フォント、フレームバッファ管理
│   └── Rendering/          - レンダリングコマンドキューと CommandRunner 実装
│       └── GL/             - OpenGL固有のランナー・ファクトリ実装
├── Input/                  - キーボード、マウス、ゲームパッド入力
├── Nodes/                  - 描画可能なノード階層 (Sprite, Text等)
├── Backends/               - バックエンド抽象化 (BackendBase) と各バックエンド実装
│   ├── GL/                 - OpenGLデスクトップバックエンド実装
│   ├── Headless/           - ヘッドレスバックエンド実装
│   └── SilkNetCommon/      - Silk.NET系バックエンド共通処理
├── Windowing/              - ウィンドウ関連の型 (IWindow は非推奨、WindowOptions 等を提供)
├── GLDesktop/              - OpenGLデスクトップ用ビルド拡張 (BuildWithOpenGLDesktop)
└── Headless/               - ヘッドレス用ビルド拡張 (BuildWithHeadless)

Promete.Example/            - [Demo]属性を使用したデモプロジェクト
Promete.ImGui/              - ImGui統合プラグイン
Promete.MeltySynth/         - MIDI/SoundFontプラグイン
Promete.Test/               - xUnitテストスイート
Promete.HeadlessTest/       - ヘッドレスバックエンド用テストプロジェクト
Promete.Docs/               - ドキュメントサイト (Astro/Starlight)
```

## コアアーキテクチャ

### 1. 依存性注入システム

Promete は Microsoft.Extensions.DependencyInjection を基盤として使用しています。すべての機能は「プラグイン」としてDIコンテナを通じて登録されます。

**プラグイン登録パターン:**

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()          // シングルトンとして登録
    .Use<Mouse>()
    .Use<IMyService, MyImpl>() // インターフェースと実装
    .BuildWithOpenGLDesktop();
```

**シーンでのプラグイン取得:**

```csharp
// コンストラクタ注入 (推奨 - C# 12のプライマリコンストラクタを使用)
public class MainScene(Keyboard keyboard, AudioPlayer audio) : Scene
{
    // keyboard と audio が自動的に注入される
}
```

### 2. シーン管理

シーンはゲームの状態や画面を表します。エントリアセンブリからリフレクションによって自動的に検出・登録されます。

**シーンのライフサイクル:**

- `OnStart()` - シーン読み込み時に1度だけ呼ばれる (リソース初期化)
- `OnUpdate()` - 毎フレーム呼ばれる (ゲームロジック)
- `OnDestroy()` - シーン破棄時に呼ばれる (クリーンアップ)
- `OnPause()` - 別のシーンがプッシュされた時に呼ばれる
- `OnResume()` - プッシュされたシーンから戻った時に呼ばれる

**シーンナビゲーション:**

```csharp
App.LoadScene<TitleScene>();    // 現在のシーンを置き換え
App.PushScene<PauseScene>();    // 現在のシーンの上にスタック (現在のシーンは一時停止)
App.PopScene();                 // 最上位のシーンを削除して前のシーンを再開
```

**重要:** シーンは `Transient` サービスとして登録されるため、毎回新しいインスタンスが作成されます。自動登録を防ぐには `[IgnoredScene]` 属性を追加してください。

### 3. ノード階層システム

ノードは描画可能な要素のコアです。親子階層を形成し、変形 (位置、回転、スケール) が継承されます。

**ノードの種類:**

- `Container` - 子ノードをグループ化 (視覚的表現なし)
- `Sprite` - テクスチャを表示
- `Text` - フォントでテキストを描画
- `Shape` - プリミティブ図形 (矩形、円、三角形)
- `Tilemap` - タイルベースのマップ描画
- `NineSliceSprite` - 9スライステクスチャでスケーラブルなUI要素

**座標系:**

- 原点 (0, 0) は左上
- X軸: 右が正
- Y軸: 下が正
- 回転: 時計回りが正 (ラジアン)

**変形の階層:**

```csharp
var parent = new Container().Location(100, 100).Scale(2.0f);
var child = new Sprite(texture).Location(50, 0); // 親からの相対座標
parent.Add(child);
// child.AbsoluteLocation は親の変形を反映した結果になる
```

### 4. レンダリングシステム

描画はコマンドキューパターンで実装されています。各ノードは `Collect(RenderCommandQueue, RenderContext)` をオーバーライドしてレンダリングコマンドを発行し、`CommandRunner<T>` がコマンドを受け取って実際のOpenGL呼び出しを実行します。

```csharp
// ノード側: Collect でコマンドをキューに積む
public override void Collect(RenderCommandQueue queue, RenderContext ctx)
{
    queue.Enqueue(new DrawTextureCommand { Texture = ..., ModelMatrix = ModelMatrix, ... });
}

// ランナーの登録は RenderCommandQueue に対して行う
queue.RegisterRunner<DrawTextureBatchedCommand>(new GLDrawTextureBatchedCommandRunner(view));
```

`DrawTextureCommand` は同一テクスチャハンドル・同一マテリアルの連続するコマンドが自動的に `DrawTextureBatchedCommand` にバッチ化され、インスタンシング描画されます。標準ランナーはバックエンド (例: `BuildWithOpenGLDesktop()`) によって自動的に登録されます。

## 重要な実装パターン

### Setup API (メソッドチェーン)

ノードは流暢な初期化をサポートしています:

```csharp
var sprite = new Sprite(texture)
    .Location(100, 200)
    .Scale(2.0f)
    .Pivot(0.5f, 0.5f)
    .ZIndex(10);
```

すべての Setup API メソッドはノードインスタンスを返すため、チェーンできます。

### LoadSpriteSheet の挙動

`TextureFactory.LoadSpriteSheet` は画像ファイルを**1枚のGLテクスチャ（アトラス）**としてアップロードし、各セルに対応する `Texture2D` を `UvStart`/`UvEnd` だけ異なる形で返します。全セルが同じ `Handle` を共有するため、Tilemapで使用すると同一バッチにまとめてインスタンシング描画されます。UV 境界の浮動小数点誤差による隣接タイルへのブリーディングを防ぐため、ハーフテクセルインセットが適用されています。

### リソース管理

テクスチャなどの IDisposable リソースは手動で破棄する必要があります:

```csharp
public override void OnDestroy()
{
    texture?.Dispose();
    sprite?.Destroy(); // ノードの場合
}
```

### NextFrame パターン

コールバック中にシーン状態を変更する操作は `App.NextFrame()` を使用してください:

```csharp
App.NextFrame(() => {
    App.LoadScene<GameScene>();
});
```

これにより、イテレーション中のコレクション変更による問題を防ぎます。

### ノードの自己参照保護

ノードは (直接的または間接的に) 自身の子として追加できません。システムは循環階層を防ぐため `ArgumentException` をスローします。

## バックエンドシステム

Promete は `Backends/BackendBase` 抽象クラスの実装を通じて複数のバックエンドをサポートしています (旧 `IWindow` は非推奨・後方互換のため残置のみ):

- **OpenGL Desktop** (`BuildWithOpenGLDesktop()`): `Backends/GL/OpenGLDesktopBackend` — Windows/macOS/Linux対応のプロダクション向け
- **Headless** (`BuildWithHeadless()`): `Backends/Headless/HeadlessBackend` — グラフィックスなしで動作するバックエンド (`Promete.HeadlessTest` でテスト)

`BackendBase` の責務 (各 `Setup*` メソッドで提供):

- `SetupTimeProvider()` - 時間情報 (`ITimeProvider`)
- `SetupGameView()` - ゲーム画面アクセス (`IGameView`)
- `SetupInputProvider()` - 入力ハンドリング (`InputProvider`)
- `SetupScreenBlitter()` - 画面転送 (`IScreenBlitter`)
- `SetupTextureFactory()` - テクスチャ読み込み (`TextureFactoryBase`)
- `SetupRenderTextureProvider()` - RenderTexture機能 (`IRenderTextureProvider`)
- `SetupShaderFactory()` - シェーダーAPI (`IShaderFactory`)
- `OnInitialize()` / `OnStart()` / `OnExit()` - 初期化・起動・終了処理

## デモシステム (Promete.Example)

サンプルは自動メニュー生成のために `[Demo]` 属性を使用します:

```csharp
[Demo("/category/name", "Description")]
public class MyDemo(Keyboard keyboard) : Scene
{
    // デモの実装
}
```

デモは自動的に検出され、サンプルランチャーメニューに表示されます。

## ドキュメントに関する注意

- ドキュメントは**初学者向け**に、段階的な説明で記述されています
- メインドキュメントは `Promete.Docs/` にあります (Astro/Starlightフレームワーク)
- 目次構造は `Promete.Docs/toc.md` で定義されています
- LLM向けの包括的なドキュメントは `docs-llm.md` にあります (AI支援のためプロジェクトにコピー)

## 言語とコミュニケーション

- **IssueとPRのメッセージは日本語で記載すること**
- こちらからの指示や質問などには日本語で返すこと
- コードコメントは主に日本語
- パブリックAPIドキュメントは XML コメントを使用

## 重要なファイル

- `PrometeApp.cs` - アプリケーションのエントリポイント、DIコンテナ、シーン管理
- `Scene.cs` - ライフサイクルメソッドを持つシーン基底クラス
- `Nodes/Node.cs` - 変形階層を持つノード基底クラス
- `Graphics/TextureFactoryBase.cs` - テクスチャ読み込み基底クラス (実装は `Graphics/Rendering/GL/GLTextureFactory.cs`)
- `Backends/BackendBase.cs` - バックエンド抽象クラス (実装は `Backends/GL/OpenGLDesktopBackend.cs`, `Backends/Headless/HeadlessBackend.cs`)
