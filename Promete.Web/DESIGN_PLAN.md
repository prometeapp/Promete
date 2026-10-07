# Promete Web 対応 計画書

`DESIGN_NOTES.md`（PoC の検証結果）を受けて、実装に進むための計画をまとめる。

- 前提: PoC で必要だったコアの変更は、すべて正式な形でコアに入った。PoC（`Promete.Experimental.Wasm`）は削除し、ライブラリ `Promete.Web` と、サンプル `Promete.Web.Example` に置き換えた。サンプルを動かすときは `dotnet publish Promete.Web.Example -c Release`（トリミングが必須。§3.11）。
- 進捗: C7・C8 はコアに入った（#112）。C2・C6、C1（`GLBackendBase`、`IGLGameView`、`BuildWithGLBackend`）も入った。PoC の `WebBackend` は `GLBackendBase` を使う形に書き換え、`InternalsVisibleTo` なしでビルドできる。C3（`IFontProvider` / `BackendBase.SetupFontProvider`）と C4（`IAudioProvider` / `BackendBase.SetupAudioProvider`）も入り、PoC は `CanvasFontProvider` と `WebAudioProvider` を使う。Example の audio のデモ 4 本は、例外なしで起動するようになった（ただし `ogg vorbis.demo` は、`VorbisAudioSource` の読み込みでメインスレッドが約 6 秒止まる。DESIGN_NOTES §3.4。別タスク）。
- 本書の範囲: (1) コアの変更タスク、(2) Silk.NET フォークの変更タスク、(3) Promete.Web の設計、(4) JS と HTML の構成。
- 確認できていないことは「未確認」「要スパイク」と明記する。

## 0. 決定事項と、残っている判断

**決定済み**

1. **2.x では破壊的変更をしない**（semver 準拠）。コアの変更は、§1 の「追加のみ」「修正」に限る。整理系（Silk の型を公開 API から外す、`IGameView` の分離、パッケージ分割など）は v3 に回す。
2. **起動の主導権は C# 側に置く。** ゲームのロジックを C# で書くので、`Main` がゲームを組み立てて `Run` する形にする。JS は薄くし、Promete のコードが変わっても追従しやすいよう、Web 固有のコードを増やさない（§3.11、§4.1）。
3. **開発体験の要件**: MSBuild（`dotnet run` / `dotnet publish`）で動かせてデバッグでき、デプロイで html / js / アセンブリ群が生成されること。**スパイクで、実現できることを確認した**（§3.11）。利用者の `csproj` は `PackageReference` だけで済み、html / js を 1 行も書かずに済む構成まで確認している。

4. **Silk.NET に Web 用のウィンドウ / 入力プラットフォームを足さない**（§2.3）。
5. **パッケージは `Promete.Web` の単一パッケージ。** 既存のバックエンドがプラットフォーム名（`GLDesktop` / `Headless`）で、API も `BuildWithWeb` / `WebBackend` なので、それに揃える。Blazor 向け（`Promete.Web.Blazor`）は、需要が出てから足す（機能モジュールを `main.js` から分けているので、後から足せる。§4.1）。
6. **カスタマイズは 3 段階すべてを正式にサポートする**（§3.11）。そのために、パッケージの JS モジュールが起動の定型を `startPromete(options)` という 1 つの関数として export する。既定の `main.js` はそれを呼ぶだけにし、利用者が自前の `main.js` を書く場合も同じ関数を呼ぶ。互換性を保つ契約は、この関数の引数と、`index.html` 側の約束（canvas の ID など）だけになる。段階 1（設定だけ）の設定項目は、まだ作らない（2026-10-07 判断。既定の html は固定。タイトルと canvas のサイズは、C# の `WindowOptions` / `View` から実行時に反映する）。

---

## 1. コアの変更タスク

### 1.1 判断の前提（公開 API の現状）

破壊的かどうかは、次の公開 API の形に左右される（コードで確認した事実）。

- `BackendBase` は公開の abstract クラス。**abstract メンバーの追加は破壊的**（外部のバックエンド実装が壊れる）。`virtual` の追加は破壊的ではない。v2 は `TextureFactoryBase` に abstract メンバー `Update` を足している（バックエンド開発者向けの移行手順に記載）。
- `IGameView` は公開インターフェース。メンバーを足すと破壊的だが、C# の**既定実装つきメンバー（DIM）**なら破壊的でない。実装は `OpenGLDesktopGameView` / `VulkanDesktopGameView` / `HeadlessGameView`。
- `InputProvider` は公開クラス（名前空間は `Promete.Backends.SilkNetCommon`）で、Silk.NET の `IWindow` を受け取る公開コンストラクターと、`IInputContext` を返す `virtual CreateInput()` を持つ。`Keyboard` / `Mouse` / `Gamepads` のコンストラクターがこれを受け取る。`BackendBase.SetupInputProvider()` の戻り値でもある。
- GL 系: `GLScreenBlitter` / `GLRenderTextureProvider` / `GLShaderFactory` / `GLDrawTextureBatchedCommandRunner` は `internal`。その他の GL ランナー、`GLMaskedContainerHelper`、`GLRenderState`、`GLTextureFactory`、`GLHelper` は `public`。GL ランナーは `IGameView` を受け取るが、中で `OpenGLDesktopGameView` にキャストしている（ランナー 7 つ、`GLMaskedContainerHelper`、`GLScreenBlitter` の計 9 ファイル）。
- `ImGuiPlugin` は `OpenGLDesktopGameView` にパターンマッチして `GL` を取り出している。
- `Font` は sealed のクラスで、`FromFile` / `FromStream` / `GetDefault` は static。
- `AudioPlayer` の既定コンストラクターは `new OpenALAudioOutput(...)`、`PlayOneShot*` は `AudioDevice` と OpenAL を直接使う。`AudioPlayer(IAudioOutput)` は公開。

### 1.2 タスク一覧

各タスクの「種別」: **追加**（非破壊）、**修正**（バグ修正。通常の使い方では挙動が変わらない）、**破壊**（v3 以降）。

