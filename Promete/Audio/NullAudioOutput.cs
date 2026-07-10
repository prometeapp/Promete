namespace Promete.Audio;

/// <summary>
/// 何も出力しない <see cref="IAudioOutput"/> の実装です。テストや音声出力を無効化した環境で使用します。
/// </summary>
public class NullAudioOutput : IAudioOutput
{
    private AudioRenderCallback? _render;

    /// <summary>
    /// 出力を開始します。実際には音声を出力せず、コールバックを保持するのみです。
    /// </summary>
    /// <param name="render">1バッファ分のs interleaved float PCM を生成するコールバック。</param>
    /// <param name="channels">出力するチャンネル数。</param>
    /// <param name="sampleRate">出力するサンプリング周波数。</param>
    /// <param name="bufferSizeInFrames">1回のコールバックで生成するフレーム数。</param>
    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames
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
    /// リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Stop();
    }
}
