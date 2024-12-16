## Unreleased
- **Feature(Node): ** Node.Pivot
    - 移動・回転・拡大縮小操作の中心点を設定できるようになります。
- **Enhance(Node): ** `ZIndex` をSetup APIに追加
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