**C1. GL 系バックエンドの共通層**（種別: 追加）
- 内容: GL ランナーとヘルパーが、ウィンドウの実装（デスクトップの Silk ウィンドウ / canvas）に依存しないようにする。
- 方法: `IGLGameView : IGameView`（`GL` と `FramebufferSize`）を追加し、ランナー側のキャスト先を置き換える。`GLScreenBlitter` / `GLRenderTextureProvider` / `GLShaderFactory` を公開する（または、`GLBackendBase : BackendBase` のような共通基底を新設して公開する）。
- 破壊性: 追加のみ。ランナーのコンストラクター（`IGameView` を受け取る）は変えない。`OpenGLDesktopGameView` が `IGLGameView` を実装するだけ。
- 注意: 公開すると API として固定される。公開範囲を最小に絞る（共通基底を新設する案なら、内部型は `internal` のまま）。
- 付随: `ImGuiPlugin` のパターンマッチを `IGLGameView` に一般化する（任意）。
- 規模: M。

**C2. GLES 向けシェーダー変換**（種別: 追加）
- 内容: `#version 330 core` を、接続先が OpenGL ES のとき `#version 300 es` + 精度宣言に書き換える。判定は `GL_VERSION` の接頭辞（`OpenGL ES`）。GL インスタンスごとにキャッシュする。
- 方法: 5 ファイル 10 か所の `ShaderSource` 呼び出しを 1 か所（`GLHelper.ShaderSource`）に集約する（PoC で実証済み）。
- 破壊性: なし。デスクトップの挙動は変わらない（`GL_VERSION` の取得が 1 回増えるだけ）。
- 規模: S。

**C3. フォントのグリフソース供給元の差し替え**（種別: 追加）
- 内容: `Font.FromFile` / `FromStream` / `GetDefault` が FreeType とシステムフォントに固定されているのを、環境ごとに差し替えられるようにする。
- 案 A（PoC）: static のフック（`FileSourceFactory` など）。簡単だが、プロセス全体のグローバル状態になる（テストや複数アプリで干渉する）。
- 案 B（推奨）: `BackendBase` に `virtual` のファクトリー（例: `SetupFontProvider()`、既定は FreeType）を追加し、`Font` の static メソッドは `PrometeApp.Current` 経由でそれを参照する。グローバル状態を持たない。
- 破壊性: なし（`virtual` の追加）。
- 規模: M。

**C4. オーディオ出力の選択と `PlayOneShot`**（種別: 追加。ただし挙動の確認が必要）
- 内容: `AudioPlayer()` の既定コンストラクターが OpenAL 固定。バックエンドが提供する出力（`IAudioOutput` の factory）を使うようにし、提供が無ければ従来どおり OpenAL にする。
- `PlayOneShot*` は OpenAL を直接使っている。`IAudioOutput` に既定実装つきのワンショット再生メンバー（DIM。既定は OpenAL 経路）を足し、Web 実装がそれを上書きする。
- 破壊性: なし。デスクトップの既定の挙動は変えない。ワンショットの遅延 / 音質の扱いは、Web 側の実装次第で別途確認が必要。
- 規模: M。

**C5. IGameView の capability**（種別: 追加）
- 内容: ビューが対応する機能（位置、フルスクリーン、スクリーンショット、ファイルドロップ、アンチエイリアス無効化など）を問い合わせられるようにする。方針は「未対応の API は無視される。検知はできる」。
- 方法: `IGameView` に DIM で `Capabilities` を足す（既定は「すべて対応」）。アプリ全体の機能問い合わせ（フォントの機能、オーディオの機能も含む）が必要なら、`PrometeApp.Capabilities` のような集約点も検討する。
- 破壊性: なし（DIM）。
- 論点: 粒度（フラグ列挙か、機能名の文字列か）。
- 規模: S〜M。

**C6. ループ / ライフサイクルの契約の明文化**（種別: 追加。ドキュメントと小さな API）
- 方針（合意済み）: `OnStart` がすぐ戻るバックエンドを許す。**毎フレームの `OnUpdate` より前に、必ず `OnStart` が完了していること**を契約にする。
- 内容: `BackendBase.OnStart` / `PrometeApp.Run` / `OnUpdate` / `OnRender` の契約をドキュメントとテストで固定する。`Run` が戻り値を返しても、ゲームが続いている場合があることを明記する。
- 破壊性: なし。
- 規模: S。

**C7. エントリアセンブリが無い環境への対応**（種別: 修正）
- 内容: ブラウザでは `Assembly.GetEntryAssembly()` が null になる。`UseScenesFrom` が 1 つでも指定されていれば動くようにし、どちらも無いときだけ、対処法つきで例外にする（PoC で実証済み）。
- 規模: S。

**C8. シーン生成失敗からの回復**（種別: 修正）
- 内容: `LoadScene` は先に現在のシーンを破棄するため、新しいシーンの生成（コンストラクター）で例外が起きると、以降の遷移が全滅する（破棄済みシーンの `OnDestroy` で NullReference）。生成を先に行うか、破棄済みを記録して二重破棄を防ぐ。
- 破壊性: 例外時の挙動だけが変わる。Web 固有ではないが、Example のデモ巡回で露呈した。
- 規模: S〜M。

**C9. `LineWidth` > 1 の修正**（種別: 修正。別件）
- 内容: 太い線が引けない（デスクトップの GL 3 でも同じ）。三角形への展開が必要。Web 対応とは独立に進める。
- 規模: M。

**C10. 可視性とパッケージ参照**（種別: 追加。または保留）
- 別パッケージ（`Promete.Web`）から使う型を、`InternalsVisibleTo` ではなく公開 API として整える（C1 に含まれる）。当面は Vulkan と同様に `InternalsVisibleTo` を使ってもよい。
- `Promete.csproj` はデスクトップ向けのネイティブ参照（OpenAL / MoltenVK / Shaderc）を持つ。browser-wasm では公開されない。配布サイズは実測済みで（§3.11。brotli で約 2.7MB）、これらのネイティブは成果物に入っていない（トリミングで、使われない Windowing / Vulkan 等のマネージドのコードも落ちる）。Web 向けに分けたい場合の依存の整理は、堅牢性のためで、サイズのためではない。
- 規模: S（計測のみ）。

### 1.3 破壊的変更の精査（まとめ）

