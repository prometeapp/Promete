---
title: オーディオフィルター
description: PrometeのAudioPlayerに搭載されたDSPフィルター機能の使い方・標準フィルター・自作フィルターの作り方を解説します。
sidebar:
  order: 3
---

`AudioPlayer`は、再生する音声に対してディレイやローパスなどのDSP（デジタル信号処理）フィルターを適用できます。
フィルターは`audio.Filters`コレクションに追加するだけで有効になり、BGMや効果音にエフェクトをかけられます。

## 基本の使い方

`audio.Filters`は`ObservableCollection<IAudioFilter>`型のプロパティです。`Add`・`Remove`で追加・削除できます。

```csharp
using Promete.Audio;
using Promete.Audio.Filters;

var audio = new AudioPlayer();
var lowPass = new LowPassFilter();

// フィルターを追加すると、次のバッファから効果が反映される
audio.Filters.Add(lowPass);

// パラメータはいつでも変更できる
lowPass.CutoffFrequency = 800;

// フィルターを外す
audio.Filters.Remove(lowPass);
```

`Filters`に複数のフィルターを追加した場合、追加した順番にすべて適用されます。

## 標準フィルター

`Promete.Audio.Filters`名前空間に、以下の3種類の標準フィルターが用意されています。

### DelayFilter（ディレイ）

フィードバック付きのこだま効果を加えます。

- `Time`<br/>ディレイタイム（秒）。デフォルトは0.3秒
- `Feedback`<br/>フィードバック量（0.0～1.0未満）。デフォルトは0.4
- `Mix`<br/>ウェット（ディレイ音）の混合比率（0.0～1.0）。デフォルトは0.5

```csharp
var delay = new DelayFilter { Time = 0.5f, Feedback = 0.5f, Mix = 0.4f };
audio.Filters.Add(delay);
```

### LowPassFilter（ローパス）

指定した周波数より高い成分を減衰させます。

- `CutoffFrequency`<br/>カットオフ周波数（Hz）。デフォルトは1000Hz
- `Resonance`<br/>レゾナンス（Q値）。デフォルトは0.707（バターワース特性）
- `Mix`<br/>ウェット（フィルター適用後の音）の混合比率（0.0～1.0）。デフォルトは1（ウェットのみ）

```csharp
var lowPass = new LowPassFilter { CutoffFrequency = 500f };
audio.Filters.Add(lowPass);
```

`Mix`を0から1へ徐々に変化させると、フィルターなしの状態からフィルターが掛かった状態へスムーズに遷移できます。
ミックス比率に関わらず内部状態は常に更新されているため、遷移中に波形が不連続になることはありません。

```csharp
// 毎フレーム少しずつウェットに寄せていく
lowPass.Mix = Math.Min(1f, lowPass.Mix + 0.02f);
```

### DistortionFilter（ディストーション）

波形をソフトクリップして歪みを加えます。

- `Drive`<br/>増幅量。値が大きいほど歪みが強くなります。デフォルトは2
- `Level`<br/>出力レベル（0.0～1.0）。デフォルトは1

```csharp
var distortion = new DistortionFilter { Drive = 4f, Level = 0.8f };
audio.Filters.Add(distortion);
```

## 停止後も鳴り続ける残響

`AudioPlayer`は常駐のレンダーループを持っており、フィルターは**再生停止中・無音中も含めて毎バッファ呼び出され続けます**。
そのため、`DelayFilter`のような残響を持つフィルターは、`Stop()`を呼んだ後や音源の終端に達した後も、こだまが自然に減衰しながら鳴り切ります。

また、フィルターの内部状態（ディレイラインなど）は自動的にはリセットされません。再生の停止・シーク・別の音源への差し替えを挟んでも、フィルターの状態はそのまま維持されます。
状態を明示的にクリアしたい場合は、任意のタイミングで`Reset()`を呼び出してください。

```csharp
// ディレイの残響を含め、フィルターの状態を即座にクリアする
delay.Reset();
```

## 自作フィルターを作る

`IAudioFilter`インターフェースを実装すると、独自のDSPフィルターを作成できます。

```csharp
using System;
using Promete.Audio.Filters;

/// <summary>
/// バッファ全体の音量を単純に半分にするフィルターです。
/// </summary>
public class HalfGainFilter : IAudioFilter
{
    public void Process(Span<float> buffer, int channels, int sampleRate)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] *= 0.5f;
    }

    public void Reset()
    {
        // 内部状態を持たないフィルターの場合、何もしなくてよい
    }
}
```

```csharp
audio.Filters.Add(new HalfGainFilter());
```

### 実装すべきメンバー

- `Process(Span<float> buffer, int channels, int sampleRate)`<br/>バッファをインプレースで加工します。`buffer`はチャンネルインターリーブ形式のfloat PCM（ステレオの場合はL, R, L, R, ...の順）です
- `Reset()`<br/>ディレイラインなど、フィルターが持つ内部状態を明示的に消去します

### 注意点

- `Process`はオーディオ出力スレッドから呼び出されます。一方でカットオフ周波数などのパラメータプロパティは、ゲームスレッドから随時書き込まれることを想定しています。
  実装はfloatの単発の読み書きのみで完結させ、ロックを取得しないでください（多少の値の食い違いは許容し、単純さを優先する設計です）
- パイプラインが自動的に`Reset`を呼び出すことはありません
