using System;
using System.IO;

namespace Promete.Audio;

/// <summary>
/// Waveファイル形式を表すオーディオソースです。
/// </summary>
public class WaveAudioSource : IAudioSource
{
    private const short FormatTagPcm = 1;
    private const short FormatTagIeeeFloat = 3;

    private readonly int _channels;
    private readonly int _sampleRate;

    private readonly float[] _store;

    /// <summary>
    /// 指定されたパスからWaveオーディオソースを初期化します。
    /// </summary>
    public WaveAudioSource(string path)
        : this(File.OpenRead(path)) { }

    /// <summary>
    /// 指定されたストリームからWaveオーディオソースを初期化します。
    /// </summary>
    public WaveAudioSource(Stream stream)
    {
        _store = LoadWave(stream, out _channels, out _sampleRate);
    }

    /// <summary>
    /// 総フレーム数を取得します。
    /// </summary>
    public int? Frames => _store.Length / _channels;

    /// <summary>
    /// チャンネル数を取得します。
    /// </summary>
    public int Channels => _channels;

    /// <summary>
    /// サンプリングレートを取得します。
    /// </summary>
    public int SampleRate => _sampleRate;

    /// <summary>
    /// サンプルデータを指定されたバッファに読み込みます。
    /// </summary>
    public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
    {
        var totalFrames = _store.Length / _channels;
        var offsetSamples = Math.Clamp(offsetFrames, 0, totalFrames) * _channels;
        var requestedSamples = Math.Min(buffer.Length, buffer.Length / _channels * _channels);
        var actualReadSamples = Math.Max(
            0,
            Math.Min(requestedSamples, _store.Length - offsetSamples)
        );

        _store.AsSpan(offsetSamples, actualReadSamples).CopyTo(buffer);

        var filledFrames = actualReadSamples / _channels;
        var isFinished = offsetFrames + filledFrames >= totalFrames;
        return (filledFrames, isFinished);
    }

    private static float[] LoadWave(Stream stream, out int channels, out int rate)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream);
        // RIFF header
        string riff = new(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new NotSupportedException("Specified stream is not a wave file.");

        reader.ReadInt32(); // riffChunkSize

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

        var formatTag = reader.ReadInt16();
        var fileChannels = reader.ReadInt16();
        var sampleRate = reader.ReadInt32();
        reader.ReadInt32(); // byte rate
        reader.ReadInt16(); // block align
        int bitsPerSample = reader.ReadInt16();

        // 拡張とかあったりなかったりするらしい
        if (size - 16 > 0)
            reader.ReadBytes(size - 16);

        if (formatTag is not FormatTagPcm and not FormatTagIeeeFloat)
            throw new NotSupportedException("Promete only supports PCM or IEEE float wave files.");

        if (formatTag == FormatTagIeeeFloat && bitsPerSample != 32)
            throw new NotSupportedException("Promete only supports 32bit IEEE float wave files.");

        if (formatTag == FormatTagPcm && bitsPerSample is not 8 and not 16 and not 24 and not 32)
            throw new NotSupportedException(
                "Promete only supports 8bit, 16bit, 24bit or 32bit PCM wave files."
            );

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
        rate = sampleRate;

        var rawData = reader.ReadBytes(size);

        return formatTag == FormatTagIeeeFloat
            ? DecodeFloat32(rawData)
            : DecodePcm(rawData, bitsPerSample);
    }

    private static float[] DecodeFloat32(byte[] rawData)
    {
        var sampleCount = rawData.Length / 4;
        var result = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
            result[i] = BitConverter.ToSingle(rawData, i * 4);
        return result;
    }

    private static float[] DecodePcm(byte[] rawData, int bitsPerSample)
    {
        var bytesPerSample = bitsPerSample / 8;
        var sampleCount = rawData.Length / bytesPerSample;
        var result = new float[sampleCount];

        switch (bitsPerSample)
        {
            case 8:
                // 8bit waveは符号なし、中心値128
                for (var i = 0; i < sampleCount; i++)
                    result[i] = (rawData[i] - 128) / 128f;
                break;

            case 16:
                for (var i = 0; i < sampleCount; i++)
                {
                    var sample = BitConverter.ToInt16(rawData, i * 2);
                    result[i] = sample / (float)short.MaxValue;
                }
                break;

            case 24:
                for (var i = 0; i < sampleCount; i++)
                {
                    var offset = i * 3;

                    // リトルエンディアンの24bit符号付き整数を、上位バイトへ詰めてから8bit分算術シフトし符号拡張する
                    var sample =
                        (rawData[offset] << 8)
                        | (rawData[offset + 1] << 16)
                        | (rawData[offset + 2] << 24);
                    result[i] = (sample >> 8) / 8388608f;
                }
                break;

            case 32:
                for (var i = 0; i < sampleCount; i++)
                {
                    var sample = BitConverter.ToInt32(rawData, i * 4);
                    result[i] = sample / (float)int.MaxValue;
                }
                break;

            default:
                throw new NotSupportedException(
                    "Promete only supports 8bit, 16bit, 24bit or 32bit PCM wave files."
                );
        }

        return result;
    }
}
