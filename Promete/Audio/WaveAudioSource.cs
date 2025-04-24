using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Promete.Audio;

/// <summary>
/// Waveファイル形式を表すオーディオソースです。
/// </summary>
public class WaveAudioSource : IAudioSource
{
    private readonly int _bits;
    private readonly int _channels;
    private readonly int _sampleRate;

    private readonly short[] _store;

    /// <summary>
    /// 指定されたパスからWaveオーディオソースを初期化します。
    /// </summary>
    public WaveAudioSource(string path)
        : this(File.OpenRead(path))
    {
    }

    /// <summary>
    /// 指定されたストリームからWaveオーディオソースを初期化します。
    /// </summary>
    public WaveAudioSource(Stream stream)
    {
        _store = LoadWave(stream, out _channels, out _bits, out _sampleRate);
    }

    /// <summary>
    /// データの長さを取得します。
    /// </summary>
    public int? Length => _store.Length;

    /// <summary>
    /// 合計サンプル数を取得します。
    /// </summary>
    public int? Samples => _store.Length;

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels => _channels;

    /// <summary>
    /// サンプリングビット数を取得します。
    /// </summary>
    public int Bits => _bits;

    /// <summary>
    /// サンプリングレートを取得します。
    /// </summary>
    public int SampleRate => _sampleRate;

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// </summary>
    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
    {
        var actualReadSize = Math.Min(buffer.Length, _store.Length - offset);
        Buffer.BlockCopy(_store, offset * sizeof(short), buffer, 0, actualReadSize * sizeof(short));
        return (actualReadSize, actualReadSize < buffer.Length);
    }

    private static short[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream);
        // RIFF header
        string riff = new(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new NotSupportedException("Specified stream is not a wave file.");

        var riffChunkSize = reader.ReadInt32();

        string format = new(reader.ReadChars(4));
        if (format != "WAVE")
            throw new NotSupportedException("Specified stream is not a wave file.");

        // WAVE header
        var fmt = "";
        var size = 0;
        while (true)
        {
            fmt = new string(reader.ReadChars(4));
            size = reader.ReadInt32();
            if (fmt == "fmt ")
                break;
            reader.ReadBytes(size);
        }

        reader.ReadInt16();
        var fileChannels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt16();
        int bitsPerSample = reader.ReadInt16();

        // 拡張とかあったりなかったりするらしい
        if (size - 16 > 0)
            reader.ReadBytes(size - 16);

        if (bitsPerSample is not 8 and not 16)
            throw new NotSupportedException("Promete only supports 8bit or 16bit per sample.");

        if (fileChannels is < 1 or > 2)
            throw new NotSupportedException("Promete only supports 1ch or 2ch audio.");

        while (true)
        {
            var data = new string(reader.ReadChars(4));
            size = reader.ReadInt32();
            if (data == "data")
                break;
            reader.ReadBytes(size);
        }

        channels = fileChannels;
        bits = bitsPerSample;
        rate = sampleRate;

        var rawData = reader.ReadBytes(size);

        if (bits != 8) return MemoryMarshal.Cast<byte, short>(rawData).ToArray();

        var shortData = new short[rawData.Length / (bitsPerSample / 8)];
        var span = rawData.AsSpan();

        unchecked
        {
            for (var i = 0; i < shortData.Length; i++) shortData[i] = (short)((span[i] - 128) * 256);
        }

        return shortData;
    }
}