**Web 対応のために必須の変更は、すべて「追加のみ」で実現できる。** 理由は、差し込み口（`IGlyphSource`、`IAudioOutput`、`IInputContext` を返す `InputProvider`、`BackendBase`）がすでに存在し、足りない部分は DIM、virtual、可視性の拡大で埋められるから。

**v3 以降に回す整理系（破壊的）:**
- `InputProvider` / `BackendBase.SetupInputProvider()` から Silk.NET の型（`IWindow`、`IInputContext`）を公開 API から外し、Promete 独自の入力抽象に置き換える。`Keyboard` / `Mouse` / `Gamepads` の依存先も変わる。
- `IGameView` からデスクトップ前提のメンバー（`Location`、`TopMost`、`Mode` など）を別インターフェースに分離する。
- GL の共通層を独立したアセンブリ / 名前空間に整理する（`Promete.Windowing.GLDesktop.GLTextureFactory`、`Promete.GLDesktop.*` など、名前空間が散らばっている）。型の移動は、`TypeForwardedTo` で緩和できるが、ソース互換は壊れる。
- `AudioPlayer` の API 整理（既定コンストラクターの扱い、`PlayOneShot` の位置づけ）。
- `Promete.csproj` のパッケージ分割（コアとバックエンド）。

**判断（決定済み）:** 2.x では破壊的変更をしない。「入力の抽象のテコ入れ」（所感）は破壊的になるので v3 に回し、Web 対応はまず Silk の型のまま実装する（PoC で動作済み）。

### 1.4 推奨する着手順

1. C7、C8（小さな修正。Web とは独立に出せる）
2. C2、C6（小さく、他に依存しない）
3. C1（共通層）。C3、C4 はこの上に載る
4. C5（capability）。Web 側の実装と並行して決める

---

## 2. Silk.NET フォークの変更タスク

### 2.1 現状（確認した事実）

- フォークは `Promete.Silk.*`（net10.0 専用）。OpenGL は core プロファイルのみで、ES / Legacy は削除済み。`CLAUDE.md` に「上流のサブシステムを、頼まれずに再追加しない」とある。
- バインディングは SilkTouch（netstandard2.0 の Roslyn ソースジェネレーター）が生成し、`calli` で呼ぶ。
- パッケージには `build/` と `buildTransitive/` のターゲットを同梱する仕組みがすでにある（`build/props/bindings.props` の `SilkGenerateILLinkTargets`）。
- ウィンドウ / 入力には `IWindowPlatform` / `IInputPlatform` のプラットフォーム抽象がある（GLFW / SDL が実装）。

### 2.2 タスク一覧

**S1. `calli` シグネチャの登録を Silk 側で完結させる**（推奨・必須級）
- 背景: Mono WASM は、`calli` のシグネチャごとのスタブをビルド時に作るが、`calli` は集計対象外。PoC では、アプリ側のツール（`tools/gen-calli-signatures.cs`）が IL から集めて、登録用の P/Invoke を生成した。OpenGL 全体で 47 種類。
- 方針: アプリ側ではなく Silk 側のビルドで解決し、利用者に見えなくする。
  - 案 A: SilkTouch が生成時に、シグネチャ（引数の型）を知っているので、登録用の P/Invoke（C#）と C スタブを同時に生成する。シグネチャの表記は Mono の cookie（`I` / `L` / `F` / `D` / `V`）。
  - 案 B: Silk のビルド後に、PoC のツールを走らせて生成する（コンパイルの前後関係が難しい）。
- 付随: C スタブはパッケージの `buildTransitive` で、`browser-wasm` のときだけ `NativeFileReference` として追加する。トリミングで参照が消えないよう、確実に参照が残る仕組みにする（PoC では、実行されない条件付きの呼び出しを置いた）。
- 規模: M〜L。
- リスク: Mono の cookie の規則（`type_to_c` 相当）を複製することになる。Mono 側のバージョン差に注意。
- 並行して検討: dotnet/runtime 側で `calli` を収集対象にする修正の可能性（上流に既存の課題があるかは**未確認**）。

**S2. browser-wasm での restore / publish の健全性確認**
- 内容: `Promete.Silk.*` が browser-wasm で restore / publish できること、不要なネイティブ（`Ultz.Native.GLFW` など）が成果物に入らないこと、トリミング警告が出ないことを確認する。PoC では、IL のトリミング警告は 0 件で、ネイティブも成果物に入らなかった（§3.11）。ただし、トリミングを無効にすると、Silk.NET.SDL の P/Invoke を WASM のツールが収集しようとしてビルドが失敗する。トリミング有効が前提の構成になっているので、この前提を CI で固定する（`PublishTrimmed=false` を試さない）か、SDL 等の P/Invoke を収集対象にしない手段を調べる。
- 規模: S。

**S3. `SilkPInvokeOverride`（静的リンク）の調査**
- 内容: フォークのビルドには、P/Invoke を静的リンクに切り替える設定（`SilkEnableStaticLinking`、`SilkPInvokeOverride`）がある。Web の `calli` 問題や GL の接続で、活用できるかを調べる。
- 規模: S（調査のみ）。

**S4. CI（任意）**
- browser-wasm でのサンプルのビルドと、ヘッドレスブラウザでの簡易テスト。規模: M。

### 2.3 やらないこと（推奨）

- **Web 用のウィンドウ / 入力プラットフォームを Silk に足さない。**
  - 理由 1: `IWindowPlatform` + `IView` + `IViewProperties` + `IWindow` + `IWindowProperties` で、約 75 のメンバーを実装することになり、デスクトップ前提の項目（モニター、位置、枠、フルスクリーン…）が大半を占める。
  - 理由 2: Promete 自身が `IWindow` から離れ、`BackendBase` に移行した（v2）。Silk のウィンドウ抽象を Web に延命する方向は、Promete の設計と逆向き。
  - 理由 3: フォークの方針（上流のサブシステムを足さない）にも反する。
  - 代わりに、Web の窓口は Promete 側（`Promete.Web`）で持つ。
- Emscripten の GL コンテキスト生成と `GetProcAddress`（`gl_shim.c` に相当）は、当面 `Promete.Web` に置く。他の Silk 利用者が使う見込みが出たら、別パッケージとして Silk 側に切り出す。

