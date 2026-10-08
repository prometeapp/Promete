## 2.1.0

Promete v2.1では、ゲームをWebブラウザ上で動かせる `Promete.Web` パッケージを追加しました（実験的）。

コアには、ブラウザ対応のための拡張点を追加しています。破壊的変更はありません。

### Features

- `Promete.Web` パッケージを追加しました (実験的)
    - .NET の WebAssembly と WebGL2 を用いて、ゲームをWebブラウザ上で動かせます
    - `BuildWithOpenGLDesktop()` の代わりに `BuildWithWeb()` で使用します
    - `<PrometeAsset>` で指定したアセットを起動前に読み込むため、デスクトップ版と同じコードでアセットを読み込めます
    - 既定のHTML・JavaScriptを同梱しており、プロジェクトに置いたファイルで差し替えることもできます
    - Releaseビルドでは、AOTコンパイルが既定で有効になります
    - 対応していない機能や制約は、ドキュメントの「Webブラウザ対応」を参照してください
- フォントの生成方法を、バックエンドが差し替えられるようになりました
    - `IFontProvider` を実装し、`BackendBase.SetupFontProvider()` をオーバーライドして提供します
    - 既定はこれまでどおり FreeType を用います
- 音声の出力先を、バックエンドが差し替えられるようになりました
    - `IAudioProvider` を実装し、`BackendBase.SetupAudioProvider()` をオーバーライドして提供します
    - `AudioPlayer` と `PlayOneShot` は、バックエンドが提供する出力を用います。既定はこれまでどおり OpenAL を用います
- OpenGLバックエンドをリファクタリングしました
    - 複数のOpenGL系バックエンドが存在するため、共通となるベースクラス `GLBackendBase` を追加しました
    - テクスチャ・RenderTexture・シェーダー・画面転送などのGLの部品の組み立てを共通化します
    - `IGLGameView` を実装したゲーム画面と組み合わせ、`BuildWithGLBackend<TBackend>()` でアプリケーションを構築できます
    - `OpenGLDesktopBackend` も `GLBackendBase` を用いるようになりました。挙動は変わりません

### Enhancements

- OpenGL ES (WebGL2) のコンテキストでは、シェーダー (GLSL 3.30) を GLSL ES 3.00 へ自動的に変換するようにしました
- エントリアセンブリを取得できない環境でも、`UseScenesFrom` で指定したアセンブリからシーンを登録できるようにしました
- `PrometeApp.Run` や `OnStart`、`OnUpdate` などのライフサイクルの契約を、XMLドキュメントに明記しました

### Bug Fixes

- シーンのコンストラクタで例外が発生すると、以降のシーン遷移がすべて失敗する不具合を修正しました
    - `LoadScene` では現在のシーンを破棄済みとして扱い、`PushScene` では元のシーンを再開してから例外を再スローします

## 2.0.0

Promete v2では、より高速な描画を実現する新たなレンダリングシステムと、フレームバッファやシェーダーに対する高度なサポートを追加しました。

また、必要なランタイムを .NET 10 へ更新しました。

破壊的変更を伴う、APIの全体的な整理・書き直しを実施しているため、既存プロジェクトには慎重なマイグレーションが必要です。

### Breaking Changes

- `IWindow` インターフェイスを廃止し、`BackendBase` ベースの新しいバックエンドアーキテクチャに移行しました
    - `IWindow` が担っていた複合責務（ウィンドウ管理・時間情報・テクスチャ・イベント）を以下のように分離しました
    - `IGameView`: 画面表示に関するインターフェイス
    - `ITimeProvider`: 時間情報（DeltaTime, TotalTime等）に関するインターフェイス
    - `BackendBase`: バックエンド実装の抽象基底クラス
    - `IWindow` は後方互換のため `[Obsolete]` として残してありますが、新規コードでは `IGameView` / `ITimeProvider` を直接使用してください
- `TextureFactory` を `TextureFactoryBase`（基底クラス）に名称変更しました
- `PrometeApp` にライフサイクルイベント（`PreUpdate`, `PostUpdate`, `PreRender`, `PostRender` 等）を移動しました
- グラフィック描画の仕組みとして、コマンドキューシステムに移行しました
    - テクスチャ・プリミティブ描画、マスクモード有効化など、GPUへの命令を「コマンド」としてカプセル化して扱うレイヤーを新たに導入しました
    - 従来のNode Rendererを廃止しました。Nodeは描画コマンドを直接発行するようになりました
