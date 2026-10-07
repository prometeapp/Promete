---
name: promete
description: Promete (C# / .NET 10 向けの 2D ゲームエンジン) を使ったコードを書く・読む・デバッグするときに使う。PrometeApp と DI プラグイン、シーン、ノード (Sprite / Text / Tilemap / Shape / Container / MaskedContainer など)、テクスチャ、フォントとテキスト、キーボード・マウス・ゲームパッド入力、オーディオ、Vector / Angle、コルーチン、ImGui 連携、カスタムノード・描画コマンド・バックエンドによる拡張の API と使い方を収録する。プロジェクトが Promete パッケージを参照しているとき、または「Promete で〜を作りたい」「Promete game」などの依頼で使う。v1 から v2 への移行作業には promete-v2-migration スキルを使う。
license: MIT
---

# Promete

Promete は .NET 10 以降向けの 2D ゲームエンジン。ノードの階層構造と、DI コンテナベースのプラグインで構成される。このスキルは Promete v2 を対象とする。

## 使い方

- 下の「基本」で全体像をつかみ、具体的な API は「ページ索引」から該当するページを読む
- references 以下は公式ドキュメント (https://promete.app) のガイドのコピー。Astro の MDX なので `import` 文や `<Tabs>` などのコンポーネントが含まれるが、中身は通常の Markdown として読める
- ページ内のリンク `/guide/<カテゴリ>/<ページ>` は `references/<カテゴリ>/<ページ>.md` (または `.mdx`) に対応する
- ドキュメントに書かれていない挙動は推測で断定せず、Promete のソースコードや XML ドキュメントコメントを確認する

## 基本

### アプリの構築と実行

```csharp
var app = PrometeApp.Create()
    .Use<Keyboard>()      // プラグインをシングルトンとして登録
    .Use<Mouse>()
    .BuildWithOpenGLDesktop(WindowOptions.Default with { Title = "My Game" });

return app.Run<MainScene>();
```

`WindowOptions` はバックエンドの構築時 (`BuildWithOpenGLDesktop`) に渡す。

### シーン

```csharp
public class MainScene(Keyboard keyboard) : Scene
{
    public override void OnStart() { /* 1 度だけ。リソースの読み込み */ }
    public override void OnUpdate() { /* 毎フレーム */ }
    public override void OnDestroy() { /* 破棄時。リソースの解放 */ }
}
```

- プラグインはコンストラクタ注入で受け取る。`Use<T>()` で登録していないプラグインは注入できない
- シーンからは `App` (PrometeApp)、`View` (画面: サイズ・タイトルなど)、`Time` (`DeltaTime` など)、`Root` (ルートのコンテナ) を使う
- 遷移は `App.LoadScene<T>()` (置き換え)、`App.PushScene<T>()` / `App.PopScene()` (スタック)
- シーンはソースジェネレータが自動登録する。`internal` 以上の可視性と public コンストラクタが必要
- コールバックの中でシーンの状態を変える操作は `App.NextFrame(() => ...)` で次のフレームに回す

### ノード

```csharp
var texture = App.TextureFactory.Load("assets/player.png");
var sprite = new Sprite(texture)
    .Location(100, 200)
    .Pivot(0.5f, 0.5f)
    .Angle(90.Degrees)
    .ZIndex(10);
Root.Add(sprite);
```

- 座標系は左上が原点。X は右、Y は下が正、回転は時計回りが正
- 子ノードの変形は親の変形を引き継ぐ
- 角度は `Angle` 型。`90.Degrees` / `MathF.PI.Radians` で生成する
- テクスチャなどの `IDisposable` なリソースは、`OnDestroy` で自分で破棄する

## 避けること

- `IWindow` と、シーンの `Window` プロパティは v1 の API で非推奨。`View` / `Time` / `App` を使う
- `NodeRendererBase` / `UseRenderer` は v2 で廃止された。描画はノードの `Collect()` でコマンドを積む

## ページ索引

<!-- BEGIN GENERATED INDEX -->
### 入門編

- [Prometeとは？](references/intro/about.md): Prometeゲームエンジンの概要について
- [クイックスタート](references/intro/start.mdx): Prometeの導入方法とプロジェクトのセットアップ
- [Hello, World!](references/intro/hello-world.md): 初めてのPrometeアプリケーションで、ConsoleLayerを使って簡単にテキストを表示する方法

### コア

- [PrometeApp](references/manual/app.md): Prometeアプリケーションのコアクラスの使い方と機能
- [ゲームビュー](references/manual/gameview.md): ウィンドウの表示・サイズ・状態を管理するIGameViewについて解説します。
- [プラグインシステム](references/manual/plugin-system.mdx): Prometeのプラグインシステムと依存性注入の仕組み
- [時間管理](references/manual/timeprovider.md): フレーム時間・FPS・タイムスケールを管理するITimeProviderについて解説します。

### グラフィック

- [シーン](references/graphics/scene.md): Prometeにおけるシーンの概念と使い方を解説します。
- [テクスチャ](references/graphics/textures.md): Prometeにおけるテクスチャの読み込みと管理について解説します。
- [ノードとは？](references/graphics/nodes.md): Prometeの描画システムの基本となるノードの概念について解説します。
- [Sprite](references/graphics/sprite.md): テクスチャ（画像）を表示するSpriteノードの使用方法について解説します。
- [PieSprite](references/graphics/pie-sprite.md): テクスチャを扇状（円グラフ状）に描画するPieSpriteノードの使用方法について解説します。
- [Text](references/graphics/text.md): テキストを表示するTextノードの使用方法について解説します。
- [Tilemap](references/graphics/tilemap.md): タイルベースの2Dマップを効率的に表示するTilemapノードの使用方法について解説します。
- [Shape](references/graphics/shape.md): 基本的な図形（線、矩形、三角形など）を描画するShapeノードの使用方法について解説します。
- [NineSliceSprite](references/graphics/nine-slice-sprite.md): 9スライス方式でリサイズ可能なUI要素を表示するNineSliceSpriteノードの使用方法について解説します。
- [Container](references/graphics/container.md): 複数のノードをグループ化して管理するContainerノードの使用方法について解説します。
- [フレームバッファ](references/graphics/framebuffer.md): ノードをテクスチャにレンダリングするFrameBufferの使用方法について解説します。
- [MaskedContainer](references/graphics/masked-container.md): マスク画像を使用して子要素を切り抜いて描画するMaskedContainerノードの使用方法について解説します。
- [シェーダーとマテリアル](references/graphics/shader.md): ShaderProgramとMaterialを使ってノードにカスタムシェーダーを適用する方法を解説します。
- [ポストプロセス](references/graphics/postprocess.md): App.PostProcessMaterialsを使って画面全体にシェーダーエフェクトを適用する方法を解説します。

### テキスト

- [ConsoleLayer](references/text/console-layer.md): ConsoleLayerによる簡易テキスト出力・主なAPI・サンプル・注意点を解説します。
- [フォント](references/text/font.md): フォントの基本・カスタムフォントの利用・主なAPIについて解説します。
- [PTML（リッチテキスト）](references/text/ptml.md): Prometeのリッチテキスト（PTML）機能の使い方・タグ一覧・サンプル・注意点を解説します。

### 入力

- [キーボード入力](references/input/keyboard.mdx): PrometeのKeyboardプラグインによるキーボード入力の基本・主なAPI・サンプル・注意点を解説します。
- [マウス入力](references/input/mouse.md): PrometeのMouseプラグインによるマウス入力の基本・主なAPI・サンプル・注意点を解説します。
- [ゲームパッド入力](references/input/gamepad.md): PrometeのGamepads/Gamepadプラグインによるゲームパッド入力の基本・主なAPI・サンプル・注意点を解説します。

### オーディオ

- [オーディオプレイヤー](references/audio/playback.md): PrometeのAudioPlayerによる音声再生・主なAPI・サンプル・注意点を解説します。
- [オーディオソース](references/audio/source.md): PrometeのIAudioSourceによる音源データの扱い・主なAPI・サンプル・注意点を解説します。
- [オーディオフィルター](references/audio/filters.md): PrometeのAudioPlayerに搭載されたDSPフィルター機能の使い方・標準フィルター・自作フィルターの作り方を解説します。

### 数学

- [VectorとRect](references/math/vector-rect.md): PrometeのVector/VectorInt/Rect/RectIntの基本・主なAPI・サンプル・注意点を解説します。
- [数学ヘルパー](references/math/helper.md): PrometeのMathHelperによる便利な数学関数・主なAPI・サンプル・注意点を解説します。
- [Angle（角度）](references/math/angle.md): PrometeのAngle型の基本・生成方法・演算・使用例を解説します。

### その他

- [コルーチン](references/other/coroutine.md): PrometeのCoroutine/CoroutineManagerによる非同期処理・主なAPI・サンプル・注意点を解説します。
- [便利な拡張メソッド](references/other/extensions.md): PrometeのRandomExtension/StringExtensionによる便利な拡張メソッド・主なAPI・サンプル・注意点を解説します。
- [Vulkanバックエンド](references/other/vulkan.md): Prometeの実験的なVulkanバックエンドを使ってアプリケーションを起動する方法と注意点を解説します。
- [Webブラウザ対応](references/other/web.md): Prometeの実験的なWebバックエンド（Promete.Web）を使って、ゲームをブラウザで動かす方法と注意点を解説します。

### プラグイン

- [ImGUI連携](references/plugins/imgui.md): PrometeでImGUIを利用する方法・主なAPI・サンプル・注意点を解説します。

### エンジンを拡張する

- [カスタムノード](references/extends/nodes.md): Prometeで独自のノードを作成し、シーンに追加する方法を解説します。
- [カスタムノードのレンダリング](references/extends/renderers.md): Prometeでカスタムノードに描画処理を実装する方法を解説します。
- [レンダリングコマンドシステム](references/extends/rendercommand.md): v2のコマンドキューベースのレンダリングアーキテクチャを解説します。
- [カスタムタイルの自作](references/extends/tiles.md): Prometeで独自のタイル（ITile実装）を作成し、Tilemapで利用する方法を解説します。
- [カスタムオーディオソース](references/extends/audio-source.md): Prometeで独自のオーディオソース（IAudioSource実装）を作成し、AudioPlayerで再生する方法を解説します。
- [カスタムグリフソース](references/extends/font.md): Prometeで独自のグリフソース（IGlyphSource実装）を作成し、Fontとして利用する方法を解説します。
- [カスタムバックエンド](references/extends/backend.md): Prometeで独自のバックエンドを作成し、アプリケーションで利用する方法を解説します。

<!-- END GENERATED INDEX -->
