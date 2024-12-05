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
