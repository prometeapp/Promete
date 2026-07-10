using System;
using System.Collections.Generic;
using System.Linq;

namespace Promete.Audio;

/// <summary>
/// テスト用の <see cref="IAudioOutput"/> 実装です。実時間には駆動されず、<see cref="RenderNext"/> を
/// 呼び出した回数分だけレンダーコールバックを実行し、生成された PCM を内部に蓄積します。
/// 「バッファをN回生成させる＝時間経過」とみなすことで、決定的なテストが可能になります。
/// </summary>
public class CaptureAudioOutput : IAudioOutput
{
    private readonly List<float[]> _capturedBuffers = [];

    private AudioRenderCallback? _render;
    private int _channels;
    private int _bufferSizeInFrames;
    private bool _isStarted;

    /// <summary>
    /// これまでに生成されたバッファの一覧を取得します。各要素は1回の <see cref="RenderNext"/> 呼び出しで
    /// 生成された interleaved float PCM です。
    /// </summary>
    public IReadOnlyList<float[]> CapturedBuffers => _capturedBuffers;

    /// <summary>
    /// 出力を開始します。実際には音声を出力せず、以後の <see cref="RenderNext"/> 呼び出しに備えて
    /// コールバックとバッファ形状を保持します。
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
    )
    {
        _render = render;
        _channels = channels;
        _bufferSizeInFrames = bufferSizeInFrames;
        _isStarted = true;
    }

    /// <summary>
    /// 出力を停止します。以後 <see cref="RenderNext"/> を呼び出しても何も行われません。
    /// </summary>
    public void Stop()
    {
        _isStarted = false;
        _render = null;
    }

    /// <summary>
    /// レンダーコールバックを指定回数実行し、生成された PCM を <see cref="CapturedBuffers"/> に追加します。
    /// 出力が停止している場合は何も行いません。
    /// </summary>
    /// <param name="bufferCount">レンダーコールバックを実行する回数。</param>
    public void RenderNext(int bufferCount = 1)
    {
        if (!_isStarted || _render is null)
            return;

        for (var i = 0; i < bufferCount; i++)
        {
            var buffer = new float[_bufferSizeInFrames * _channels];
            _render(buffer);
            _capturedBuffers.Add(buffer);
        }
    }

    /// <summary>
    /// これまでに生成された全てのバッファを、生成順に連結した1つの配列として取得します。
    /// </summary>
    /// <returns>連結された interleaved float PCM。</returns>
    public float[] GetAllSamples()
    {
        return _capturedBuffers.SelectMany(x => x).ToArray();
    }

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Stop();
    }
}
