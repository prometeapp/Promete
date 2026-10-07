# Promete Web バックエンド 設計メモ（PoC の検証結果）

`Promete.Experimental.Wasm` での検証結果をもとに、Promete を .NET WebAssembly（ブラウザ）で動かすために、コアへ入れるべき要件と、設計上の論点をまとめる。

- 対象: Promete 2.0.0 / .NET 10（Mono の browser-wasm ランタイム）
- 検証環境: Windows 11、.NET SDK 10.0.401、`wasm-tools` ワークロード、Chromium 系ブラウザ（Claude Code のブラウザペイン）
- 位置づけ: 要件の洗い出しが目的の PoC。実装の品質は問わない。コアに入れた変更も最小限の暫定対応である。

## 1. 結論

- **Promete は WebGL2 上で動く。** GL ランナー群はほぼそのまま使え、`Promete.Example`（58 本のデモ）のうち 48 本が例外なしで動作した。
- ブラウザ側の接続は、ほとんどが「差し込み口がすでにある」形で済んだ。具体的には `IGlyphSource`、`IAudioOutput`、`IInputContext`、`BackendBase` の 4 つ。
- 設計が必要なのは、次の 4 点。
  1. GL 系バックエンドの共通層（ランナーが `OpenGLDesktopBackend` に結合している）
  2. アセットの扱い（同期 API とブラウザの非同期 IO の不一致）
  3. 入力の抽象（Silk.NET の `IInputContext` への依存）
  4. 既定の動作が OpenAL / FreeType / ファイルシステムを前提にしている箇所（`AudioPlayer()`、`PlayOneShot`、`Font.GetDefault` など）
- 当初の問い「WebGPU か」への答え: **今回は WebGL2 を選んだ**。GL ランナーを流用でき、GL 呼び出しが約 40 種類と少ないため。WebGPU は新しいランナー群（WGSL を含む）が必要で、試していない。Blazor のコンポーネント機構は使っていない。.NET の WASM ランタイムと JS interop（`JSExport` / `JSImport`）だけで足りた。

## 2. PoC の構成

プロジェクト: `Promete.Experimental.Wasm/`

- `Web/WebBackend.cs`: `BackendBase` の実装。ブラウザの `requestAnimationFrame` が描画ループを持ち、`OnStart` はすぐに戻る。毎フレーム `Frame()` が呼ばれ、更新と描画を 1 回ずつ行う。
- `Web/WebGameView.cs`: `IGLGameView` の実装（canvas 固定）。
- `Web/WebTimeProvider.cs`、`Web/WebAppExtension.cs`（`BuildWithWeb`）。
- `Web/WebInputContext.cs` ほか: DOM のキーボード・マウスイベントを Silk.NET の `IInputContext` として見せる実装。
- `Web/WebAudioOutput.cs` + `wwwroot/webAudio.js`: Web Audio への `IAudioOutput`。
- `Web/CanvasGlyphSource.cs` + `wwwroot/canvasGlyph.js`: Canvas2D によるグリフ描画の `IGlyphSource`。
- `gl_shim.c`: WebGL2 コンテキストの生成と、GL 関数ポインタの解決（Emscripten）。
- `tools/gen-calli-signatures.cs`（リポジトリ直下の `tools/`）: ビルド時のシグネチャ登録の生成ツール（§4.1）。
- 検証用シーン: `FeatureScene`（描画系）、`TextScene`、`InputScene`、`AudioScene`、`MiscScene`。Example は `?scene=example`（既定）。

ビルドと実行:

```bash
dotnet publish -c Release -o bin/publish   # Promete.Experimental.Wasm ディレクトリで
# bin/publish/wwwroot を静的配信する (.claude/launch.json の wasm-poc は port 5290)
```

- クエリ: `?scene=example|feature|text|input|audio|misc`、`&demo=<パス>`（例: `graphics/font.demo`）、`&f=<機能名>`（feature シーン用）。
- 現在はインタプリタ実行（`RunAOTCompilation=false`）。Release publish で、トリミングは有効のまま。

## 3. 検証結果

### 3.1 描画

動いたもの（目視で確認）:

- 図形（矩形、枠線、三角形）、スプライト（Tint、回転、拡大縮小）、トリム
- ステンシルマスク、アルファマスク、PieSprite、NineSlice、Tilemap
- カスタムシェーダー（セピア、ラスタースクロール）、FrameBuffer、ポストプロセス、RenderTexture

要点:

