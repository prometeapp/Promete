using MeltySynth;
using Promete.Audio;

namespace Promete.MeltySynth;

/// <summary>
/// MeltySynthによるMIDIシンセサイザーを音源とするオーディオソースです。無限に続くストリームとして扱われます。
/// </summary>
public class MeltySynthAudioSource : IAudioSource
{
    private readonly object _mutex;
    private readonly MidiFileSequencer _sequencer;

    private readonly Synthesizer _synthesizer;

    /// <summary>
    /// 指定されたサウンドフォントを用いて、このオーディオソースの新しいインスタンスを初期化します。
    /// </summary>
    public MeltySynthAudioSource(string soundFontPath)
    {
        _synthesizer = new Synthesizer(soundFontPath, SampleRate);
        _sequencer = new MidiFileSequencer(_synthesizer);

        _mutex = new object();
    }

    /// <summary>
    /// 総フレーム数。このソースは無限ストリームであるため、常に <c>null</c> を返します。
    /// </summary>
    public int? Frames => null;

    /// <summary>
    /// チャンネル数。常にステレオ (2ch) です。
    /// </summary>
    public int Channels => 2;

    /// <summary>
    /// サンプリングレート。
    /// </summary>
    public int SampleRate => 44100;

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// このソースはシーク不可であるため、<paramref name="offsetFrames"/> は無視されます。
    /// </summary>
    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
    {
        var frames = buffer.Length / Channels;
        Span<float> left = frames <= 1024 ? stackalloc float[frames] : new float[frames];
        Span<float> right = frames <= 1024 ? stackalloc float[frames] : new float[frames];

        lock (_mutex)
        {
            _sequencer.Render(left, right);
        }

        for (var t = 0; t < frames; t++)
        {
            buffer[t * 2] = left[t];
            buffer[t * 2 + 1] = right[t];
        }

        return (frames, false);
    }

    /// <summary>
    /// 指定されたMIDIファイルの再生を開始します。
    /// </summary>
    public void Play(MidiFile midiFile, bool loop)
    {
        lock (_mutex)
        {
            _sequencer.Play(midiFile, loop);
        }
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    public void Stop()
    {
        lock (_mutex)
        {
            _sequencer.Stop();
        }
    }
}
