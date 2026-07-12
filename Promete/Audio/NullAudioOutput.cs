using System;

namespace Promete.Audio;

/// <summary>
/// 何も出力しない <see cref="IAudioOutput"/> の実装です。テストや音声出力を無効化した環境で使用します。
/// </summary>
public class NullAudioOutput : IAudioOutput
{
    private AudioRenderCallback? _render;

    /// <summary>
    /// このプレイヤーのピッチを取得または設定します。実際の出力には影響しません。
    /// </summary>
    public float Pitch { get; set; } = 1;

    /// <summary>
    /// キュー済み未再生フレーム数。この実装では常に 0 を返します。
    /// </summary>
    public long PendingFrames => 0;

    /// <summary>
    /// 出力を開始します。実際には音声を出力せず、コールバックを保持するのみです。
    /// </summary>
    /// <param name="render">1バッファ分の interleaved float PCM を生成するコールバック。</param>
    /// <param name="channels">出力するチャンネル数。</param>
    /// <param name="sampleRate">出力するサンプリング周波数。</param>
    /// <param name="bufferSizeInFrames">1回のコールバックで生成するフレーム数。</param>
    /// <param name="getSampleRate">現在のサンプリング周波数を取得するコールバック。この実装では使用しません。</param>
    /// <param name="bufferCount">先行キューするバッファ数。この実装では使用しません。</param>
    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames,
        Func<int>? getSampleRate = null,
        int bufferCount = 3
    )
    {
        _render = render;
    }

    /// <summary>
    /// 出力を停止します。
    /// </summary>
    public void Stop()
    {
        _render = null;
    }

    /// <summary>
    /// 未再生バッファの破棄要求。この実装では何もしません。
    /// </summary>
    public void Flush() { }

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Stop();
    }
}