- GL ランナーが使う GL 関数は、VAO、インスタンシング、FBO、ステンシル、シザー、ブレンドなど、すべて GLES 3.0 / WebGL2 に存在する。
- 非互換だったのはシェーダーの `#version 330 core` だけ。`#version 300 es` と精度宣言に書き換えれば通った（§5 の `GLHelper.ShaderSource`）。Example のカスタムシェーダーも同じ変換で通った。
- 既知の問題: `LineWidth` が 1 より太い線を引けない（デスクトップの GL 3 でも同じ。別途対応）。

### 3.2 テキスト

- Canvas2D のグリフ描画（`IGlyphSource` の差し替え）で、日本語、複数行、混在サイズの `Text` が動く。
- Example の `font.demo` で、美咲ゴシック、JF-Dot-Shinonome14、Koruri の字形が、それぞれのフォントで描画されることを確認した。
- 制約: `isAntialiased: false` は無視される（Canvas2D では切れない）。カーニングは 0。字形の細部（ヒンティング）はブラウザ依存。
- FreeType（ネイティブ）は動かなかった（§4.2）。

### 3.3 入力

- キーボード（押下、イベント、`KeyChar`）、マウス（座標、ボタン、ホイール）が動く。Silk.NET の `IKeyboard` / `IMouse` を実装して `IInputContext` として渡した。
- 発見: ブラウザの入力イベントはフレームと無関係に届く。同一フレーム内で「押して離した」キーは、`IsKeyPressed` のポーリングから消えてしまい、エッジ検出（`IsKeyDown`）を取りこぼす。PoC では、離した状態への反映を次のフレームの更新後まで遅らせて対応した。
- 未対応: ゲームパッド、IME の合成入力、タッチ / ポインター、カーソルの変更、クリップボード。
- マウス座標は、canvas の CSS サイズと論理サイズが一致している前提（DPR、`View.Scale` は未対応）。

### 3.4 オーディオ

- `AudioPlayer(IAudioOutput)` に Web Audio の出力を差し込み、Ogg Vorbis の BGM と WAV の SE（`Play`）が再生された。`AudioContext` が `running`、出力の RMS が 0 でないことを確認した。
- 出力の方式: 毎フレーム、JS 側のキューが約 0.2 秒を下回るたびに、1 バッファ分をレンダリングして追加する（pull 型）。
- 動かなかったもの: `AudioPlayer()`（既定のコンストラクターが OpenAL）、`PlayOneShot` / `PlayOneShotAsync`（出力を差し込んでいても OpenAL を直接使う）。いずれも `PlatformNotSupportedException`。
- 注意: ブラウザの自動再生ポリシーにより、ユーザー操作があるまで `AudioContext` は `suspended` のまま。PoC では最初の入力で `resume()` している。
- 注意: .NET は単一スレッドなので、フレームが重いとアンダーランする。AudioWorklet 化は未検証。
- **`VorbisAudioSource` の読み込みが、メインスレッドを約 6 秒止める**（2026-10-07 に計測）。コンストラクターが `Task.Factory.StartNew` で全体のデコードを始めるが、ブラウザの .NET は単一スレッドなので、この処理はメインスレッドで実行される。ループが途中で制御を返さないため、デコードが終わるまで描画と入力が止まる。
  - 計測: Long Tasks API で、Example の `audio/ogg vorbis.demo`（約 870 万サンプル）の起動直後に 6,071ms のタスクがあった。Vorbis を使わない `audio/wav sfx.demo` では、最大 76ms だった。いずれもインタプリタ実行。
  - 読み込み自体は完了する（固まるだけで、壊れてはいない）。
  - 対処の案（別タスク）: (1) 読み込みのループで、一定サンプルごとに `await Task.Yield()` で制御を返す。デスクトップでは別スレッドのままなので、影響は小さい。(2) `FillSamples` で必要な分だけデコードする（ストリーミング）。変更は大きい。AOT でデコード自体を速くする余地もある。

### 3.5 アセットと IO

- `File` / `Directory` は仮想ファイルシステム（MEMFS）として動く。ページが `fetch` したバイト列を `/assets/...` に書き込んでから起動すれば、既存の同期 API（`Load("assets/x.png")` など）がそのまま動く。
- ネイティブの `WasmFilesToIncludeInFileSystem`（ビルド時にファイルを仮想 FS へ入れる機能）は、今回の指定ではファイルが置かれず、原因は調べていない。
- `HttpClient` による取得自体は動く。ただし、そのストリームを `TextureFactory.Load(Stream)` に渡すと、`net_http_synchronous_reads_not_supported` で失敗する（ブラウザのネットワークストリームは同期読みを許さない）。
- Example のアセット（約 21 ファイル、約 7.9MB）は、起動前に全部取得する方式で動いた。転送サイズと読み込み時間は未計測。

