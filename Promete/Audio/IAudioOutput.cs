using System;

namespace Promete.Audio;

/// <summary>
/// レンダーコールバックが書き込んだ float PCM を出力デバイスへ送るドライバを表すインターフェースです。
/// フェーズ3で実装される常駐レンダーループが、この抽象を通じて実際の音声出力先（OpenAL 等）とやり取りします。
/// </summary>
public interface IAudioOutput : IDisposable
{
    /// <summary>
    /// 出力を開始します。ドライバは、次の出力バッファが必要になるたびに <paramref name="render"/> を呼び出します。
    /// </summary>
    /// <param name="render">1バッファ分の interleaved float PCM を生成するコールバック。</param>
    /// <param name="channels">出力するチャンネル数。</param>
    /// <param name="sampleRate">出力するサンプリング周波数。</param>
    /// <param name="bufferSizeInFrames">1回のコールバックで生成するフレーム数。</param>
    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames
    );

    /// <summary>
    /// 出力を停止します。
    /// </summary>
    public void Stop();
}