---

## 3. Promete.Web の設計

### 3.1 パッケージの構成

`Promete.Web`。含めるもの:

- C#: `WebBackend`、`WebGameView`、`WebTimeProvider`、`WebInputProvider`、`WebAudioOutput`、グリフソース、アセットローダー、JS interop 宣言。
- JS: §4 の ES モジュール群（静的 Web アセットとして配布）。
- ビルドの設定: `buildTransitive` の props / targets（§3.7）。
- ネイティブのシム: `gl_shim.c`（WebGL2 コンテキストの生成と `GetProcAddress`）。
- 拡張メソッド: `BuildWithWeb(WebOptions)`。

### 3.2 利用者のコード

デスクトップと同じ形にする。利用者のプロジェクトは、`csproj` と `Program.cs`（と、ゲームのコード）だけ。

```xml
<!-- MyGame.csproj -->
<Project Sdk="Microsoft.NET.Sdk.WebAssembly">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Promete.Web" Version="2.*" />
    <PrometeAsset Include="assets/**" />   <!-- プリロードするアセットの宣言 -->
  </ItemGroup>
</Project>
```

```csharp
// Program.cs
await PrometeWeb.InitializeAsync();   // パッケージの JS モジュールの読み込みなど (C# 主導)
var app = PrometeApp.Create()
    .Use<Keyboard>().Use<Mouse>().Use<ConsoleLayer>()
    .BuildWithWeb();
return app.Run<MainScene>();          // すぐ戻り、以降は requestAnimationFrame が駆動する
```

- `Main` が `Run` から戻っても、ゲームは続く（C6 の契約）。JS 側は `runMain()`（`Main` が終わってもランタイムを終了しない版）で起動する（§3.11）。
- `index.html` と `main.js` は、パッケージが配る（§4.1）。

### 3.3 ループと時間

- 描画ループは `requestAnimationFrame`。毎フレーム、「入力の取り込み → 更新 → 描画」の順に行う（C6 の契約: 更新の前に必ず `OnStart` が完了している）。
- **更新頻度**: 更新と描画を rAF に 1:1 で結ぶと、リフレッシュレート（60/120/144Hz）で更新頻度が変わる。`TargetUps` に従う**固定タイムステップ**（アキュムレーターで、1 フレームあたりの更新回数に上限を設ける）を、`WebBackend` が持つ。`TargetFps` は rAF 間引きで実現する。
- タブが非表示になると rAF が止まる。復帰時の巨大な delta は丸める（上限を設ける）。
- 起動前の準備（アセットのプリロードなど）が終わるまで、`Run` は呼ばない。

### 3.4 表示（GameView と canvas）

- `WebGameView : IGLGameView`。`Size` / `Scale` / `Title` / `IsFullScreen` を canvas とドキュメントに反映する（PoC では未反映）。
- サイズの決め方（要決定）:
  - 固定: 論理サイズ = canvas のバッキングサイズ。`Scale`（1/2/4/8）は、CSS による拡大で実現し、バッキングストアは論理サイズのまま（ピクセルパーフェクト）。
  - フィット: 親要素に合わせて整数倍で拡大し、余白に背景を出す。
  - DPR: `devicePixelRatio` をどう扱うか（`PixelRatio` に反映するか、無視して CSS 拡大に任せるか）。2D ピクセルアートでは、バッキングを論理サイズに固定して `image-rendering: pixelated` が素直。
- `Resize` イベント: `ResizeObserver`。
- フルスクリーン: Fullscreen API（ユーザー操作が必要）。
- 未対応のもの（`Location`、`TopMost`、`TakeScreenshot` の一部など）は、capability（C5）で「未対応」と答え、呼んでも例外にしない。
- スクリーンショット: `canvas.toBlob` / WebGL の `readPixels`。ダウンロード保存は `SaveScreenshotAsync` の代わりに別 API。

### 3.5 入力

- Silk.NET の `IInputContext` を実装する（PoC で動作済み。v3 の抽象整理までは、この方式）。
- **イベントの取り込みはフレーム境界で行う。** JS が DOM イベントをキューに溜め、.NET が毎フレームの先頭で一括して取り出す（§4.3）。これにより、「同一フレーム内で押して離したキーが消える」問題が、設計上起きなくなる。PoC の暫定対応（離した状態への反映を遅らせる）は不要になる。
- キーボード: `KeyboardEvent.code` を `Key` に変換。`KeyChar` はテキスト入力として扱う。IME は、非表示の `textarea` を使う設計が必要（`OpenVirtualKeyboard` / `CloseVirtualKeyboard` の対応）。要スパイク。
- マウス / タッチ / ペン: Pointer Events に統一し、ポインターキャプチャを使う。canvas の CSS サイズと論理サイズの比で座標を変換する（DPR と `Scale` を考慮）。
- ホイール: 正規化（行 / ページ / ピクセル単位の差を吸収）。
- ゲームパッド: `navigator.getGamepads()` を毎フレームのポーリングで読む。
- クリップボード: Async Clipboard API（ユーザー操作とセキュアコンテキストが必要）。
- カーソル: CSS の `cursor`。

### 3.6 オーディオ

- 出力は `IAudioOutput` の Web 実装。PoC は、毎フレーム JS のキューが約 0.2 秒を下回るたびに 1 バッファを追加する方式で、Ogg Vorbis と WAV の `Play` が動いた。
- 自動再生ポリシー: ユーザー操作まで `AudioContext` は `suspended`。最初の入力で `resume()` する。「クリックして開始」の表示を出すかは、`WebOptions` で選べるようにする（要決定）。
- `PlayOneShot*`: C4 で `IAudioOutput` 側に持たせ、Web 実装が Web Audio のバッファソースで実現する。
- 発展: AudioWorklet 化（別スレッドでのリングバッファ消費）。フレームが重いときの音切れを避けられる。ただし AudioWorklet はセキュアコンテキストが必要で、.NET からバッファを渡す方法（SharedArrayBuffer は COOP/COEP が必要）を要検討。

### 3.7 ビルドと配布

