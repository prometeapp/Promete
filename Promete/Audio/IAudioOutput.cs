using System;

namespace Promete.Audio;

/// <summary>
/// レンダーコールバックが書き込んだ float PCM を出力デバイスへ送るドライバを表すインターフェースです。
/// 常駐レンダーループ（<see cref="AudioRenderPipeline"/>）が、この抽象を通じて実際の音声出力先（OpenAL 等）とやり取りします。
/// </summary>
public interface IAudioOutput : IDisposable
{
    /// <summary>
    /// このプレイヤーのピッチを取得または設定します。ピッチのリサンプリングは負荷が高いため、
    /// PCM レンダリングではなく出力ドライバ側（OpenAL のソースプロパティ等）で適用されます。
    /// 対応しない実装は、この値を保持するだけで無視して構いません。
    /// </summary>
    /// <value>ピッチ比率の値。デフォルトは 1 です。</value>
    public float Pitch { get; set; }

    /// <summary>
    /// 出力を開始します。ドライバは、次の出力バッファが必要になるたびに <paramref name="render"/> を呼び出します。
    /// </summary>
    /// <param name="render">1バッファ分の interleaved float PCM を生成するコールバック。</param>
    /// <param name="channels">出力するチャンネル数。</param>
    /// <param name="sampleRate">出力を開始する時点のサンプリング周波数。</param>
    /// <param name="bufferSizeInFrames">1回のコールバックで生成するフレーム数。</param>
    /// <param name="getSampleRate">
    /// 現在要求されているサンプリング周波数を取得するコールバック。<c>null</c> の場合、
    /// ドライバは <paramref name="sampleRate"/> が変化しないものとして扱います（サンプルレート変更の再構成を行わないなら省略可）。
    /// </param>
    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames,
        Func<int>? getSampleRate = null
    );

    /// <summary>
    /// 出力を停止します。実装は、駆動用スレッドが完全に終了するまで待機してからリソースを解放する必要があります。
    /// </summary>
    public void Stop();
}
