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

    private readonly short[] _store;

    /// <summary>
    /// 指定されたパスからVorbisオーディオソースを初期化します。
    /// </summary>
    public VorbisAudioSource(string path)
        : this(File.OpenRead(path))
    {
    }

    /// <summary>
    /// 指定されたストリームからVorbisオーディオソースを初期化します。
    /// </summary>
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
    /// 合計サンプル数を取得します。
    /// </summary>
    public int? Samples { get; init; }

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// サンプルビット数を取得します。
    /// </summary>
    public int Bits => 16;

    /// <summary>
    /// サンプリングレートを取得します。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// </summary>
    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
        var actualReadSize = Math.Min(buffer.Length, LoadedSize - offset);
        Buffer.BlockCopy(_store, offset * sizeof(short), buffer, 0, actualReadSize * sizeof(short));
        return (actualReadSize, IsLoadingFinished && actualReadSize < buffer.Length);
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