- 角度をfloat値として要求または戻り値とする箇所を `Angle` 構造体に置き換えました
- .NET 10へ移行し、.NET 8のサポートを削除しました。
- ノードの描画位置をデフォルトでピクセル単位にスナップするようになりました
    - `Node.IsPixelSnapEnabled` プロパティ（デフォルト `true`）で制御できます
    - ピボットや位置の端数（例: 奇数サイズ + `Pivot(0.5, 0.5)`）による描画のにじみを防ぎます
    - サブピクセル単位の滑らかな移動・回転・ズーム演出を行うノードでは、`.PixelSnap(false)` で無効化してください
- フォント機能を完全に作り直しました
    - 以前はImageSharpの機能を用いて文字列画像を生成したものを表示していましたが、レイアウトとラスタライズを 直接 Promete が行うようになりました
    - `Font` は、グリフソースとサイズ・スタイルを束ねた不変の値オブジェクトになりました
    - ImageSharp によるテキスト描画を廃止し、FreeType でグリフを供給してグリフアトラスへ集約する方式に変更しました
    - `Text` はレイアウト結果のグリフを個別に発行します。同一アトラスページのグリフは1回の描画命令にまとめられるため、内容を毎フレーム書き換えてもテクスチャの再生成は発生しません
- SixLabors.ImageSharp への依存を廃止し、PNG / BMP の読み込みと PNG の書き出しを Promete 自身で実装しました
    - 画像の読み込みで対応する形式は PNG と BMP のみになりました。JPEG・GIF などを読み込んでいた場合は PNG へ変換するか、別途ライブラリを用いてください
    - PNG は全カラータイプ・ビット深度 (1〜16bit)・Adam7 インターレース・tRNS に、BMP は 1〜32bit・BITFIELDS・トップダウンに対応します。RLE 圧縮の BMP は非対応です
    - `TextureFactoryBase.LoadFromImageSharpImage` と、ImageSharp の `Image` を受け取る `Load9Sliced` オーバーロードを削除しました。独自のテクスチャファクトリでの実装は不要になります
    - `SixLabors.ImageSharp` は推移的にも入らなくなります。利用側で使っている場合は自分で `PackageReference` を追加してください
- `TextureFactoryBase` に抽象メンバー `Update(Texture2D, VectorInt, VectorInt, byte[])` を追加しました
    - グリフアトラスへの部分書き込みに使用します
    - 独自のバックエンドを実装している場合は、このメンバーの実装が必要です
- オーディオシステムを再設計しました
    - `IAudioSource` をfloat32・フレーム単位の契約に変更しました
        - `int? Frames`（総フレーム数。未確定・無限ストリームの場合は`null`）、`Channels`、`SampleRate`、`FillSamples(Span<float> buffer, int offsetFrames)` を実装します
        - `Bits` プロパティを廃止しました
    - `AudioPlayer` は常駐のレンダーループ（pull型）で動作するようになりました。常に音声出力を続け、停止中は無音を出力します
    - 再生はステレオ出力に固定されます
- シーンの自動登録を、実行時のリフレクションからコンパイル時のソースジェネレータ (`Promete.SceneGen`) に移行しました
    - シーンは `internal` 以上の可視性が必要です。生成コードは同一アセンブリのトップレベルクラスに置かれるため、`private` なネスト型は参照できず自動登録の対象外になります (`PROMETE0001` の警告が出ます)
    - コンパイル時に参照が存在しないアセンブリ (実行時に読み込むプラグイン等) のシーンは、原理的に列挙できません
    - エントリアセンブリ以外にシーンを置く場合は、新たに追加した `UseScenesFrom` でそのアセンブリを指定してください
- 内部的に依存するグラフィックスライブラリ Silk.NET を net10.0 および NativeAOT に対応させるため、専用のフォーク (`prometeapp/Silk.NET`) に差し替えました
    - パッケージ ID は `Promete.Silk.*` です。upstream の `Silk.NET.*` は nuget.org でプレフィックスが予約されているため、別 ID で配布しています
    - 推移的依存で使用している場合、 **アセンブリ名と名前空間は `Silk.NET.*` のまま**なので、`using Silk.NET.OpenGL;` のようなコードは変更不要です
    - Silk.NET を直接参照しているプロジェクトは、`PackageReference` の ID を `Promete.Silk.*` に揃えてください。upstream のものと併用すると、同じアセンブリ名が二重に持ち込まれます

### Features

