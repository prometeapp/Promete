---
title: オーディオの再生
description: 音声を扱う方法を解説します。
sidebar:
  order: 7
  badge:
    text: WIP
    variant: danger
---

Prometeで音声を再生するには、`AudioPlayer`クラスを使用します。

インスタンス化をするか、プラグインとして登録して使うことができます。
```csharp
// インスタンス化
var player = new AudioPlayer();

// プラグインとして登録
PrometeApp.Create()
  .Use<AudioPlayer>()
  .BuildWithOpenGLDesktop();
```

:::caution[インスタンスを使う場合の注意]
インスタンスを用いる場合、必ずシーンやアプリケーションのライフサイクルに合わせて、適切なタイミングで破棄してください。
```csharp
player.Dispose();
```

しかし、AudioPlayerのDispose()メソッドは、フリーズを伴う重たい処理です。 特段の事情がない場合は、アプリケーション全体で共有するために、プラグインとして登録して使うことをおすすめします。
:::

実際に音声を再生するには、まずオーディオソースを読み込む必要があります。

## オーディオソース
オーディオソースとは、Prometeにおける音声データを表すクラスです。

標準でサポートされているオーディオファイル形式は、以下の通りです。
* Ogg Vorbis `*.ogg`: `VorbisAudioSource`
* Wave `*.wav`: `WaveAudioSource`

独自にオーディオソースのクラスを作成することで、その他の音声形式や、シンセサイザーのような動的な波形生成も可能です。
詳しくは、[オーディオソースの拡張](/guide/extends/custom-audio-source)を参照してください。

### オーディオソースの読み込み
使用するオーディオソースによって、読み込み方法が異なります。
標準のオーディオソースは、以下のように読み込めます。

```csharp
var vorbis = new VorbisAudioSource("path/to/file.ogg");
var wav = new WaveAudioSource("path/to/file.wav");
```

### オーディオソースの再生
オーディオソースを再生するには、`AudioPlayer` クラスの `Play` メソッドを使用します。

```csharp
player.Play(vorbis);
```

また、第二引数には、ループポイントをサンプル位置単位で指定できます。
```csharp
player.Play(vorbis, (int)(vorbis.SampleRate * 2.4f));
```
こうすると、最後まで再生した後、2.4秒目からループ再生します。イントロのある音楽をループする場合に最適です。

## 再生を停止・一時停止する
再生を停止するには、`Stop` メソッドを使用します。
```csharp
player.Stop();
```

秒単位の時間を指定することで、フェードアウトできます。以下の例では、1秒かけてフェードアウトします。
```csharp
player.Stop(1.0f);
```

一時停止するには、`Pause` メソッドを使用します。
```csharp
player.Pause();
```

再開するには、`Resume` メソッドを使用します。
```csharp
player.Resume();
```

## 音量・ピッチ調整
音量を調整するには、`Gain` プロパティを使用します。以下の例では、音量がデフォルトの50%になります。
```csharp
player.Gain = 0.5f;
player.Play(vorbis);
```

ピッチを調整するには、`Pitch` プロパティを使用します。以下の例では、2倍速で再生されます。
```csharp
player.Pitch = 2.0f;
player.Play(vorbis);
```

## ワンショット再生
ソースを再生しつつ、効果音のような比較的短い音を同時再生できます。
```csharp
player.PlayOneShot(wav);
```

