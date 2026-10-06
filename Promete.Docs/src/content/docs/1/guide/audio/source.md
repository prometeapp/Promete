---
title: オーディオソース
description: PrometeのIAudioSourceによる音源データの扱い・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 2
---

オーディオソースは、Prometeにおける音源データを扱う概念です。

WAV形式であれば `WaveAudioSource` クラス、Ogg Vorbis形式であれば `VorbisAudioSource` クラスが対応しており、インスタンス生成時に音声ファイルを読み込みます。

読み込んだオーディオソースは `AudioPlayer` クラスで再生できます。

標準でサポートされている形式は以下の通りです：
- WAV（WaveAudioSource）
- Ogg Vorbis（VorbisAudioSource）

必要に応じて独自のオーディオソースを実装することも可能です。

## 基本の使い方

```csharp
// WAVファイルの読み込み
var wav = new WaveAudioSource("assets/se.wav");

// Ogg Vorbisファイルの読み込み
var ogg = new VorbisAudioSource("assets/bgm.ogg");

// AudioPlayerで再生
var audio = new AudioPlayer();
audio.Play(ogg);
```

## 主なAPI

- `Samples`<br/>合計サンプル数（未指定の場合はnull）
- `Channels`<br/>チャンネル数（1=モノラル, 2=ステレオ）
- `Bits`<br/>量子化ビット数（8または16）
- `SampleRate`<br/>サンプリング周波数（Hz）
- `FillSamples(buffer, offset)`<br/>サンプルデータをバッファに読み込む

## サンプル：効果音の再生

```csharp
var se = new WaveAudioSource("assets/se.wav");
audio.PlayOneShot(se);
```