- 利用者の `csproj`: `Microsoft.NET.Sdk.WebAssembly`、`RuntimeIdentifier=browser-wasm`、`Promete.Web` への参照。それ以外は、`buildTransitive` の props / targets で設定する。
- `buildTransitive` が行うこと: `WasmBuildNative=true`、`EmccExtraLDFlags`（WebGL2 / GLES3 / `GetProcAddress`）、`gl_shim.c` の `NativeFileReference`、`calli` シグネチャのスタブ（S1 が完了するまでは、PoC のツールを呼ぶ）、アセットのマニフェスト生成（§4.4）、`ValidateExecutableReferencesMatchSelfContained` の設定が必要な場合の対処。
- **`wasm-tools` ワークロードが必須**（ネイティブのシムを Emscripten でリンクするため）。`dotnet workload install wasm-tools` を、ドキュメントとテンプレートに明記する。ネイティブを一切使わない構成（GL を JS 経由で呼ぶ）は、Silk を使えなくなるので採らない。
- AOT: 現状はインタプリタ。Silk.NET.OpenGL 単体の AOT は動作確認済みだが、Promete 全体の AOT は未検証。性能が必要なら、AOT を実測して判断する（`sample5`: 約 20fps が基準値）。トリミングは、既定で有効、かつ必須（§3.11）。
- 配布サイズ: **実測済み**（トリミング有り、インタプリタ、Release publish）。brotli で約 2.7MB、gzip で約 3.5MB、展開後で約 9.3MB（§3.11）。内訳の最大は、ランタイム（`dotnet.native.wasm`）の 3.0MB と、`System.Private.CoreLib` の 1.6MB。**この 2 つは .NET の WASM ランタイムの固定費で、Promete 側では減らせない**ので、サイズの目標には含めない（Promete + Silk.NET + サードパーティの合計は約 0.6MB で、`.wasm` 全体の約 1 割）。AOT 時のサイズと、起動時間は未計測。
- 依存の整理: Web 向けに不要なもの（Windowing、Vulkan、OpenAL、Shaderc のネイティブ）が、成果物に入らないことを確認する（S2、C10）。

### 3.8 アセットとフォント

- **方針（合意済み）**: アセットは宣言したものを起動前にすべてプリロードして、仮想 FS に置く。同期 API（`Load("assets/x.png")`）は、そこから読む。
- Web 版の**非同期ロード API**を別途用意する（例: `PrometeWebAssets.LoadTextureAsync(url)`）。内部では `fetch` → バイト列 → デコード。`HttpClient` のストリームを直接 `Load(Stream)` に渡すと失敗するため（同期読み不可）、バッファリングしてから渡す。
- プリロードの宣言: `<PrometeAsset Include="assets/**" />` のような MSBuild の項目から、マニフェスト（パス、URL、サイズ、ハッシュ、種類）を生成する（§4.4）。
- 種類ごとの後処理: フォント（`.ttf`）は、ブラウザの `FontFace` への登録（非同期）をプリロード時に済ませる。
- フォント: `IGlyphSource` を Canvas2D で実装（PoC で動作済み）。`Font.FromFile` はパスに対応するファミリー名に結びつけ、`GetDefault` は `sans-serif`。制約は、アンチエイリアスの無効化とカーニングが効かないこと（capability で通知）。
- 性能: グリフごとに `getImageData` を呼ぶ。`GlyphAtlas` がキャッシュするので、文字種の少ないゲームでは問題になりにくいが、**要計測**（`OffscreenCanvas` の検討）。
- 発展: 画像のデコードをブラウザ（`createImageBitmap`）に任せる最適化。ただし、乗算済みアルファの扱いなど、結果が変わる点に注意。

### 3.9 テストと運用

- スモークテスト: `ListDemos` / `OpenDemo` のような JS から呼べる窓口を用意し、ヘッドレスブラウザ（Playwright など）で Example の全デモを 1 本ずつ開いて、フレーム例外を記録する（PoC で実証した方法）。CI に載せるかは要決定。
- 見た目の検証: スクリーンショット比較は、環境差（フォント、GPU）が大きい。範囲を絞る（図形、スプライトなど）。
- エラー処理: フレームで起きた例外は、コンソールに出し、オプションでオーバーレイにも出す。回復できない例外が続く場合は、ループを止める。
- ドキュメント: `Promete.Docs` に Web 版のガイド（セットアップ、制約、capability の一覧）を追加する。

### 3.10 スパイクの結果と、残っている確認事項

**確認できたこと**（詳細は §3.11）

1. ランタイム作成後、`Main` の前に、仮想 FS へ書き込める（`dotnet.create()` が返す `Module.FS`）。
2. `WasmFilesToIncludeInFileSystem` は、`dotnet run` のビルドでファイルが入らなかった（絶対 / 相対パスのどちらでも、ビルド成果物にも無い。原因は未解明）。**使わない**。`Module.FS` への書き込みで足りる。
3. `dotnet run`（Debug）で、ネイティブのリンクを含めて起動できる。パッケージの静的 Web アセット、`buildTransitive` の props / targets、ネイティブのシムも、パッケージ経由で動く。
4. `dotnet publish` で、`index.html` / `main.js` / `_framework/`（アセンブリ）/ 事前圧縮（br / gz）が生成される。

**未確認**