### 3.6 その他

- コルーチン（`WaitForSeconds`、`WaitForTask`）は動く。`App.IsMainThread()` は true。
- `Font.GetDefault()` と `SystemFonts` は、システムフォントが存在しないため失敗する。
- `View.Title` / `View.Size` などは、例外なく受け付けるが、canvas やドキュメントには反映されない。
- シーンの生成に失敗すると、アプリが回復できない（§5.7）。

### 3.7 性能

- 軽いシーンでは約 60fps。
- Example の `sample5`（10000 スプライト）は約 20fps（インタプリタ実行）。GL 側の負荷ではなく、ノードの更新と描画命令の収集という CPU 側が支配的と推測している（未プロファイル）。
- Promete 全体の AOT は未検証。Silk.NET.OpenGL 単体の AOT では、描画が動くことを確認した。

### 3.8 Promete.Example の巡回結果

58 本を 1 本ずつ別ページで開き、3 秒間のフレーム例外を記録した。

- 例外なし: 48 本（graphics 全 16 本、async、coroutine、window、sample1〜9、ptml、debug の一部、input の一部）。
- 失敗: 10 本。
  - OpenAL（`new AudioPlayer()`）: audio ×4、`debug/issue39`、`debug/issue43`
  - ImGui プラグイン未登録: `plugins/imgui`、`experimental/vertex`
  - ブラウザ非対応の処理を使うデモ固有: `debug/text_node_memory_leak`（`Process`）
  - Vulkan 専用: `debug/vulkan_material_slot_leak`
- 「例外なし」は「正しく動く」ではない。目視で確認したのは `font.demo`、`postProcess.demo`、`sample5.demo` のみ。

### 3.9 未検証

- ゲームパッド、`Promete.ImGui`（ネイティブ依存）、`Promete.MeltySynth`、`ConsoleLayer` 以外のプラグイン。
- Promete 全体の AOT、配布サイズ（publish 先が増分で溜まり、正確に測れていない）、読み込み時間。
- モバイル、タッチ、複数ブラウザ。WebGPU。

## 4. 技術的な発見

### 4.1 Mono WASM の `calli`（Silk.NET）

- Silk.NET のバインディングは `calli` でネイティブ関数ポインタを直接呼ぶ。
- Mono の WASM は、ネイティブ関数ポインタを呼ぶためのスタブを、引数の型の並び（シグネチャ）ごとにビルド時に生成する。生成対象のシグネチャは、P/Invoke、`UnmanagedCallersOnly`、`UnmanagedFunctionPointer` から集められるが、`calli` は集められない。
- そのため、そのシグネチャを使う GL 呼び出しは、実行時に `CANNOT HANDLE INTERP ICALL SIG` で落ちる。整数だけの関数（例: `Viewport`）は、既存のシグネチャで偶然通るが、`float` / `double` を含むもの（`ClearColor` など）は落ちる。
- 対処: 登録専用の空の P/Invoke を宣言して、スタブを作らせる。手書きは現実的でないので、`tools/gen-calli-signatures.cs` が IL から `calli` を集めて、P/Invoke と C 関数を生成する（`Silk.NET.OpenGL` 全体で 47 種類）。
- シグネチャは Mono の表記で、`int` / `uint` / enum / ポインタ / bool は `I`、`long` は `L`、`float` は `F`、`double` は `D`、戻り値なしは `V`（例: `glClearColor` は `VFFFF`）。
- **インタプリタでも AOT でも、この登録は必須。** 一度「AOT だと登録しても落ちる」と判断したが、これは増分ビルドの食い違い（古い中間成果物）が原因で、クリーンビルドでは動いた。AOT 切替などで挙動がおかしいときは `obj/.../wasm` を消す。
- 設計上の論点: このツールをフォーク（Silk）側のビルド、または Promete の MSBuild ターゲットに置くか。

### 4.2 FreeType

- `FreeTypeSharp` は browser-wasm 向けのネイティブを同梱していない（win / linux / osx / ios / android のみ）。
- Emscripten のポート（`-sUSE_FREETYPE=1`）を試した。`.NET` 同梱の Emscripten キャッシュは書き込めないため、`-p:WasmCachePath=<書き込み可能なコピー>` が必要。ポート自体は取得とビルドができたが、リンクで `__wasm_setjmp`（setjmp のランタイム）が未定義になり、解決できなかった。
- 結論: FreeType の WASM ビルドは現状困難。Canvas2D の glyph source（`IGlyphSource`）が現実的で、そもそも FreeType への依存を `IGlyphSource` で分離していたことが効いた。

