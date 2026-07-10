using System.IO;
using FluentAssertions;
using Promete.Audio;

namespace Promete.Test;

/// <summary>
/// <see cref="IAudioSource"/> の各実装（<see cref="WaveAudioSource"/>, <see cref="VorbisAudioSource"/>）の契約を検証するテストです。
/// </summary>
public class AudioSourceTests
{
    // Wave フォーマット読み込みテスト

    [Fact]
    public void Wave_8bit_LoadsExpectedValues()
    {
        // 8bit unsigned PCM: 0, 64, 128, 192, 255 という階段波
        var wave = LoadWave(Write8BitWave([0, 64, 128, 192, 255], 1, 8000));

        wave.Channels.Should().Be(1);
        wave.SampleRate.Should().Be(8000);
        wave.Frames.Should().Be(5);

        var buffer = new float[5];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(5);
        isFinished.Should().BeTrue();
        buffer[0].Should().BeApproximately(-1f, 0.01f);
        buffer[1].Should().BeApproximately(-64 / 128f, 0.01f);
        buffer[2].Should().BeApproximately(0f, 0.01f);
        buffer[3].Should().BeApproximately(64 / 128f, 0.01f);
        buffer[4].Should().BeApproximately(127 / 128f, 0.01f);
    }

    [Fact]
    public void Wave_16bit_LoadsExpectedValues()
    {
        short[] samples = [short.MinValue, -16384, 0, 16384, short.MaxValue];
        var wave = LoadWave(Write16BitWave(samples, 1, 44100));

        wave.Channels.Should().Be(1);
        wave.SampleRate.Should().Be(44100);
        wave.Frames.Should().Be(5);

        var buffer = new float[5];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(5);
        isFinished.Should().BeTrue();
        for (var i = 0; i < samples.Length; i++)
            buffer[i].Should().BeApproximately(samples[i] / (float)short.MaxValue, 0.001f);
    }

    [Fact]
    public void Wave_24bit_LoadsExpectedValues()
    {
        int[] samples = [-8388608, -4194304, 0, 4194304, 8388607];
        var wave = LoadWave(Write24BitWave(samples, 1, 44100));

        wave.Frames.Should().Be(5);

        var buffer = new float[5];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(5);
        isFinished.Should().BeTrue();
        for (var i = 0; i < samples.Length; i++)
            buffer[i].Should().BeApproximately(samples[i] / 8388608f, 0.001f);
    }

    [Fact]
    public void Wave_32bitFloat_LoadsExpectedValues()
    {
        float[] samples = [-1f, -0.5f, 0f, 0.5f, 0.999f];
        var wave = LoadWave(Write32BitFloatWave(samples, 1, 44100));

        wave.Frames.Should().Be(5);

        var buffer = new float[5];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(5);
        isFinished.Should().BeTrue();
        for (var i = 0; i < samples.Length; i++)
            buffer[i].Should().BeApproximately(samples[i], 0.0001f);
    }

    [Fact]
    public void Wave_Stereo16bit_InterleavesChannelsCorrectly()
    {
        // L, R, L, R ... の順にインターリーブされていることを検証
        short[] samples = [1000, -1000, 2000, -2000];
        var wave = LoadWave(Write16BitWave(samples, 2, 44100));

        wave.Channels.Should().Be(2);
        wave.Frames.Should().Be(2);

        var buffer = new float[4];
        var (filledFrames, _) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(2);
        buffer[0].Should().BeApproximately(1000 / (float)short.MaxValue, 0.001f);
        buffer[1].Should().BeApproximately(-1000 / (float)short.MaxValue, 0.001f);
        buffer[2].Should().BeApproximately(2000 / (float)short.MaxValue, 0.001f);
        buffer[3].Should().BeApproximately(-2000 / (float)short.MaxValue, 0.001f);
    }

    // FillSamples 振る舞いテスト

    [Fact]
    public void Wave_FillSamples_RespectsOffsetFrames()
    {
        short[] samples = [0, 100, 200, 300, 400];
        var wave = LoadWave(Write16BitWave(samples, 1, 44100));

        var buffer = new float[2];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 2);