0. （解決済み）Promete を含むアプリが `dotnet run`（Debug ビルド）で動かなかった。Debug ビルドはトリミングをしないので、Silk.NET.SDL の P/Invoke（`GameControllerButtonBind` を返すコールバック）を `ManagedToNativeGenerator` が扱えずにビルドが失敗していた。`Promete.Web` の targets で、P/Invoke の表を作るときだけ Silk.NET.SDL を走査の対象から外して解決した（`PrometeWebPInvokeScanExclude`。アセンブリ自体は配布物に残るが、ブラウザでは読み込まれない）。`dotnet run` での起動と描画、アセットの配信まで確認済み。
1. IDE からのデバッグとステップ実行。
   - **VS Code: 確認済み（2026-10-07）。** サンプルのコードと、Promete のコアのコードの両方で、ブレークポイント、ステップ実行、変数の表示ができた。構成はリポジトリの `.vscode/launch.json` と `.vscode/tasks.json`（F5 でタスクが `dotnet run` を起動し、`App url:` を待ってから Chrome を接続する。デバッグの終了でタスクも止める）。
     - C# Dev Kit の `blazorwasm` は使えない。Blazor 向けで、このアプリでは「Failed to start WASM managed debug session」で失敗する。
     - 代わりに js-debug の `"type": "chrome"` に `inspectUri` を指定する。ただし `inspectUri` は、ブラウザとの接続がポートのときだけ使われる（js-debug の実装で確認）。そのため `"port": 9222` が必須。指定しないと、デバッグプロキシがブラウザに接続できない（`DevToolsProxy.Run: ... Unable to connect to the remote server`）。
     - サンプルの `Properties/launchSettings.json` に `inspectUri` を書き、ポートを 5292 に固定した。
     - 既知の問題: F5 のたびに「タスク 'web-example: run' は終了せず、'problemMatcher' が定義されていません」というダイアログが出る（「このままデバッグ」で続行できる）。問題マッチャーの書き方を変えても解消しなかった。原因は未調査で、保留。
   - Visual Studio / Rider: 未確認。
2. **`dotnet watch` によるホットリロード: 確認済み（2026-10-07）。** 毎フレーム実行されるメソッド（`OnUpdate`）の変更が、約 1.3 秒で、ページの再読み込みもゲームの状態のリセットもなく反映された。一度しか呼ばれないメソッド（`OnStart` など）の変更は、シーンを開き直すまで反映されない（C# のホットリロードの通常の挙動）。デバッガーとの併用は未確認。
3. Promete 全体の AOT の可否、サイズ、起動時間、性能。
4. Canvas2D グリフ描画の性能。
5. IME（非表示 `textarea`）の実装可能性。
6. AudioWorklet で、.NET から PCM を渡す方法。
7. （解決済み）利用者が自前の `index.html` を置いた場合の優先順位: 利用者のものが優先される（§3.11）。

---

### 3.11 開発体験（スパイクで確認した事実）

スパイクは、Promete とは独立した小さなプロジェクト（ネイティブの C シム、パッケージ、アプリ）で行った。

**`dotnet run`（`WasmAppHost`）**
- `Microsoft.NET.Sdk.WebAssembly` の `dotnet run` は、`WasmAppHost` で静的ファイルを配信し、アプリの URL とデバッグ用の URL（`/_framework/debug`）を表示する。
- `wwwroot` の JS の編集は、再ビルドなしで反映される。
- ネイティブのリンクを含む Debug ビルドは、スパイクで約 22 秒（Promete 本体を含まない小さなプロジェクト）。

**デバッグ**
- Debug ビルドでは、Mono のデバッガーコンポーネントが組み込まれる（AOT / publish では無効）。
- 導線は Blazor WebAssembly と同じ方式。Chrome / Edge を `--remote-debugging-port=9222` で起動して `/_framework/debug` を開くと、DevTools から .NET をデバッグできる。IDE からの操作は未検証。

**起動（C# 主導）**
- `dotnet.create()` の後に `runMain()` を呼ぶ。`dotnet.run()` は、`Main` が戻るとランタイムを終了するので、ゲームのような常駐するアプリでは使えない。PoC では `Main` を呼ばずに JS から直接エクスポートを呼んでいたため、これに気づいていなかった。
- `create()` の後・`runMain()` の前に、`Module.FS` で仮想 FS へアセットを書ける。書いたファイルは、`Main` から見える。
- C# の `Main` から、パッケージ配布の JS を `JSHost.ImportAsync` で読み込んで呼べる。URL は、`JSHost.GlobalThis` の `location.href` から組み立てられる。

**パッケージ（NuGet）で提供できるもの**

| 提供するもの | 方法 | 結果 |
| --- | --- | --- |
| ビルド設定（`WasmBuildNative`、`AllowUnsafeBlocks`） | `buildTransitive` の props | 利用者の `csproj` に不要 |
| ネイティブのシム（C） | `buildTransitive` の targets で `NativeFileReference` | 自動でリンクされる |
| JS | 静的 Web アセット（`staticwebassets/`） | `dotnet run` / `publish` で配信 / 出力される |
| 既定の `index.html` / `main.js` | パッケージの targets が、**利用者が自前のものを持たない場合だけ**、ルートの静的 Web アセットとして登録する（下記） | 利用者は html / js を持たなくてよい。持てば、そちらが使われる |

**落とし穴**
- パッケージが自前の `build/<パッケージ ID>.props` を持つと、静的 Web アセットを登録する自動生成の props が上書きされ、JS が 404 になる。自前の props から、`Microsoft.AspNetCore.StaticWebAssets.props`（と `...EndpointsProps`）を明示的に `Import` する必要がある。
  - `Promete.Web` での対処（PR2）: 静的 Web アセットの SDK は、`build/`・`buildMultiTargeting/`・`buildTransitive/` に `<PackageId>.props` を自動生成し、自前の `buildTransitive/Promete.Web.props` とぶつかる（NU5118 で自前のほうが落ちる）。`StaticWebAssetsDisableProject{Build,BuildMultiTargeting,BuildTransitive}PropsFileGeneration` で自動生成を止め、自前の props から `../build/Microsoft.AspNetCore.StaticWebAssets*.props` を読み込む。
- `ProjectReference` から作られるパッケージの依存は、既定で `exclude="Build,Analyzers"` になる。そのままだと、`Promete.Web` だけを参照した利用者にコアの SceneGen（アナライザー）と `buildTransitive` が届かない。`PrivateAssets="none"` で除外をなくす。
- `[JSExport]` / `[JSImport]` を使うコードには `AllowUnsafeBlocks` が必要。パッケージの props で設定できる（上の表）。

