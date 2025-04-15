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
-  **Fix(Font):** `Font.GetTextBounds()` メソッドが実際よりも小さいサイズを返す問題を修正

## 0.25.1
* **Enhance:** マウスボタン系イベントで、MouseButtonTypeを取得できるように改善

## 0.25.0
プラグイン取得メソッドの仕様を変更しました。
また、[シーンスタックAPI](https://github.com/prometeapp/Promete/issues/37)を実装しました。

* **BREAKING CHANGE:** `PrometeApp.GetPlugin()` メソッドをnull非許容にし、存在しないプラグインを読もうとすると例外をスローするように
* **Feat:** プラグインの取得を試みる `PrometeApp.TryGetPlugin()` メソッドを追加
* **Feat:** シーンスタックAPIを追加
* **Fix:** VorbisAudioSourceがデータを読み終わると例外をスローする不具合を修正

## 0.24.3
* **Fix:** アプリケーションを終了するまで `VorbisAudioSource` がファイルを開放しない不具合を修正

## 0.24.2
* **Fix:** macOSのHiDPI環境で描画が小さくなる不具合を修正
* **Fix:** macOSのHiDPI環境で画面サイズを切り替えるとおかしくなる不具合を修正

## 0.24.1
* **Fix:** 一部のVorbis音源が正常に読み込まれず、最後まで再生しようとしたときに内部で例外が発生する不具合を修正
