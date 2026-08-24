using System;
using System.Linq;
using FluentAssertions;
using Promete.Audio;
using Promete.Audio.Filters;

namespace Promete.Test;

/// <summary>
/// DSP フィルターチェーン (<see cref="IAudioFilter"/> とその標準実装) を、デバイス・実時間なしで検証するテストです。
/// </summary>
public class AudioFilterTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void DelayFilter_ImpulseResponse_ProducesPeakAtDelayTime()
    {
        var filter = new DelayFilter
        {
            Time = 0.1f,
            Feedback = 0.5f,
            Mix = 1f,
        };
        var delaySamples = (int)(0.1f * SampleRate);

        // 単位インパルスを含む十分な長さのバッファで一括処理する
        var buffer = new float[(delaySamples * 3) * 2];
        buffer[0] = 1f;
        buffer[1] = 1f;

        filter.Process(buffer, 2, SampleRate);

        // Mix=1 のとき、delaySamples 後のフレームはディレイ音（元の入力そのもの）が全量出力される
        buffer[delaySamples * 2].Should().BeApproximately(1f, 0.0001f);
        buffer[(delaySamples * 2) + 1].Should().BeApproximately(1f, 0.0001f);

        // フィードバックにより 2*delaySamples 後にも Feedback 倍のピークが現れる
        buffer[delaySamples * 2 * 2].Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void DelayFilter_Reset_ClearsResidualTail()
    {
        var filter = new DelayFilter
        {
            Time = 0.05f,
            Feedback = 0.5f,
            Mix = 1f,
        };
        var delaySamples = (int)(0.05f * SampleRate);

        var impulse = new float[delaySamples * 2];
        impulse[0] = 1f;
        impulse[1] = 1f;
        filter.Process(impulse, 2, SampleRate);

        filter.Reset();

        var silence = new float[delaySamples * 2];
        filter.Process(silence, 2, SampleRate);

        silence.Should().OnlyContain(x => x == 0f);
    }

    [Fact]
    public void LowPassFilter_LowFrequency_PassesThroughAlmostUnattenuated()
    {
        var filter = new LowPassFilter { CutoffFrequency = 2000f, Resonance = 0.707f };
        var buffer = GenerateSineWave(frequency: 100f, frames: 4096, channels: 2);
        var inputRms = ComputeRms(buffer);

        filter.Process(buffer, 2, SampleRate);
        var outputRms = ComputeRms(buffer);

        (outputRms / inputRms).Should().BeGreaterThan(0.9f);
    }

    [Fact]
    public void LowPassFilter_HighFrequency_IsSignificantlyAttenuated()
    {
        var filter = new LowPassFilter { CutoffFrequency = 500f, Resonance = 0.707f };
        var buffer = GenerateSineWave(frequency: 8000f, frames: 4096, channels: 2);
        var inputRms = ComputeRms(buffer);

        filter.Process(buffer, 2, SampleRate);
        var outputRms = ComputeRms(buffer);

        (outputRms / inputRms).Should().BeLessThan(0.3f);
    }

    [Fact]
    public void LowPassFilter_MixZero_PassesInputThroughUnchanged()
    {
        var filter = new LowPassFilter { CutoffFrequency = 500f, Mix = 0f };
        var input = GenerateSineWave(frequency: 8000f, frames: 1024, channels: 2);
        var buffer = (float[])input.Clone();

        filter.Process(buffer, 2, SampleRate);

        buffer.Should().Equal(input);
    }

    [Fact]
    public void LowPassFilter_HalfMix_AttenuatesLessThanFullWet()
    {
        var wetFilter = new LowPassFilter { CutoffFrequency = 500f, Mix = 1f };
        var halfFilter = new LowPassFilter { CutoffFrequency = 500f, Mix = 0.5f };
        var wetBuffer = GenerateSineWave(frequency: 8000f, frames: 4096, channels: 2);
        var halfBuffer = (float[])wetBuffer.Clone();

        wetFilter.Process(wetBuffer, 2, SampleRate);
        halfFilter.Process(halfBuffer, 2, SampleRate);

        // Mix=0.5 はドライ成分が残るため、全ウェットより減衰が緩くなる
        ComputeRms(halfBuffer).Should().BeGreaterThan(ComputeRms(wetBuffer));
    }

    [Fact]
    public void LowPassFilter_AutoMakeupGain_RestoresOutputRmsTowardInput()
    {
        var plain = new LowPassFilter { CutoffFrequency = 500f };
        var makeup = new LowPassFilter { CutoffFrequency = 500f, AutoMakeupGain = true };

        // 高域寄りの信号で大きくエネルギーが失われる状況を作り、平滑化が収束するまで複数バッファ処理する
        var inputRms = 0f;
        var plainRms = 0f;
        var makeupRms = 0f;
        for (var i = 0; i < 50; i++)
        {
            var input = GenerateSineWave(frequency: 8000f, frames: 1024, channels: 2);
            inputRms = ComputeRms(input);

            var plainBuffer = (float[])input.Clone();
            plain.Process(plainBuffer, 2, SampleRate);
            plainRms = ComputeRms(plainBuffer);

            var makeupBuffer = (float[])input.Clone();
            makeup.Process(makeupBuffer, 2, SampleRate);
            makeupRms = ComputeRms(makeupBuffer);
        }

        // 補正ありは補正なしより入力RMSに近い
        Math.Abs(makeupRms - inputRms).Should().BeLessThan(Math.Abs(plainRms - inputRms));
        makeupRms.Should().BeGreaterThan(plainRms);
    }

    [Fact]
    public void LowPassFilter_AutoMakeupGain_MixZero_DoesNotBoost()
    {
        var filter = new LowPassFilter
        {
            CutoffFrequency = 500f,
            Mix = 0f,
            AutoMakeupGain = true,
        };

        // Mix=0 では出力=入力なので、補正ゲインは1のまま変化しない
        var input = GenerateSineWave(frequency: 8000f, frames: 1024, channels: 2);
        var buffer = (float[])input.Clone();
        for (var i = 0; i < 20; i++)
        {
            buffer = (float[])input.Clone();
            filter.Process(buffer, 2, SampleRate);
        }

        ComputeRms(buffer).Should().BeApproximately(ComputeRms(input), 0.001f);
    }

    [Fact]
    public void LowPassFilter_AutoMakeupGain_SilentInput_DoesNotBlowUp()
    {
        var filter = new LowPassFilter { CutoffFrequency = 500f, AutoMakeupGain = true };

        var silence = new float[1024 * 2];
        for (var i = 0; i < 20; i++)
            filter.Process(silence, 2, SampleRate);

        // 無音入力では補正値の更新が凍結され、出力も無音のまま
        silence.Should().OnlyContain(x => x == 0f);
    }

    [Fact]
    public void DistortionFilter_LargeAmplitude_IsClampedWithinLevel()
    {
        var filter = new DistortionFilter { Drive = 10f, Level = 0.8f };
        var buffer = Enumerable.Range(0, 200).Select(i => i % 2 == 0 ? 5f : -5f).ToArray();

        filter.Process(buffer, 2, SampleRate);

        buffer.Should().OnlyContain(x => Math.Abs(x) <= 0.8f + 0.0001f);
    }

    [Fact]
    public void DistortionFilter_IsStateless_SameInputProducesSameOutput()
    {
        var filter = new DistortionFilter { Drive = 3f, Level = 1f };
        var input = GenerateSineWave(frequency: 440f, frames: 256, channels: 2);

        var bufferA = (float[])input.Clone();
        var bufferB = (float[])input.Clone();

        filter.Process(bufferA, 2, SampleRate);
        filter.Process(bufferB, 2, SampleRate);

        bufferA.Should().Equal(bufferB);
    }

    [Fact]
    public void AudioPlayer_FiltersAdd_WetSignalIsAudible()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);

        var delay = new DelayFilter
        {
            Time = 0.01f,
            Feedback = 0.5f,
            Mix = 1f,
        };
        audioPlayer.Filters.Add(delay);

        audioPlayer.Play(source);
        output.RenderNext();

        // Mix=1 のディレイを通すと、ディレイタイム経過後のサンプルは元の乾いた信号と一致しなくなる
        var delaySamples = (int)(0.01f * SampleRate);
        var buffer = output.CapturedBuffers[0];
        buffer[delaySamples * 2].Should().NotBe(0f);
    }

    [Fact]
    public void AudioPlayer_StopAfterPlay_DelayTailContinuesRingingOut()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);

        // ディレイライン(0.01秒=441フレーム)がバッファ(デフォルト1024フレーム)内に収まり、
        // 最初のバッファでラインが一巡して残響が始まるようにする
        var delay = new DelayFilter
        {
            Time = 0.01f,
            Feedback = 0.8f,
            Mix = 1f,
        };
        audioPlayer.Filters.Add(delay);

        audioPlayer.Play(source);
        output.RenderNext();

        audioPlayer.Stop();
        audioPlayer.IsPlaying.Should().BeFalse();

        // Stop 後もフィルターは毎バッファ通り続けるため、直後のバッファには残響（非無音）が含まれる
        output.RenderNext();
        var afterStop = output.CapturedBuffers[^1];
        afterStop.Should().Contain(x => x != 0f);

        // 残響は時間とともに減衰していく
        output.RenderNext(10);
        var muchLater = output.CapturedBuffers[^1];
        var laterAbsMax = muchLater.Select(Math.Abs).Max();
        var earlyAbsMax = afterStop.Select(Math.Abs).Max();
        laterAbsMax.Should().BeLessThan(earlyAbsMax);
    }

    [Fact]
    public void AudioPlayer_SeekAcrossFilter_StateIsNotAutomaticallyReset()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);

        var delay = new DelayFilter
        {
            Time = 0.01f,
            Feedback = 0.5f,
            Mix = 1f,
        };
        audioPlayer.Filters.Add(delay);

        audioPlayer.Play(source);
        output.RenderNext();

        audioPlayer.TimeInSamples = 100;
        output.RenderNext();

        // シークを挟んでもディレイラインの中身は保持され続け、無音にリセットされない
        var buffer = output.CapturedBuffers[^1];
        buffer.Should().Contain(x => x != 0f);
    }

    [Fact]
    public void AudioPlayer_FiltersRemove_TakesEffectFromNextBuffer()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);

        var distortion = new DistortionFilter { Drive = 100f, Level = 1f };
        audioPlayer.Filters.Add(distortion);

        audioPlayer.Play(source);
        output.RenderNext();

        // フィルターはパン適用後の信号(√2/2)に対して掛かる。強いDriveでtanhが飽和し、振幅はほぼ1になっているはず
        var withFilter = output.CapturedBuffers[^1];
        withFilter[0].Should().BeApproximately(MathF.Tanh((MathF.Sqrt(2f) / 2f) * 100f), 0.0001f);

        audioPlayer.Filters.Remove(distortion);
        output.RenderNext();

        var withoutFilter = output.CapturedBuffers[^1];

        // フィルター解除後は Pan=0 の constant-power スケール(√2/2)のみが乗算された値になる
        withoutFilter[0].Should().BeApproximately(MathF.Sqrt(2f) / 2f, 0.0001f);
    }

    private static float[] GenerateSineWave(float frequency, int frames, int channels)
    {
        var buffer = new float[frames * channels];
        for (var i = 0; i < frames; i++)
        {
            var value = MathF.Sin(2f * MathF.PI * frequency * i / SampleRate);
            for (var ch = 0; ch < channels; ch++)
                buffer[(i * channels) + ch] = value;
        }

        return buffer;
    }

    private static float ComputeRms(float[] buffer)
    {
        var sumSquares = 0.0;
        foreach (var sample in buffer)
            sumSquares += (double)sample * sample;
        return (float)Math.Sqrt(sumSquares / buffer.Length);
    }

    /// <summary>
    /// 常に一定値を返し続ける、長さ未確定（無限ストリーム）のモックソース。
    /// </summary>
    private sealed class ConstantAudioSource(float value, int channels = 1)
        : IAudioSource,
            IDisposable
    {
        public int? Frames => null;

        public int Channels { get; } = channels;

        public int SampleRate => 44100;

        public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
        {
            var framesToFill = buffer.Length / Channels;
            for (var i = 0; i < framesToFill; i++)
            {
                for (var ch = 0; ch < Channels; ch++)
                    buffer[(i * Channels) + ch] = value;
            }

            return (framesToFill, false);
        }

        public void Dispose() { }
    }
}
