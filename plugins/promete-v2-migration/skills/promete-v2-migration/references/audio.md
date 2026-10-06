# オーディオ

`AudioPlayer` の主な API (`Play` / `Stop` / `Pause` / `Resume` / `PlayOneShot` / `Gain` / `Pan` / `Pitch` / `Time` / イベントなど) はそのまま使える。

## 音源クラスのプロパティ

`WaveAudioSource` / `VorbisAudioSource` で次の変更がある。

- `Samples` は `Frames` に変わった
  - v1 の `Samples`: 全チャンネル合計のサンプル数
  - v2 の `Frames`: 1 チャンネルあたりのフレーム数
  - v1 の `Samples` と同じ値が必要な場合は `Frames * Channels` とする
- `Bits` は削除された
- `WaveAudioSource.Length` は削除された

再生時間を計算していた箇所は、`Frames / (float)SampleRate` で秒数になる (v1 の `Samples / Channels / SampleRate` に相当)。

## AudioPlayer.BufferSize

単位がフレーム数に変わり、既定値は 1024 になった。v1 の値 (全チャンネル合計のサンプル数。既定値 10000) をそのまま設定すると、ステレオでは約 2 倍の長さになる。

- v1 の既定値のまま使っていた場合: 設定を削除して v2 の既定値に任せる
- 明示的に設定していた場合: `v1 の値 / チャンネル数` を目安に換算する

レイテンシを調整する目的で `BufferSize` を変えていた場合は、バッファの本数も変わっている点に注意する。v1 はバッファ 2 本、v2 は `BufferCount` 本 (既定 3)。v2 の出力レイテンシはおよそ `BufferSize × BufferCount ÷ サンプリング周波数` になる。

## 挙動の変更

- `Stop(time)` によるフェードアウト: v1 はフェード中に `Gain` 自体を下げ、フェード後に `Gain` を 1 に戻していた。v2 はフェード中も `Gain` を書き換えず、Stop 前の値がそのまま残る
  - v1 でフェード後に `Gain` を戻していたコードは、v2 では不要なので削除してよい
  - `Gain` を 1 以外にしたまま `Stop(time)` し、v1 の「1 に戻る」挙動に頼って次を再生していたコードは、明示的に `Gain = 1` を設定する
- 出力はステレオ固定になり、パンはステレオ音源にも効くようになった。パンが中央のとき左右それぞれ約 0.707 倍 (約 -3dB) になる
- `IsPlaying` は `Play` を呼んだ直後から (フェード中も含めて) `true` を返す

## 独自の音源 (IAudioSource の実装)

`IAudioSource` は「16bit 整数・サンプル単位」の契約から「32bit 浮動小数点・フレーム単位」の契約に変わった。

| v1 | v2 |
| --- | --- |
| `int? Samples` (全チャンネル合計のサンプル数) | `int? Frames` (1 チャンネルあたりのフレーム数。終わりがない場合は `null`) |
| `int Bits` | 削除 |
| `FillSamples(short[] buffer, int offset)` | `FillSamples(Span<float> buffer, int offsetFrames)` |
| 戻り値: 書き込んだ `short` の数と、終端に達したか | 戻り値: 書き込んだフレーム数と、終端に達したか |

`buffer` には -1〜1 の範囲の値を、チャンネルごとにインターリーブして書き込む。16bit 整数の PCM から変換する場合は `sample / (float)short.MaxValue` とする (エンジン同梱の `WaveAudioSource` と同じ式)。

```csharp
// v1
public class MySource : IAudioSource
{
    public int? Samples => _pcm.Length;
    public int Channels => 2;
    public int Bits => 16;
    public int SampleRate => 44100;

    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
        // ...
    }
}

// v2
public class MySource : IAudioSource
{
    private readonly float[] _pcm; // -1〜1 のインターリーブ済みデータ

    public int? Frames => _pcm.Length / Channels;
    public int Channels => 2;
    public int SampleRate => 44100;

    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
    {
        var total = _pcm.Length / Channels;
        var frames = Math.Min(buffer.Length / Channels, Math.Max(0, total - offsetFrames));
        _pcm.AsSpan(offsetFrames * Channels, frames * Channels).CopyTo(buffer);
        return (frames, offsetFrames + frames >= total);
    }
}
```

実装時の注意:

- `buffer` の長さは呼び出しごとに変わる。`buffer.Length` を超えて書き込まない
- `FillSamples` はオーディオスレッドから呼ばれる。ファイル読み込みなど、ブロックする処理を行わない
- 終端でないのに要求より少ないフレーム数を返した場合、残りは無音で埋められる
- 3 チャンネル以上の音源は、`Play` では先頭の 2 チャンネルだけが再生される。`PlayOneShotAsync` では `NotSupportedException` になり、`PlayOneShot` では例外にならず何も再生されない (内部で例外を握りつぶしてデバッグログに出すだけ)
