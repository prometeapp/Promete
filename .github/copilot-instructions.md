# Promete AI 開発支援ガイド

Promete は .NET 8 対応の 2D ゲーム開発フレームワークです。DIコンテナベースのプラグインシステムと階層ノード構造が特徴的な設計です。

## 🏗️ アーキテクチャ概要

### コア設計パターン
- **DIコンテナ**: Microsoft.Extensions.DependencyInjection を使用
- **プラグインシステム**: `PrometeApp.Create().Use<T>()` でサービス登録
- **シーン管理**: `Scene` 抽象クラスを継承、DIによる依存注入
- **ノード階層**: `Container` → `Node` の親子関係で描画管理

### 重要な実装パターン
```csharp
// プラグイン登録（PrometeAppBuilder内）
_services.AddSingleton<T>();

// シーンでの依存注入（コンストラクタ）
public MainScene(Keyboard keyboard, ConsoleLayer console) : Scene

// ノード階層管理
Root.Add(new Container { new Sprite(...), new Text(...) });
```

## 🛠️ 開発ワークフロー

### ビルド・テスト
```bash
# ソリューション全体ビルド
dotnet build Promete.sln

# サンプル実行
dotnet run --project Promete.Example/
```

### プロジェクト構成
- **Promete/**: メインライブラリ（.NET 8、OpenGL/Silk.NET）
- **Promete.Example/**: デモプロジェクト（[Demo] 属性で自動検出）
- **Promete.ImGui/**: ImGui拡張プラグイン
- **Promete.Test/**: xUnit テスト（書きかけ）
- **Promete.Docs/**: Astro ドキュメントサイト

その他のプロジェクトは実験的なものを含むため、特に指定がなければ参照しないこと

### 重要なファイル
- `PrometeApp.cs`: アプリエントリポイント、DIコンテナ管理
- `Scene.cs`: シーン基底クラス（OnStart/OnUpdate/OnDestroy）
- `Nodes/`: 描画要素（Sprite, Text, Container等）
- `Graphics/TextureFactory.cs`: テクスチャ読み込み管理

## 🎮 ゲーム開発パターン

### シーン作成
```csharp
public class GameScene(Keyboard keyboard, AudioPlayer audio) : Scene
{
    public override void OnStart()
    {
        // テクスチャ読み込み
        var texture = Window.TextureFactory.Load("assets/sprite.png");

        // ノード階層構築
        Root.Add(new Sprite(texture).Location(100, 100));
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp) App.LoadScene<MainScene>();
    }
}
```

### プラグイン作成
```csharp
// PrometeAppBuilder.Use<T>() で登録される
public class CustomPlugin
{
    public CustomPlugin(PrometeApp app, IWindow window)
    {
        // 初期化処理
    }
}
```

### ノード拡張
```csharp
public class CustomNode : Node
{
    // カスタムノードレンダラーと組み合わせて使用
    // PrometeAppBuilder.UseRenderer<CustomNode, CustomRenderer>()
}
```

## 🔧 プロジェクト固有の規約

### デモ/サンプル作成
- `Promete.Example/examples/` 配下に配置
- `[Demo("/path/name", "description")]` 属性でメニュー自動生成
- コンストラクタ注入でプラグイン取得

### テスト作成
- xUnit + FluentAssertions パターン
- `Promete.Test/` にテストファイル配置
- assets は `CopyToOutputDirectory: Always` 設定

### バックエンド対応
- OpenGL デスクトップ: `BuildWithOpenGLDesktop()`
- ヘッドレス: `BuildWithHeadless()` （バックエンドやテストなどで使える実験的なハリボテバックエンド）

## 📦 依存関係

### 主要パッケージ
- **Silk.NET**: OpenGL/入力/ウィンドウ管理
- **SixLabors.ImageSharp**: 画像処理
- **NVorbis**: Ogg Vorbis デコード
- **Microsoft.Extensions.DependencyInjection**: DIコンテナ

### プラグイン拡張
- ImGui: `Promete.ImGui` パッケージ
- MeltySynth: `Promete.MeltySynth` パッケージ（MIDI音源）

## 🚀 実装のベストプラクティス

### リソース管理
```csharp
// テクスチャの適切な破棄
public override void OnDestroy()
{
    texture?.Dispose();
    sprite?.Destroy();
}
```

### ノード操作
```csharp
// メソッドチェーンを活用
var sprite = new Sprite(texture)
    .Location(100, 100)
    .Scale(2.0f)
    .TintColor(Color.Red);
```

### エラーハンドリング
- `IgnoredSceneAttribute`: シーン自動登録から除外
- プラグイン未登録時: `ArgumentException` がスロー
- テクスチャ破棄後アクセス: `TextureDisposedException`

## 📝 ドキュメント作成

ドキュメントは、`Promete.Docs/` フォルダ内に配置します。`Promete.Docs/toc.md` に、必要な内容の見出しをまとめてあるので、この構成に従い作成してください。初学者向けの段階的な説明を心がけ、動作確認済みのサンプルコードを使用することが重要です。

Starlightの仕様上、タイトルが見出しとして自動的に表示されるため、先頭の見出しは不要です。

また、次のステップ・前のステップへのリンクも自動生成されるため、手動での追加は不要です。


