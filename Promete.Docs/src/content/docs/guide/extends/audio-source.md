---
title: カスタムオーディオソース
description: Prometeで独自のオーディオソース（IAudioSource実装）を作成し、AudioPlayerで再生する方法を解説します。
sidebar:
  order: 4
---

標準の `WaveAudioSource` や `VorbisAudioSource` 以外にも、独自のオーディオソース（`IAudioSource`実装）を作成して利用できます。
ここではカスタムオーディオソースの作り方とAudioPlayerでの利用例を解説します。

## 基本

オーディオソースは `IAudioSource` インターフェースを実装して作成します。
必要に応じてサンプルデータの読み込みや再生制御を実装します。

ここでは、無音を生成するシンプルな例を示します。バッファを埋められれば良いため、理論上あらゆるPCM音源を接続できます。

```csharp
using System;
using Promete.Audio;

public class MyAudioSource : IAudioSource
{
    public int? Frames => null;
    public int Channels => 2;
    public int SampleRate => 44100;

    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
    {
        // 無音を生成する
        buffer.Clear();
        return (buffer.Length / Channels, false);
    }
}
```

## 実装すべきメンバー
- `Frames` - 総フレーム数（1フレームは全チャンネル分のサンプルをまとめた単位。不明・無限ストリームの場合はnull）
- `Channels` - チャンネル数（1=モノラル, 2=ステレオ）
- `SampleRate` - サンプリングレート（例: 44100）
- `FillSamples(Span<float> buffer, int offsetFrames)` - チャンネルインターリーブ形式のfloat PCM（範囲は-1.0～1.0）を `buffer` に書き込み、実際に書き込んだフレーム数と再生終了フラグを返す
    - `offsetFrames` は読み出しを開始するフレーム位置です。シークに対応しないソース（無限ストリームなど）はこの値を無視して構いません
    - `buffer` の長さは「読み込みたいフレーム数 × `Channels`」です。この長さを超えて書き込んではいけません

## サンプル：AudioPlayerでの利用

```csharp
var audio = new AudioPlayer();
var source = new MyAudioSource();
audio.Play(source);
```
