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
using Promete.Audio;

public class MyAudioSource : IAudioSource
{
    public int? Samples => null;
    public int Channels => 2;
    public int Bits => 16;
    public int SampleRate => 44100;

    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
      // 無音を生成する
      for (int i = 0; i < buffer.Length; i++)
      {
          buffer[i] = 0;
      }
      return (buffer.Length, false);
    }
}
```

## 実装すべきメンバー
- `Samples` - 総サンプル数（不明な場合はnull）
- `Channels` - チャンネル数（1=モノラル, 2=ステレオ）
- `Bits` - 1サンプルあたりのビット数（通常16）
- `SampleRate` - サンプリングレート（例: 44100）
- `FillSamples(short[] buffer, int offset)` - サンプルデータを `buffer` に書き込み、読み込んだサンプル数と再生終了フラグを返す

## サンプル：AudioPlayerでの利用

```csharp
var audio = new AudioPlayer();
var source = new MyAudioSource();
audio.Play(source);
```
