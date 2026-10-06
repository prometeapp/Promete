---
name: promete-v2-migration
description: Promete v1 で書かれた C# プロジェクトを Promete v2 に移行する。net10.0 化、IWindow から View/Time/App への置き換え、Angle 型、WindowOptions の渡し方、シーン自動登録、フォント・テキスト・オーディオ API の変更、NodeRenderer から Collect() への移行、BackendBase への移行を扱う。「Promete を v2 に上げたい」「Promete 2 へ移行」「upgrade/migrate Promete to v2」などの依頼や、IWindow・Window.DeltaTime・UseRenderer・NodeRendererBase・IFont・GetTextBounds など v1 の API を含むコードのビルドエラーを直すときに使う。
license: MIT
---

# Promete v1 → v2 移行

Promete v1 のプロジェクトを v2 に移行する。v2 では `IWindow` が `IGameView` / `ITimeProvider` / `PrometeApp` に分割され、描画はノードの `Collect()` によるコマンドキュー方式に変わった。そのほか角度・フォント・オーディオの API も変更されている。

## 進め方

1. **現状を把握する**
   - git の作業ツリーに未コミットの変更があれば、続行してよいかユーザーに確認する
   - `.csproj` を読み、次を確認する: Promete のバージョン、`TargetFramework`、`LangVersion`、プラグインパッケージ (`Promete.ImGui` など)、`Silk.NET.*` / `SixLabors.*` の直接参照、Promete を `ProjectReference` で参照しているか
   - 下の「検出パターン」でプロジェクト内を検索し、該当するカテゴリの reference だけを読む
2. **プロジェクトファイルを更新する** (下の「プロジェクトファイル」節)
3. **コードを書き換える**: カテゴリごとに reference を読んで対応する
4. **ビルドする**: エラーがなくなるまで修正を繰り返す。その後、`IWindow` 由来の `[Obsolete]` 警告 (CS0618) がゼロになるまで対応する。シーンが自動登録されなかったことを示す `PROMETE0001` / `PROMETE0002` の警告が出ていないかも確認する
5. **報告する**: 変更内容の要約と、「目視確認が必要な項目」のうちこのプロジェクトに該当するものを伝える

## 検出パターン

左のパターンが見つかったら、右の reference を読む。

| パターン | reference |
| --- | --- |
| `IWindow`、`window.` / `Window.` のメンバー呼び出し、`Render +=` などのイベント購読、`Window.TextureFactory`、`TextureFactory` 型、`WindowOptions` (特に引数付きの `.Run(` / `.Run<…>(` に渡している箇所)、`ImGuiPlugin` の継承、`new Keyboard(` などプラグインの手動生成、`_RawInputContext` | [references/window-time-app.md](references/window-time-app.md) |
| `.Angle =`、`.Angle(`、`Angle +=` / `Angle -=`、`AbsoluteAngle`、`Atan2`、`MathHelper.ToRadian` / `ToDegree`、`.Rotate(` | [references/angle.md](references/angle.md) |
| `SceneWillChange`、`private` / `protected` / `file` なネスト型のシーン、抽象・ジェネリックなシーン、public コンストラクタのないシーン、Promete の `ProjectReference`、エントリアセンブリ以外にあるシーン、`Use<…Scene>()` によるシーンの手動登録 | [references/scenes.md](references/scenes.md) |
| `Font.`、`IFont`、`GetTextBounds`、`GenerateTexture`、`WordWrap`、`RenderTexture()`、`RenderedTexture`、フォント読み込み時の例外の catch、`IGlyphSource` を実装すべき独自フォント | [references/fonts-text.md](references/fonts-text.md) |
| `AudioPlayer`、`PlayOneShot`、`WaveAudioSource` / `VorbisAudioSource` の `Samples` / `Bits` / `Length`、`BufferSize`、`IAudioSource` の実装 | [references/audio.md](references/audio.md) |
| `NodeRendererBase`、`UseRenderer<`、`RenderNode(`、`ContainableNode` の継承、`isTrimmable` / `sortedChildren`、サブピクセル移動するノード、`LoadSpriteSheet` | [references/rendering.md](references/rendering.md) |
| `IWindow` の実装クラス (独自バックエンド)、`IFrameBufferProvider`、`OpenGLDesktopWindow`、`OpenGLTextureFactory`、`GLFrameBufferProvider`、`HeadlessAppExtesion` | [references/backend.md](references/backend.md) |
| `using Promete.Nodes.Renderer`、`GLHelper`、`GLMaskedContainerHelper`、`CoordinateExtension`、`RenderingHelper` | [references/namespaces.md](references/namespaces.md) |

## プロジェクトファイル

