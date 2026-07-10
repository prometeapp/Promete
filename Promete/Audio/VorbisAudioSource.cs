using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NVorbis;

namespace Promete.Audio;

/// <summary>
/// Ogg Vorbis形式のデータを表すオーディオソースです。
/// </summary>
public class VorbisAudioSource : IAudioSource, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly float[] _store;

    /// <summary>
    /// 指定されたパスからVorbisオーディオソースを初期化します。
    /// </summary>
    public VorbisAudioSource(string path)
        : this(File.OpenRead(path)) { }

    /// <summary>
    /// 指定されたストリームからVorbisオーディオソースを初期化します。
    /// </summary>
    public VorbisAudioSource(Stream stream)
    {
        var reader = new VorbisReader(stream);

        Channels = reader.Channels;
        SampleRate = reader.SampleRate;
        Frames = (int)reader.TotalSamples;
        _store = new float[reader.TotalSamples * reader.Channels];

        // 別スレッドで非同期にデータを読み込む
        Task.Factory.StartNew(() =>
        {
            var temp = new float[1000];
            var loadedSize = 0;
            while (true)
            {
                if (_cts.Token.IsCancellationRequested)
                    break;
                var readSamples = reader.ReadSamples(temp.AsSpan());
                if (readSamples == 0)
                    break;

                for (var i = 0; i < readSamples; i++)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;
                    if (loadedSize >= _store.Length)
                        goto exit;

                    _store[loadedSize++] = temp[i];
                    LoadedSize = loadedSize;
                }
            }

            exit:
            reader.Dispose();
            IsLoadingFinished = true;
        });
    }

    /// <summary>
    /// 読み込まれているサンプルのサイズを取得します。
    /// </summary>
    public int LoadedSize { get; private set; }

    /// <summary>
    /// 全てのサンプルが読み込まれているかどうかを取得します。
    /// </summary>
    public bool IsLoadingFinished { get; private set; }

    /// <summary>
    /// 総フレーム数を取得します。
    /// </summary>
    public int? Frames { get; private set; }

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// サンプリングレートを取得します。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// </summary>
    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
    {
        var totalFrames = _store.Length / Channels;
        var offsetSamples = Math.Clamp(offsetFrames, 0, totalFrames) * Channels;
        var requestedSamples = buffer.Length / Channels * Channels;
        var actualReadSamples = Math.Max(0, Math.Min(requestedSamples, LoadedSize - offsetSamples));

        _store.AsSpan(offsetSamples, actualReadSamples).CopyTo(buffer);

        var filledFrames = actualReadSamples / Channels;
        var isFinished = IsLoadingFinished && offsetFrames + filledFrames >= LoadedSize / Channels;
        return (filledFrames, isFinished);
    }

    /// <summary>
    /// このオブジェクトのリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