- シェーダーAPIを追加しました
    - `ShaderProgram` クラスを用いて、シェーダーの読み込み・コンパイルを行えるように
    - 生成したシェーダーとユニフォーム値の組み合わせを `Material` クラスで保持できるように
        - `Node.Material` プロパティおよび `PrometeApp.PostProcessMaterials` プロパティで用います
- スクリーン全体に対し、シェーダーを用いてポストプロセスできるようになりました
- `Angle` 構造体を新規追加しました
- `Texture2D`: UV座標を追加しました
- `OpenGLTextureFactory`: `LoadSpriteSheet` で、同じハンドルのUV違いの `Texture2D` を生成するように
- `AudioPlayer`: シーク機能を追加しました
    - `Time` / `TimeInSamples` プロパティに値を設定すると、その位置へシークします
    - 再生していないときに設定した値は、次回再生時の開始位置になります（`Stop()` で 0 にリセット）
    - 範囲外の値は音源の長さの範囲内にクランプされます
- `AudioPlayer`: DSPフィルター機能を追加しました
    - `Filters`（`ObservableCollection<IAudioFilter>`）に追加することで、出力段にDSPエフェクトを適用できます
    - 標準フィルターとして `DelayFilter`（Time/Feedback/Mix）、`LowPassFilter`（CutoffFrequency/Resonance）、`DistortionFilter`（Drive/Level）を `Promete.Audio.Filters` 名前空間に追加しました
    - フィルターは再生停止中・無音中も毎バッファ呼び出され続けるため、ディレイの残響などが自然に鳴り切ります。パイプラインが自動的に `Reset()` を呼ぶことはありません
    - `IAudioFilter` インターフェースを実装することで、独自のDSPフィルターを追加できます
- `AudioPlayer.PlayOneShot` / `PlayOneShotAsync` に `followsMasterGain` 引数を追加しました（デフォルト`false`。`true`にするとプレイヤーの`Gain`を乗算します）
- `WaveAudioSource`: 8/16/24/32bit PCM に加え、32bit float WAV の読み込みに対応しました
- `AudioPlayer` のパンをconstant-powerのソフトウェア実装に変更し、ステレオ音源に対してもパンが効くようになりました
- 複数の `AudioPlayer` を同時に使用できるようになりました（BGM用・SE用など）。内部で `AudioDevice` がALCコンテキストを共有します
- `AudioPlayer.BufferSize` のデフォルト値は1024フレーム（実効レイテンシ約70ms）です
- Trim と NativeAOT に対応しました
    - シーンの自動登録をソースジェネレータに移行し、バックエンドの探索を明示登録に変更したことで、リフレクションに依存しなくなりました
    - `PublishAot` / `PublishTrimmed` は**コマンドラインではなく csproj に記述してください**。グローバルプロパティとして渡すと、netstandard2.0 の `Promete.SceneGen` にも伝播して `NETSDK1207` になります
- Vulkan バックエンドを追加しました (実験的)
    - `BuildWithVulkanDesktop()` で使用します
    - macOS では MoltenVK を同梱しているため、Vulkan SDK の事前インストールは不要です
    - シェーダーの仕様はバックエンド依存です。Vulkan では Vulkan 方言の GLSL が必要で、OpenGL 向けに書いたシェーダーはそのままでは使えません
- フォント周りの機能を追加しました
    - `IGlyphSource`: コードポイントからグリフを供給する抽象。ベクターフォント・ビットマップフォント・外字を同一の枠組みで扱えます
    - `FreeTypeGlyphSource`: 埋め込みビットマップストライクを自動的に利用するため、ドット絵フォントもピクセルパーフェクトに描画できます
    - `BitmapGlyphSource`: 画像ベースのビットマップフォントおよび外字。フォントサイズは元の大きさに対する整数倍として解決し、ドットを保ちます
    - `CompositeGlyphSource`: フォールバックチェーン。外字の差し込みと、収録されていない文字の補完を同じ仕組みで解決します
    - `GlyphAtlas`: ラスタライズ済みグリフを1枚のテクスチャへ集約します
    - `TextLayoutEngine`: 行分割・整列・カーニング・文字間を解決します
- `Font.FromFile` に `faceIndex` 引数を追加し、TTC などのフォントコレクションに対応しました
- 外字に対応しました
    - PTML に終了タグを持たない単独タグを導入し、`<tex=名前>` で外字を差し込めます
    - `INamedGlyphSource` により名前からグリフを引けます。`BitmapGlyphSource` は名前付き登録時に私用領域のコードポイントを自動採番します