**HTML のカスタマイズ（検証済み）**
- **カスタマイズできる。** 利用者が `wwwroot/index.html` を置けば、パッケージの既定ではなく、そちらが使われる。`main.js` も同様で、片方だけ置いても、もう片方は既定が使われる（検証した組み合わせ: 何も置かない / `index.html` だけ置く。`main.js` を置く場合と両方を置く場合は、同じ仕組みなので動くはずだが、組み合わせとしては未検証）。
- 実現方法（SDK のホットリロード用 JS の登録と同じ手法）: パッケージの targets が、利用者が同名のファイルを持たないときだけ、既定のファイルを中間ディレクトリへコピーし、`DefineStaticWebAssets` / `DefineStaticWebAssetEndpoints` でルートに登録する。利用者のプロジェクトの `wwwroot` が存在しなくてもよい（`dotnet run` は、ファイルのない `wwwroot` を必須にしないよう、今回の構成で動いた）。
- **やってはいけない方式**: パッケージの静的 Web アセットを、ルート（`StaticWebAssetBasePath` を `/`）に置く方式は、利用者が同名の `index.html` を置くと、ビルドが失敗する（`GenerateStaticWebAssetsDevelopmentManifest` が、同じパスのアセットの重複で例外）。`Content` に `Link="wwwroot/index.html"` を付けるだけの方式も、実ファイルが無いため、空のファイルが配信されて動かなかった。
- カスタマイズの段階（案）:
  1. **設定だけ**: `WebOptions` や MSBuild のプロパティで、タイトル、canvas の ID、フィットの方式などを変える（html は既定のまま）。
  2. **html を差し替える**: 利用者が `wwwroot/index.html` を置く。`<canvas>` の ID と、`main.js` の読み込みさえ合わせれば、周囲のページは自由。
  3. **起動も差し替える**: 利用者が `wwwroot/main.js` を置く（起動の定型を自分で書く）。パッケージの機能モジュール（`_content/Promete.Web/...`）は、そのまま import して使える。
- 注意: 差し替えると、既定のテンプレートへの追従（Promete 側の更新で、起動の定型が変わった場合）は、利用者の責任になる。差し替えが必要になる要因を、設定（段階 1）で減らすのが望ましい。

**トリミング（検証済み）**
- **トリミングは有効にできる。しかも事実上、有効が必須。** Release の `dotnet publish` は、browser-wasm では、トリミングが**既定で有効**（`_RunILLink` が実行される）。PoC のこれまでの検証（Example の 58 本を含む）は、すべてトリミング済みの成果物で行っていた。
- **IL のトリミング警告は 0 件**（Promete 本体、Silk.NET.OpenGL、Example を含めて）。コアの `DynamicallyAccessedMembers` の注釈と、SceneGen（実行時のリフレクションなし）が効いている。
- **トリミングを無効にするとビルドが失敗する**（`-p:PublishTrimmed=false`）。Silk.NET.SDL の P/Invoke コールバック（`GameControllerButtonBind` を返すもの）を、WASM のツールが収集しようとして、対応できない型で止まる。トリミングが有効だと、使われない SDL / GLFW / Vulkan が事前に削除されるので、表面化しない。つまり、トリミングなしの構成は、現状サポートできない。
- 示唆: 利用者のコードがトリマーに削除されないよう、`PublishTrimmed` を既定のまま保つ。リフレクションで型を探すコード（利用者側）には、トリマー向けの注釈が必要（Promete のシーンは SceneGen で対応済み）。
- 設計への含意: Silk.NET 側に Windowing / SDL / Vulkan が残っていても、トリミングで落ちる限り問題ない。ただし、トリミングに頼らず、Web 向けに不要な依存を、そもそも読み込まない構成にできれば、堅牢になる（C10、S2 の確認事項）。

**配布サイズ（実測。トリミング有り、インタプリタ、Release publish）**
- 転送サイズ（brotli）: 約 **2.7MB**、gzip: 約 3.5MB、展開後: 約 9.3MB。
- 内訳: `dotnet.native.wasm`（ランタイム）3.0MB、`System.Private.CoreLib` 1.6MB、`Promete` 236KB、`Silk.NET.OpenGL` 207KB、`System.Net.Http` 136KB、`Promete.Example` 117KB、`NVorbis` 60KB。マネージドのアセンブリは 36 個で合計 3.1MB。
- アセットは別（Example のアセットは約 7.9MB）。AOT の場合は未計測。
- 内訳の集計（展開後。`.wasm` 部分の合計は約 6.0MB）: ランタイム（`dotnet.native.wasm`）3.0MB + BCL（`System.*` / `Microsoft.*`）2.25MB（うち `System.Private.CoreLib` が 1.6MB）= **固定費が約 5.2MB**。Promete + Silk.NET.* + サードパーティ（`NVorbis`、`FreeTypeSharp`、`ImGui.NET` など）は約 0.6MB。アプリ側のコード（Example、PoC、ImGui プラグイン）は約 0.2MB。
- **判断**: `System.Private.CoreLib`（1.6MB）をはじめ、ランタイムと BCL は、.NET の WASM の固定費で、Promete では減らせない（`.wasm` 全体の約 9 割）。Promete 由来で最適化の余地があるのは、アセットと、Promete 本体 + 依存の約 0.6MB。Web 版のサイズを論じるときは、固定費を除いて見る。

**`dotnet publish` の出力**
- `wwwroot/` に、`index.html`、`main.js`、パッケージ配布の JS、`_framework/`（アセンブリ。`.wasm` 形式で、内容のハッシュが名前に入る）、事前圧縮（br / gz）。利用者のプロジェクトには html / js が無いまま、これが生成された。

**保守性の方針（JS を薄く保つ）**
- JS は、JS にしかできないこと（ブラウザ API の呼び出し）だけを持つ。ゲームのロジック、状態、判断は C# に置く。
- JS の公開面（C# から呼ぶ関数）を小さく、機能ごとのモジュールに分け、型付きの薄い宣言（`JSImport`）だけで C# から使う。
- パッケージの JS と C# は同じバージョンで配布し、組み合わせの不整合を作らない。
- スモークテスト（全デモの巡回）を CI に載せ、Promete 側の変更で壊れていないことを検知する（§3.9）。

---

## 4. JS と HTML の構成

### 4.1 配布物

パッケージが配るもの（静的 Web アセット）:

