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
- オーディオシステムを再設計しました
    - `IAudioSource` をfloat32・フレーム単位の契約に変更しました
        - `int? Frames`（総フレーム数。未確定・無限ストリームの場合は`null`）、`Channels`、`SampleRate`、`FillSamples(Span<float> buffer, int offsetFrames)` を実装します
        - `Bits` プロパティを廃止しました
    - `AudioPlayer` は常駐のレンダーループ（pull型）で動作するようになりました。常に音声出力を続け、停止中は無音を出力します
    - 再生はステレオ出力に固定されます

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

### Enhancements

- フレームバッファシステムをリファクタリングしました
    - `FrameBuffer` に `AutoRender` / `AutoClear` プロパティと手動レンダリング用の `Render()` メソッドを追加
        - `AutoRender = false` にすると毎フレームの自動レンダリングを抑止し、任意のタイミングで `Render()` を呼び出せます
        - `AutoClear = false` にすると前フレームの内容を保持したままレンダリングできます
- スクリーン全体をFBOでオフスクリーンレンダリングするよう変更しました

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
