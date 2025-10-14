---
title: オーディオプレイヤー
description: PrometeのAudioPlayerによる音声再生・主なAPI・サンプル・注意点を解説します。
sidebar:
  order: 1
---

`AudioPlayer`クラスを使ってBGMや効果音などのオーディオ再生ができます。
WAVやOgg Vorbisなどの音源ファイルを再生・一時停止・停止・ループ・フェードアウトなど多彩な制御が可能です。

## 基本の使い方

`AudioPlayer`はインスタンスを生成して利用します。

```csharp
// 初期化
var audio = new AudioPlayer();

// 再生
audio.Play(new VorbisAudioSource("assets/bgm.ogg"));

// 停止
audio.Stop();

// 一時停止・再開
audio.Pause();
audio.Resume();
```

## 主なAPI

- `audio.Play(source, loop)`<br/>音源を再生します（loopはループ開始位置、省略可）
- `audio.Stop(time)`<br/>再生を停止します（timeはフェードアウト秒、省略可）
- `audio.Pause()`<br/>一時停止します
- `audio.Resume()`<br/>一時停止を解除します
- `audio.PlayOneShot(source, gain, pitch, pan)`<br/>効果音などをその場で再生します
- `audio.Gain`<br/>音量（0.0～1.0）
- `audio.Pan`<br/>パン（-1.0～1.0）
- `audio.Pitch`<br/>ピッチ比率
- `audio.IsPlaying`<br/>再生中かどうか
- `audio.IsPausing`<br/>一時停止中かどうか
- `audio.Time`<br/>再生位置（ミリ秒）
- `audio.Length`<br/>音源の長さ（ミリ秒）
- `audio.StartPlaying`<br/>再生開始イベント
- `audio.StopPlaying`<br/>停止イベント
- `audio.Loop`<br/>ループ時イベント
- `audio.FinishPlaying`<br/>再生終了イベント

## サンプル：BGM再生とイベント

```csharp
private readonly AudioPlayer _audio = new();
private VorbisAudioSource _bgm = new("./assets/GB-Action-C02-2.ogg");

public override void OnStart()
{
    _audio.StartPlaying += (_, _) => console.Print($"BGM STARTED");
    _audio.StopPlaying += (_, _) => console.Print($"BGM STOPPED");
    _audio.Loop += (_, _) => console.Print($"BGM LOOP");
    _audio.FinishPlaying += (_, _) => console.Print($"BGM FINISHED");
    _audio.Play(_bgm, 0);
}
```

## ノート

- 再生できるフォーマットはWAV, Ogg Vorbisなどです
- 効果音再生には `PlayOneShot` を使うと便利です
- 再生中に `Stop` を呼ぶと即時またはフェードアウトで停止します
- イベントで再生状態の変化を検知できます