- テキストの縁取りに対応しました
    - ラスタライズ後のアルファを膨張させて生成するため、ビットマップフォントや外字にも同じように適用できます
- 禁則処理に対応しました (`KinsokuMode.Standard`)。句読点や閉じ括弧を行頭に、開き括弧を行末に置かないようにします
- `MaxLines` と `Ellipsis` を追加しました。行が溢れた場合は省略記号を末尾に挿入します
- `PrometeApp.PrometeAppBuilder.Use<TPlugin>(TPlugin instance)` を追加し、生成済みのプラグインを登録できるようになりました
    - インスタンス登録はサービスプロバイダ構築前でも解決できるため、バックエンドの初期化中に参照されるプラグインはこの形式で登録します

### Enhancements

- フレームバッファシステムをリファクタリングしました
    - `FrameBuffer` に `AutoRender` / `AutoClear` プロパティと手動レンダリング用の `Render()` メソッドを追加
        - `AutoRender = false` にすると毎フレームの自動レンダリングを抑止し、任意のタイミングで `Render()` を呼び出せます
        - `AutoClear = false` にすると前フレームの内容を保持したままレンダリングできます
- スクリーン全体をFBOでオフスクリーンレンダリングするよう変更しました
- `ConsoleLayer` の行数計算を、二分探索的な測定からメトリクス参照へ変更しました
- 外部のアセンブリからカスタムノードやバックエンドを実装できるよう、次の API を公開しました
    - `Node.ModelMatrix`
    - `Texture2D` と `RenderTexture` のコンストラクタ、`RenderTexture.Texture` の setter
    - `ShaderProgram.SetCompiledData`、`Material.Uniforms`
    - `DrawTextureBatchedCommand`
    - `TextureFactoryBase.LoadFromImageSharpImage` (`protected internal`)

### Bug Fixes

- `Rect.Intersect` / `RectInt.Intersect`: 幅または高さが0の矩形が、隣接する矩形と重なっていると誤判定される不具合を修正しました


## 1.3.2

- fix(Node): Sizeプロパティを変更しても内部のジオメトリマトリックスが変化しない問題を修正
- fix(OpenGLDesktopWindow): TakeScreenshotAsImage()の戻り値を修正し、警告を抑制
- fix(Rect, RectInt): Centerプロパティを追加し、矩形の中心座標を取得できるように

## 1.3.1

- Windowのレンダリングスケールを2以上にしているときに、Containerのトリムモードが正常に動作しない不具合を修正
- Container: トリムモードを有効化したコンテナが入れ子になっていると、親の範囲外に子がはみ出てしまうことがある不具合を修正

## 1.3.0

- PieSprite ノードを追加
    - Spriteノードの亜種
    - 円グラフのように、画像を扇状に描画可能なスプライトです
    - 指定したパーセンテージの分だけ、上から時計回りに切り抜きます
    - 円状のプログレスバーの実装などにご利用いただけます
- MaskedContainer ノードを追加
    - Container ノードの亜種
    - 指定したマスク画像を用いて子要素を切り抜いて描画できます
    - 複雑なUI要素や、スポットライト表現などに利用できます
- 特別なインターフェイスを実装したプラグインの初期化が、ゲーム開始後に行われるように改善
- `Random` クラスの拡張メソッド `NextVector` などに、便利なオーバーロードをいくつか追加しました

## 1.2.2

- 特別なインターフェイス（IInitializable, IUpdatable, IDisposable）を複数実装していても、そのうち1つしかメソッドが呼ばれない不具合を修正

## 1.2.1

- Keyboard.ClipboardText プロパティを追加
    - 現在のクリップボード上の値を取得/設定できます
    - Silk.NET の不具合により、一部環境で非ASCII文字が文字化けします
- 特定のインターフェイスを実装したプラグインが、ゲーム開始やゲームループなどのイベントを購読できるように
    - IInitializable: ゲーム起動時に行う処理を定義可能に
    - IUpdatable: ゲームループ処理を定義可能に
    - IDisposable: 破棄処理を定義可能に
- Promete: Silk.NET, ImageSharp等の依存関係を更新
- Promete.ImGui: ImGui の依存関係を更新

## 1.2.0

本バージョンには起動できない重大な不具合があるため、リリースを停止しました。1.2.1をご利用ください。

## 1.1.0