### 4.3 その他

- `Span<byte>` を `JSImport` に渡すと、JS 側では `Uint8Array` ではなく `MemoryView` というラッパーで届く。`.slice()` でコピーを取る。
- ブラウザの JS 起動では、`Assembly.GetEntryAssembly()` が null になる。
- `dotnet run <file>.cs`（file-based app）を csproj のあるディレクトリで実行すると、終わらずにプロセスが増殖する。作業ディレクトリを `tools/` に固定する必要がある。
- `Promete.Example`（実行可能プロジェクト）をライブラリとして参照するには、`ValidateExecutableReferencesMatchSelfContained=false` が必要（NETSDK1150）。
- Emscripten のキャッシュ: `WasmCachePath` を別ディレクトリにすると、`EM_FROZEN_CACHE=0` に切り替わる（公式の仕組み）。

## 5. コアへの要件と設計の論点

方針の欄は、これまでの議論で合意した内容。

### 5.1 GL 系バックエンドの共通化

- 現状: GL ランナー群（7 つ）、`GLMaskedContainerHelper`、`GLScreenBlitter` が `OpenGLDesktopGameView` を直接キャストしている。`GLScreenBlitter` / `GLRenderTextureProvider` / `GLShaderFactory` が `internal`。
- PoC の暫定対応: `IGLGameView`（`GL` と `FramebufferSize` だけ）を追加し、キャスト先をそれに置き換えた。
- 要件: 「GL 系バックエンド共通層」の設計。候補は、共通層（テクスチャ、RenderTexture、スクリーンブリット、シェーダー）をバックエンド非依存の形で公開し、各バックエンドが「コンテキスト、ウィンドウ、入力、時間、ループ」だけを実装する形。
- シェーダーの GLSL 変換は、GLES 系バックエンド共通の責務として扱う（§5.8）。

### 5.2 ループとライフサイクル

- 方針: `OnStart` がすぐ戻る形で問題ない。ただし、**毎フレームの `OnUpdate` より前に必ず `OnStart` が呼ばれていること**を保証する。
- 要件: `Run` の意味（ブロッキング前提か）を整理する。`PrometeApp.OnUpdate` / `OnRender` を、バックエンドが呼ぶ公開 API として位置づける。
- 論点（要検討）: 更新と描画が rAF に 1:1 で結びつくため、ディスプレイのリフレッシュレート（60 / 120 / 144Hz）で更新頻度が変わる。`TargetUps` / `TargetFps` は現状無視している。固定タイムステップの扱いを決める必要がある（実装から導いた論点で、実測はしていない）。

### 5.3 アセット

- 背景: OpenGL コンテキストのように非同期が許されない環境が主軸なので、同期 API の前提は崩しにくい。
- 方針:
  - アセットは、宣言したものを**起動前にすべてプリロード**して仮想 FS に置く。同期 API は、そこから参照する。
  - Web 版には、非同期ロード API を別途用意して、開発しやすくする。
- 要件:
  - プリロードの宣言方法（マニフェスト）と、アセットの種類ごとの後処理の仕組み。たとえばフォント（`.ttf`）は、ブラウザ側の `FontFace` への登録が非同期のため、プリロード時に済ませる必要がある（PoC では JS 側で、パス名をファミリー名にして登録した）。
  - `ImageDecoder.Decode(Stream)` など、同期読みを前提にしたストリーム処理の見直し（ネットワークストリームでは失敗する）。
  - Web 版の非同期ロード API の設計。

### 5.4 テキスト / フォント

- 方針: Canvas2D が速度面で問題なければ、Web 用の glyph source として採用する。
- PoC で入れたコア変更: `Font` に差し替えフック（`FileSourceFactory`、`StreamSourceFactory`、`DefaultFontFactory`）を追加した。既定は従来どおり FreeType。
- 要件:
  - 既定フォントの決め方（Web は `sans-serif`。環境で字形が変わる）。
  - `FromStream`（バイト列から）は、`FontFace` の非同期登録が必要なので、PoC では未対応。
  - 速度: グリフごとに `getImageData` を呼ぶ。`GlyphAtlas` がキャッシュするが、性能は未計測。
  - capability: アンチエイリアスの無効化、カーニングなど、実現できない機能の検知。

