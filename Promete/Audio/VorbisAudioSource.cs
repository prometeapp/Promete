using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NVorbis;

namespace Promete.Audio;

/// <summary>
/// An audio source that represents data in Ogg Vorbis format.
/// </summary>
public class VorbisAudioSource : IAudioSource, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly short[] _store;

    public VorbisAudioSource(string path)
        : this(File.OpenRead(path))
    {
    }

    public VorbisAudioSource(Stream stream)
    {
        var reader = new VorbisReader(stream);

        Channels = reader.Channels;
        SampleRate = reader.SampleRate;
        Samples = (int)reader.TotalSamples * reader.Channels;

        var temp = new float[10000];
        _store = new short[reader.TotalSamples * reader.Channels];
        var loadedSize = 0;

        // 別スレッドで非同期にデータを読み込む
        Task.Run(() =>
        {
            unchecked
            {
                while (true)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    // 10000サンプルずつ読み込む
                    var readSamples = reader.ReadSamples(temp.AsSpan());
                    if (readSamples == 0) break;

                    // 各サンプルを16bit shortに変換
                    for (var i = 0; i < readSamples; i++)
                    {
                        if (_cts.Token.IsCancellationRequested) break;
                        if (loadedSize >= _store.Length) break;

                        _store[loadedSize++] = (short)(temp[i] * short.MaxValue);
                        LoadedSize = loadedSize;
                    }
                }
                reader.Dispose();
            }
        });
    }

    /// <summary>
    /// 読み込まれているサンプルのサイズを取得します。
    /// </summary>
    public int LoadedSize { get; private set; }

    /// <summary>
    /// 全てのサンプルが読み込まれているかどうかを取得します。
    /// </summary>
    public bool IsLoadingFinished => LoadedSize == Samples;

    /// <summary>
    /// Get the total number of samples.
    /// </summary>
    /// <returns></returns>
    public int? Samples { get; init; }

    /// <summary>
    /// Get channels.
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// Get sample bits.
    /// </summary>
    public int Bits => 16;

    /// <summary>
    /// Get sample rate.
    /// </summary>
    public int SampleRate { get; init; }

    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
        var actualReadSize = Math.Min(buffer.Length, LoadedSize - offset);
        Buffer.BlockCopy(_store, offset * sizeof(short), buffer, 0, actualReadSize * sizeof(short));
        return (actualReadSize, IsLoadingFinished && actualReadSize < buffer.Length);
    }

    /// <summary>
    /// Dispose this object.
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        GC.SuppressFinalize(this);
    }
}