- `TargetFramework` を `net10.0` にする
- `LangVersion` を明示している場合は `14` 以上にするか削除する。`Angle` の `.Degrees` / `.Radians` は C# 14 の拡張プロパティで実装されている
- Promete を v2 系の最新版に更新する (`dotnet add package Promete`)。`Promete.ImGui` などのプラグインパッケージも合わせて更新する
- `Silk.NET.*` を直接参照している場合は、パッケージ ID を `Promete.Silk.*` (例: `Promete.Silk.OpenGL`) に揃える。upstream と併用すると同名アセンブリが二重に入る。アセンブリ名と名前空間は `Silk.NET.*` のままなので、`using` の変更は不要
- **推移的に入っていたパッケージが減っている。** csproj に直接参照がなくても、コード中の `using SixLabors.` / `using Silk.NET.` を検索して確認する
  - v1 の Promete は `SixLabors.ImageSharp.Drawing` (`SixLabors.Fonts` を含む) と `Silk.NET` メタパッケージを参照していたため、利用側は直接参照なしにこれらを使えていた
  - v2 で推移的に入るのは `Promete.Silk.{Windowing, Input, OpenGL, OpenAL, Maths, Vulkan, Vulkan.Extensions.KHR, Shaderc}` だけ。`SixLabors.*` は一切入らない
  - `SixLabors.ImageSharp` / `SixLabors.Fonts` / `SixLabors.ImageSharp.Drawing` を使っている場合は、自分で `PackageReference` を追加する
  - 画像の読み込みは Promete 自前の実装になり、対応形式は PNG と BMP のみ。JPEG / GIF などを `Load` している場合は PNG へ変換する
  - 上記以外の Silk.NET のサブシステムを使っている場合は、upstream の `Silk.NET.*` ではなく `Promete.Silk.*` を、Promete と同じバージョン (`2.23.0-prmt.1.0.0`) で追加する
- `Microsoft.Extensions.DependencyInjection` を直接参照している場合は、Promete v2 が要求する 10.x 以上にする。8.x のままだとダウングレード警告 (NU1605) になる
- `global.json` で .NET SDK のバージョンを固定している場合は、.NET 10 SDK に更新する
- Promete を `ProjectReference` で参照している場合は、ソースジェネレータの参照も必要になる。詳細は [references/scenes.md](references/scenes.md)
- 変更後に `dotnet restore` を実行する

## 間違えやすい点

- **v1 の単位の違い**: `Vector.Angle()` / `Vector.Rotate(float)` はラジアン、`Node.Angle` は度数法だった。機械的に `.Degrees` を付けると誤る。ラジアンだった箇所には `.Radians` を使う
- **オーディオのサンプル数**: v1 の `Samples` は全チャンネル合計のサンプル数、v2 の `Frames` は 1 チャンネルあたりのフレーム数。`BufferSize` も同じく単位が変わっている
- **コンパイルが通っても動かない `IWindow` のメンバー**: `IWindow.Run(WindowOptions)` は `NotSupportedException` をスローし、`IWindow.IsVsyncMode` への設定は反映されない。`IWindow` を使い続けず、置き換えきること
- **`Render` イベントのタイミング**: v1 の `window.Render` はシーン描画の後に呼ばれていたが、v2 の `App.Render` はシーンのコマンド収集後・描画の実行前に呼ばれる。GL を直接呼んで重ね描きしていたハンドラは `App.PostRender` に移す ([references/window-time-app.md](references/window-time-app.md))
- **スプライトシートの破棄**: v2 の `LoadSpriteSheet` は全セルが 1 枚のテクスチャを共有する。セルを 1 つ `Dispose` すると全セルが使えなくなる ([references/rendering.md](references/rendering.md))
- **警告を握りつぶさない**: CS0618 を `#pragma warning disable` や `NoWarn` で消さず、API を置き換えて解消する
- **シーンのコンストラクタ**: `IWindow` を受け取っていたシーンは、引数を削除して `View` / `Time` / `App` プロパティを使う。代わりに `IGameView` を注入する必要はない

## 目視確認が必要な項目

ビルドでは検出できない挙動の変更。プロジェクトが該当する API を使っている項目だけを、完了報告でユーザーに伝える。

- **テキスト** (`Text` / `Font` を使っている場合)
  - 禁則処理 (`KinsokuMode.Standard`) が既定で有効になった
  - カーニングが既定で有効になった (`UseKerning = true`)。アンチエイリアスの有無に関わらず適用される
  - `Text.Size` が「文字の送り幅 × 行の高さ」で計算されるようになった。`Pivot` を中央にしたテキストは位置が少しずれることがある
  - 縁取りの描画方式が変わり、太さの見え方が変わることがある
  - `TextRenderingOptions.TextColor` の既定値が白になった
- **オーディオ** (`AudioPlayer` を使っている場合)
  - パンが中央のとき左右それぞれ約 0.707 倍 (約 -3dB) になるため、ステレオ音源が v1 より小さく聞こえることがある
  - `Stop(time)` のフェードアウトは `Gain` を変更しなくなった。v1 ではフェード後に `Gain` が 1 に戻っていたため、`Gain` を 1 以外にしたまま `Stop(time)` していると、次の再生の音量が v1 と変わる
  - `IsPlaying` は `Play` を呼んだ直後から (フェード中も含めて) `true` を返す
- **描画**: ノードの描画位置が既定で整数ピクセルにスナップされる。サブピクセル単位で滑らかに移動・回転・ズームするノードはカクつくことがある (対処は [references/rendering.md](references/rendering.md))
- **数学**
  - `Vector.Dot` / `VectorInt.Dot` が正しい内積を返すようになった。v1 は誤った式で計算していたため、v1 の結果を前提に調整した値があると結果が変わる
  - `Rect.Intersect` / `RectInt.Intersect` で、幅または高さが 0 の矩形は常に重なっていないと判定される
