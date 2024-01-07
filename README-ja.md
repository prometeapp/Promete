# Promete

[![GitHub Releases](https://img.shields.io/github/release-pre/ebiselutica/Promete.svg?style=for-the-badge)][releases]
[![Nuget](https://img.shields.io/nuget/vpre/Promete.svg?style=for-the-badge)](https://www.nuget.org/packages/Promete/)

Promete Engine は、 C# と .NET 8 のための、軽量で汎用的な2Dゲームエンジンです。

[README is also available in English!](README.md)

## ビルドの仕方

```shell
git clone https://github.com/EbiseLutica/Promete
cd Promete
dotnet build
```

## サポートされるプラットフォーム

- Windows
- macOS
- GNU/Linux

## 機能

- 軽快な処理
- 2Dに特化したグラフィックシステム
	- スプライト - 画面上へのテクスチャ表示
	- タイルマップ - テクスチャを敷き詰めたマップ表示
	- グラフィック - 線分や矩形を1ピクセル単位で描画
	- コンテナー - 描画要素を格納できるオブジェクト
	- テキスト - 文字列を描画できるオブジェクト
	- 9スライススプライト - テクスチャを9分割して、矩形状のテクスチャ−をスムーズに引き伸ばせる特殊なスプライト
- キーボード入力
- マウス入力
- スクリーンショット撮影機能
- 動画作成を支援するための、画面を連番画像でキャプチャーする機能
- シーン遷移機能
- 音楽再生
- 効果音再生
- 返り値の取得やエラーハンドリングを備えた Unity ライクなコルーチンシステム
- 高い拡張性
	- 独自の描画機能の追加
	- オーディオ機能の拡張

## ドキュメント

WIP

## コントリビュート

[コントリビュートの手引き](CONTRIBUTING-ja.md) をご確認ください。

[![GitHub issues](https://img.shields.io/github/issues/ebiselutica/promete.svg?style=for-the-badge)][issues]
[![GitHub pull requests](https://img.shields.io/github/issues-pr/ebiselutica/promete.svg?style=for-the-badge)][pulls]

## ライセンス

[![License](https://img.shields.io/github/license/ebiselutica/promete.svg?style=for-the-badge)](LICENSE)

Promete はいくつかのサードパーティソフトウェアに依存しています。ライセンスをご確認ください [THIRD_PARTIES.md](THIRD_PARTIES.md)

[ci]: https://ci.appveyor.com/project/EbiseLutica/Promete
[issues]: //github.com/EbiseLutica/Promete/issues
[pulls]: //github.com/EbiseLutica/Promete/pulls
[releases]: //github.com/EbiseLutica/Promete/releases