- シーン無しで起動する機能を追加
- ノードの子要素に自分自身を追加しようとした際に `ArgumentException` 例外をスローするように
- NextFrame に渡したコールバックが次のフレームではなく現在のフレームで実行される問題を修正

## 1.0.1

### :tada: 正式リリース！

- 内容は1.0.1-rc.6 と同一です。
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Feat(Rect, RectInt):** 平行移動した結果を返す `Translate` メソッドを追加しました。
- **Feat(Rect, RectInt):** お互いへのキャストおよび、タプルからのキャストができるようになりました。
- **Enhance(ContainableNode):** レンダリング前に、必要に応じて内部の子要素配列をソートするように改善しました。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。
- **Fix(AudioPlayer):** Timeプロパティが極稀におかしな値を返す不具合を修正しました。
- **Fix(AudioPlayer):** 稀に再生が終わらず終端で短いループが発生する不具合を修正しました。
- **Fix(Node):** 破棄されているノードの更新・描画を行わないよう対策しました。

## 1.0.1-rc.6

### Promete v1 正式リリース！

- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Feat(Rect, RectInt):** 平行移動した結果を返す `Translate` メソッドを追加しました。
- **Feat(Rect, RectInt):** お互いへのキャストおよび、タプルからのキャストができるようになりました。
- **Enhance(ContainableNode):** レンダリング前に、必要に応じて内部の子要素配列をソートするように改善しました。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。
- **Fix(AudioPlayer):** Timeプロパティが極稀におかしな値を返す不具合を修正しました。
- **Fix(AudioPlayer):** 稀に再生が終わらず終端で短いループが発生する不具合を修正しました。
- **Fix(Node):** 破棄されているノードの更新・描画を行わないよう対策しました。

## 1.0.1-rc.5

- Promete v1 正式リリース！
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。
- **Fix(AudioPlayer):** Timeプロパティが極稀におかしな値を返す不具合を修正
- **Fix(AudioPlayer):** 稀に再生が終わらず終端で短いループが発生する不具合を修正
- **Fix(Node):** 破棄されているノードの更新・描画を行わないよう対策

## 1.0.1-rc.4

- Promete v1 正式リリース！
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。
- **Fix(AudioPlayer):** Timeプロパティが極稀におかしな値を返す不具合を修正
- **Fix(AudioPlayer):** 稀に再生が終わらず終端で短いループが発生する不具合を修正

## 1.0.1-rc.3

- Promete v1 正式リリース！
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。
- **Fix(AudioPlayer):** Timeプロパティが極稀におかしな値を返す不具合を修正
- **Fix(AudioPlayer):** 稀に再生が終わらず終端で短いループが発生する不具合を修正

## 1.0.1-rc.2

- Promete v1 正式リリース！
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。
- **Fix:** CoroutineManager プラグインが利用できなくなっていた重大な不具合を修正しました。

## 1.0.1-rc.1

- Promete v1 正式リリース！
- AudioPlayerにいくつかの状態変化を起点に発生する、イベントハンドラーを追加しました。
    - StartPlaying - 音声が再生されたときに発生します。
    - StopPlaying - 音声が停止されたときに発生します。
    - FinishPlaying - ソースが最後まで再生されたときに発生します。
    - Loop - ソースが最後まで再生され、ループされたときに発生します。

## 0.29.0

- **New Feature: フレームバッファ**
    - 子要素をフレームバッファに描画し、テクスチャとして取り出せるようになりました。
    - Prometeのノードの描画内容をそのまま `Sprite` として使えるので、様々な方法に使えそうです
    - Promete.ImGUI と組み合わせると、ImGUI ウィンドウに Promete ノードの描画ができるようになります
    - ゆくゆくは PostProcessing などに応用できるようにする予定です

## 0.28.0

- **BREAKING CHANGE:** `IWIndow` インターフェイスに `TimeScale` `TotalTimeWithoutScale` プロパティを追加しました
    - `TimeScale` は、アプリケーションの時間の進み方を変更するためのプロパティです。ポーズ機能、スローモーション、早送りなどに利用できます。
    - `TotalTimeWithoutScale` は、アプリケーションの時間の進み方を変更せずに、経過した時間を取得するためのプロパティです。アプリケーションの実行時間を取得するために利用できます。

## 0.27.2

- **Fix(Sprite):** ピボットを設定した状態でテクスチャを変更すると、描画位置がおかしくなる不具合を修正

## 0.27.1