### 5.5 オーディオ

- 要件:
  - `AudioPlayer()` の既定コンストラクターが OpenAL 固定。出力を**バックエンドが提供する**（`IAudioOutput` の factory を登録する）形にしないと、既存コードが動かない。
  - `PlayOneShot` が OpenAL を直接使う。`IAudioOutput` の抽象に含める必要がある。
  - 自動再生ポリシーへの対応（ユーザー操作まで `suspended`）。
  - 単一スレッドでの pull 型のため、レイテンシとアンダーランの設計。AudioWorklet 化は今後の課題。
  - 「別スレッドで重い処理をする」前提のコード（`VorbisAudioSource` の読み込みなど）が、Web ではメインスレッドを塞ぐ（§3.4）。

### 5.6 入力

- 方針: `IInputContext`（Silk.NET の型）は**テコ入れが必要**。
- 要件:
  - Silk.NET の `IWindow` / `IInputContext` に結合した `InputProvider` の抽象化。
  - フレームとイベントのずれ（§3.3）の扱い。ポーリング型を残すなら、押下状態をフレーム境界で確定するルールを、仕様として明文化する。
  - ゲームパッド、IME、タッチ / ポインター、クリップボード、カーソル。
  - 座標: canvas の CSS サイズと DPR、`View.Scale`、`PixelRatio` の関係。

### 5.7 IGameView と堅牢性

- 方針: 未対応の API は**無視される仕様**でよい。ただし、検知はできるようにする（capability）。
- 要件:
  - `Size` / `Scale` / `Title` / `IsFullScreen` / `Mode` が canvas やドキュメントに反映されない。`Resize` イベント、DPR、`TakeScreenshot`、`SaveScreenshotAsync`、`FileDropped` が未対応。`Location` / `TopMost` はブラウザでは無意味。
  - capability の設計: どの機能が使えるかを、アプリ側が問い合わせられる仕組み。
  - **シーン生成の失敗からの回復**（Web 固有ではない）: `LoadScene` は先に現在のシーンを破棄するため、新しいシーンのコンストラクターが例外を投げると、次の遷移で破棄済みシーンの `OnDestroy` が NullReference になり、以降の遷移が全滅する。

### 5.8 ビルドと配布

- シグネチャ登録の生成（§4.1）の置き場所。
- AOT: Silk.NET.OpenGL 単体では動く。Promete 全体（Vulkan、Windowing、ImGui を含む）は重く未検証。性能面で必要かを、実測して判断する。
- 依存の整理: Web 向けには、Windowing、Vulkan、OpenAL、Shaderc のネイティブ参照が不要。`Promete.csproj` の参照を分けるか、条件付きにするかの検討。
- ホスティング: JS 資産（`canvasGlyph.js`、`webAudio.js`、入力の配線）、アセット、`dotnet.js` の配置の形。

### 5.9 既知の描画上の問題

- `LineWidth` > 1（デスクトップ GL 3 でも発生。別途対応）。

## 6. PoC でコアに入れた変更

テストは 339 件すべて合格（`Promete.Test`）。デスクトップ側の挙動は変えていない。

- `Backends/GL/IGLGameView.cs`（新規）と、`OpenGLDesktopGameView` への実装、GL ランナー 7 つ + `GLMaskedContainerHelper` + `GLScreenBlitter` のキャスト置き換え。
- `Graphics/Rendering/GL/GLHelper.cs`: `IsOpenGlEs`、`ShaderSource`（ES のとき `#version 330 core` を `300 es` に書き換え）。5 ファイル 10 箇所の `ShaderSource` 呼び出しを置き換えた。
- `PrometeApp.RegisterAllScenes`: エントリアセンブリが取れなくても、`UseScenesFrom` があれば動く。
- `Graphics/Fonts/Font.cs`: 差し替えフック 3 つ（§5.4）。
- `Promete.csproj`: `InternalsVisibleTo`（`Promete.Experimental.Wasm`）。

## 7. 次のステップ（案）

1. §5 の方針が決まっていない項目（固定タイムステップ、入力の抽象、capability の形、`AudioPlayer` の出力の注入）の設計。
2. GL 系バックエンド共通層の設計と、`OpenGLDesktopBackend` のリファクタ。
3. Web 版の非同期ロード API とプリロード宣言の仕様。
4. 性能の実測（AOT の有無、プロファイル）、配布サイズと読み込み時間の計測。
5. ゲームパッド、IME、タッチ、`MeltySynth`、WebGPU の検討。
