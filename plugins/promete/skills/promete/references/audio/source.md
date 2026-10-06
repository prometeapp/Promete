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

- `Frames`<br/>合計フレーム数（1フレームは全チャンネル分のサンプルをまとめた単位です。未確定・無限ストリームの場合はnull）
- `Channels`<br/>チャンネル数（1=モノラル, 2=ステレオ）
- `SampleRate`<br/>サンプリング周波数（Hz）
- `FillSamples(buffer, offsetFrames)`<br/>チャンネルインターリーブ形式のfloat PCM（範囲は-1.0～1.0）をバッファに読み込む

## サンプル：効果音の再生

```csharp
var se = new WaveAudioSource("assets/se.wav");
audio.PlayOneShot(se);
```