- **Fix(AudioPlayer):** `Play` を2回呼び出すと、やっぱり `IsPlaying == false` となる不具合が治っていなかったので、今度こそ、きっと、多分、直した（はず）

## 0.27.0

- **BREAKING CHANGE**: Rect, RectIntそれぞれのRight, Bottomプロパティの挙動が変わりました。
    - 元々、座標にサイズを加算した値を返していましたが、これは矩形の外側を指しており、不正確でした。
    - **このバージョンから、座標にサイズを加算した値から、(1, 1)を引くようにし、内側の右と下を指すようになりました**
    - つまり、以前の挙動よりも1ピクセル左上を指すようになりました。
    - 既存のRect.Right, Rect.Bottomの挙動を維持するためには、Rect.Right + 1, Rect.Bottom + 1 としてください。
    - **また、Vector.In メソッドの判定も1px変わっています。**
- **Fix(AudioPlayer):** `Play` を2回呼び出すと、`IsPlaying == false` となる不具合を修正
- **Fix(AudioPlayer):** `Pause` の後に `Stop` すると、`IsPausing == true` となる不具合を修正
- **Fix(StringExtension)**: ReplaceAt で、文字列の長さを超えるインデックスを指定したときに例外が発生する不具合を修正

## 0.26.1

- **Enhance(Node/Text):** 内部的なテクスチャ生成を、レンダリングの直前に行うよう改善
- **Fix(Node/Text):** テキストを変更したときにPivotの変更が反映されない不具合を修正
- **Feat(Node):** Node.OnPreRender フック
    - ノードがレンダリングされる直前に呼び出されるイベントフックです。描画の下準備などに利用できます。

## 0.26.0

**BREAKING CHANGE**: `IWindow` インターフェイスに `TopMost` プロパティを追加しました。<br/>
本リリースに更新する場合、`IWindow` インターフェイスを実装しているクラスに `TopMost` プロパティを追加する必要があります。
未対応のバックエンドは利用できません。

- **Feature(Node):** Node.Pivot
    - 移動・回転・拡大縮小操作の中心点を設定できるようになります。
- **Feature(Node):** Node.IsVisible
    - 描画の表示・非表示を切り替えることができるようになります。
- **Feature(Windowing):** TopMost プロパティを追加
    - ウィンドウを常に最前面に表示できます。
- **Enhance(Node):** `ZIndex` をSetup APIに追加
    - `node.ZIndex(0)` のように書けるようになります。
- **Fix(VorbisAudioSource):** VorbisAudioSourceのデータ読み込みを高速化
- **Fix(AudioPlayer):** Pan設定が反映されない不具合を修正
    - ただし、OpenALの仕様上、ステレオ音源のPan設定は無視されます

## 0.25.2

- **Fix(AudioPlayer):** macOSで、1024回を超える音声の再生ができない問題を修正
    - この問題を解決するため、一時的にOpenAL Softを強制的に使用しています
    - OpenAL Softによる問題が他のプラットフォームで確認された場合、追加の対応を検討します
- **Fix(Font):** 描画したテキストの右1px、下1pxが見切れる不具合を修正
- **Fix(Font):** `Font.GetTextBounds()` メソッドが実際よりも小さいサイズを返す問題を修正

## 0.25.1

- **Enhance:** マウスボタン系イベントで、MouseButtonTypeを取得できるように改善

## 0.25.0

プラグイン取得メソッドの仕様を変更しました。
また、[シーンスタックAPI](https://github.com/prometeapp/Promete/issues/37)を実装しました。

- **BREAKING CHANGE:** `PrometeApp.GetPlugin()` メソッドをnull非許容にし、存在しないプラグインを読もうとすると例外をスローするように
- **Feat:** プラグインの取得を試みる `PrometeApp.TryGetPlugin()` メソッドを追加
- **Feat:** シーンスタックAPIを追加
- **Fix:** VorbisAudioSourceがデータを読み終わると例外をスローする不具合を修正

## 0.24.3

- **Fix:** アプリケーションを終了するまで `VorbisAudioSource` がファイルを開放しない不具合を修正

## 0.24.2

- **Fix:** macOSのHiDPI環境で描画が小さくなる不具合を修正
- **Fix:** macOSのHiDPI環境で画面サイズを切り替えるとおかしくなる不具合を修正

## 0.24.1

- **Fix:** 一部のVorbis音源が正常に読み込まれず、最後まで再生しようとしたときに内部で例外が発生する不具合を修正