- `index.html`: 最小のテンプレート。`<canvas>`、読み込み中表示、エラー表示、（任意の）「クリックして開始」。
- `main.js`: 起動の定型（§4.2）。20 行程度に収める。
- 機能ごとの ES モジュール（C# から `JSImport` で呼ぶ。ランタイムに依存しない）:
  - `canvas.js`: canvas のサイズ / DPR / フルスクリーン / カーソル。
  - `input.js`: DOM イベントのキューと、フレーム境界での取り出し。ゲームパッド、IME、クリップボード。
  - `audio.js`: Web Audio への出力（`AudioContext`、キュー、`resume`）。
  - `glyph.js`: Canvas2D のグリフ描画と、`FontFace` の登録。
  - `assets.js`: マニフェストに従った取得と、種類ごとの後処理。

利用者は、これらを書かない（`csproj` と C# だけ。§3.2）。利用者が独自の `index.html` / `main.js` を置いた場合は、そちらが優先される（§3.11）。

**分け方の理由:** 機能モジュールを `main.js`（起動）から切り離しておけば、Blazor WebAssembly に組み込む場合（Blazor は独自の起動スクリプトを持つ）でも、同じモジュールを使える。`Promete.Web.Blazor` を後から足せる。

### 4.2 起動の流れ（C# 主導）

1. ブラウザが `index.html` を開き、`main.js` を読む。
2. `main.js`: `dotnet.create()` でランタイムを作り、同時にアセットの `fetch` を始める（並行して進める）。
3. `main.js`: アセットを `Module.FS` で仮想 FS に書き、フォントを `FontFace` に登録する。
4. `main.js`: `runMain()` で、利用者の `Main` を実行する（ランタイムは終了しない）。
5. C# の `Main`: `await PrometeWeb.InitializeAsync()` で機能モジュールを読み込み、`BuildWithWeb()` → `Run` でゲームを開始する。以降は rAF が駆動する。
6. 進捗は、`main.js` から読み込み表示へ渡す（`onProgress`）。

`main.js` の役割は「ランタイムの作成、アセットの配置、`runMain`」だけで、ゲームのロジックは C#。

### 4.3 JS と .NET の境界（interop）

- .NET → JS: `JSImport`（モジュールごとに名前を分ける）。
- JS → .NET: `JSExport` は最小限（起動の完了通知、フレームの駆動くらい）。
- **入力などのイベントは「JS が溜めて、.NET が引く」**（プル型）。
  - JS は、イベントをリングバッファ（型付き配列）に積む。.NET は、毎フレームの先頭で 1 回、まとめて取り出す。
  - 利点: 任意のタイミングで .NET の関数が呼ばれない（フレーム処理中の再入を避けられる）、interop の呼び出し回数が少ない、フレーム境界のセマンティクスが明確（§3.5）。
  - PoC のプッシュ型（イベントのたびに `JSExport` を呼ぶ）は、上の理由で採らない。
- バイト列の受け渡し: `Span<byte>` は JS では `MemoryView` として届く（`.slice()` でコピーが必要）。大きなデータはコピーを最小限にする。

### 4.4 アセットのマニフェスト

- 生成: ビルド時に MSBuild のターゲットが、`<PrometeAsset>` の項目から `assets/manifest.json`（またはランタイムの設定に埋め込む）を作る。内容は、パス、URL、サイズ、ハッシュ、種類（`font` / `image` / `audio` / `other`）。
- 利用: `assets.js` が、並列に `fetch` して、種類ごとに後処理（フォントは `FontFace`）し、仮想 FS に書く。キャッシュは、ハッシュ付きの URL（またはサーバー側の設定）で効かせる。
- 一括プリロードの限界: 起動時間が、全アセットのサイズに比例する。大きなアセットが多い場合のために、「起動時に必要なもの」と「遅延ロード（非同期 API）」を分けて宣言できるようにする（要設計）。

### 4.5 HTML と CSS

- 最小の構成: 1 つの `<canvas>`（ID は `WebOptions` で指定）、読み込み中 / エラー / 「クリックして開始」の要素。
- サイズの設定: `WebOptions` の `FitMode`（`Fixed` / `Contain` / `Cover` / `FillParent`）に応じて、CSS を適用する。ピクセルアート向けに `image-rendering: pixelated`（または `crisp-edges`）を既定にする。
- マウント先: 既定は ID 指定の canvas。任意の `div` にマウントする方式（`mount` オプション）も用意する（埋め込み用途）。
- 複数インスタンス: 当面は 1 ページ 1 インスタンスとする（グローバルな状態を持つため）。

### 4.6 ホスティングの要件

- `.wasm` に `application/wasm` の MIME タイプ。
- 事前圧縮（brotli / gzip）の配信設定（サイズが大きいため）。
- セキュアコンテキスト（HTTPS または `localhost`）: Async Clipboard、AudioWorklet などで必要。
- COOP / COEP: マルチスレッド（`SharedArrayBuffer`）を使う場合のみ必要。当面は単一スレッドで、不要。
- 開発時: `dotnet run`（`WasmAppHost`）が、静的ファイルの配信とデバッグ用の URL を提供する（§3.11。JS の編集は再ビルド不要）。`dotnet watch` のホットリロードと、VS Code でのステップ実行は確認済み（§3.10）。

---

## 5. 次のステップ

1. §0 の残りの判断（Silk に Web 用のプラットフォームを足さない、パッケージの構成、独自の `index.html` の扱い）。
2. §3.10 の未確認事項のうち、開発体験に直結するもの（IDE デバッグ、`dotnet watch`）を、スパイクで確認する。独自の `index.html` の優先順位は確認済み（§3.11）。トリミングは有効が前提（S2 の CI 固定）。
3. 着手順（§1.4）に沿って、コアの小さな修正（C7、C8、C2、C6）から。
4. S1（Silk 側の `calli` 登録）の設計。それまでは、`tools/gen-calli-signatures.cs` で生成したファイルを `Promete.Web/Generated/` にコミットしてつなぐ。
5. （済）`Promete.Web` の骨組み（PR1: ライブラリと、リポジトリ内のサンプル）。PoC は削除した。（済）PR2（NuGet のパッケージ化）。ローカルのフィードで、利用者の csproj が `PackageReference` と `<PrometeAsset>` だけで動くこと、既定の `index.html` / `main.js`、片方ずつの差し替え、`dotnet run` と Release の publish を確認した。バージョンは `2.1.0-preview.1`（コアの 2.1.0 にそろえる）。公開の CI は、リリースのときに作る。