        filledFrames.Should().Be(2);
        isFinished.Should().BeFalse();
        buffer[0].Should().BeApproximately(200 / (float)short.MaxValue, 0.001f);
        buffer[1].Should().BeApproximately(300 / (float)short.MaxValue, 0.001f);
    }

    [Fact]
    public void Wave_FillSamples_DoesNotWriteBeyondBufferLength()
    {
        short[] samples = [0, 100, 200, 300, 400];
        var wave = LoadWave(Write16BitWave(samples, 1, 44100));

        // バッファ長より読み込めるサンプル数が多い場合でも、バッファ長を超えて書き込まないことを検証
        var buffer = new float[3];
        var sentinel = 12345f;
        var extended = new float[4];
        buffer.CopyTo(extended, 0);
        extended[3] = sentinel;

        var (filledFrames, isFinished) = wave.FillSamples(extended.AsSpan(0, 3), 0);

        filledFrames.Should().Be(3);
        isFinished.Should().BeFalse();
        extended[3]
            .Should()
            .Be(sentinel, "FillSamples must not write past the given buffer length");
    }

    [Fact]
    public void Wave_FillSamples_IsFinished_ExactlyAtEnd()
    {
        short[] samples = [0, 100, 200];
        var wave = LoadWave(Write16BitWave(samples, 1, 44100));

        var buffer = new float[3];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 0);

        filledFrames.Should().Be(3);
        isFinished.Should().BeTrue();
    }

    [Fact]
    public void Wave_FillSamples_IsFinished_WhenOffsetPastEnd()
    {
        short[] samples = [0, 100, 200];
        var wave = LoadWave(Write16BitWave(samples, 1, 44100));

        var buffer = new float[3];
        var (filledFrames, isFinished) = wave.FillSamples(buffer, 10);

        filledFrames.Should().Be(0);
        isFinished.Should().BeTrue();
    }

    // Vorbis フォーマットテスト

    [Fact]
    public void VorbisAudioSource_CanLoad()
    {
        var initialize = () => new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        initialize.Should().NotThrow();
    }

    [Fact]
    public void VorbisAudioSource_Properties_AreValid()
    {
        using var vorbis = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        vorbis.Frames.Should().NotBeNull();
        vorbis.Frames!.Value.Should().BeGreaterThan(0);
        vorbis.Channels.Should().BeGreaterThan(0);
        vorbis.SampleRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task VorbisAudioSource_FillSamples_ProducesValuesInRangeAndReachesEnd()
    {
        using var vorbis = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        // デコードスレッドの完了を待つ
        while (!vorbis.IsLoadingFinished)
            await Task.Delay(10);

        var totalFrames = vorbis.Frames!.Value;
        var buffer = new float[totalFrames * vorbis.Channels];
        var (filledFrames, isFinished) = vorbis.FillSamples(buffer, 0);

        filledFrames.Should().Be(totalFrames);
        isFinished.Should().BeTrue();

        foreach (var sample in buffer)
            sample.Should().BeInRange(-1f, 1f);
    }

    // Private helpers

    private static WaveAudioSource LoadWave(byte[] wavBytes)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, wavBytes);
        try
        {
            return new WaveAudioSource(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static byte[] Write8BitWave(byte[] samples, short channels, int sampleRate) =>
        BuildWaveFile(1, 8, channels, sampleRate, samples);

    private static byte[] Write16BitWave(short[] samples, short channels, int sampleRate)
    {
        var raw = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, raw, 0, raw.Length);
        return BuildWaveFile(1, 16, channels, sampleRate, raw);
    }

    private static byte[] Write24BitWave(int[] samples, short channels, int sampleRate)
    {
        var raw = new byte[samples.Length * 3];
        for (var i = 0; i < samples.Length; i++)
        {
            var value = samples[i];
            raw[i * 3] = (byte)(value & 0xFF);
            raw[i * 3 + 1] = (byte)((value >> 8) & 0xFF);
            raw[i * 3 + 2] = (byte)((value >> 16) & 0xFF);
        }

        return BuildWaveFile(1, 24, channels, sampleRate, raw);
    }

    private static byte[] Write32BitFloatWave(float[] samples, short channels, int sampleRate)
    {
        var raw = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, raw, 0, raw.Length);
        return BuildWaveFile(3, 32, channels, sampleRate, raw);
    }

    /// <summary>
    /// 指定されたフォーマットとPCMデータから、最小限のWaveファイルバイト列を構築します。
    /// </summary>
    private static byte[] BuildWaveFile(
        short formatTag,
        short bitsPerSample,
        short channels,
        int sampleRate,
        byte[] data
    )
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var byteRate = sampleRate * blockAlign;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + data.Length);
        writer.Write("WAVE"u8.ToArray());

        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write(formatTag);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        writer.Write("data"u8.ToArray());
        writer.Write(data.Length);
        writer.Write(data);

        return ms.ToArray();
    }
}
