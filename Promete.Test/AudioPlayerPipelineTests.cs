using System;
using System.Threading.Tasks;
using FluentAssertions;
using Promete.Audio;

namespace Promete.Test;

/// <summary>
/// 常駐レンダーループ化された <see cref="AudioPlayer"/> の状態機械・PCM生成ロジックを、
/// <see cref="CaptureAudioOutput"/> を使って実デバイス・実時間なしで決定的に検証するテストです。
/// </summary>
public class AudioPlayerPipelineTests
{
    // Pan=0（中央）のとき constant-power パンにより乗算されるスケール(√2/2)
    private const float CenterPanScale = 0.70710678f;

    [Fact]
    public void Play_SetsIsPlayingImmediately()
    {
        using var audioPlayer = new AudioPlayer(new CaptureAudioOutput());
        using var source = new RampAudioSource(frames: 100);

        audioPlayer.Play(source);

        audioPlayer.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void Pause_StopsAdvancingPositionAndProducesSilence()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);

        audioPlayer.Play(source);
        output.RenderNext();
        var positionAfterFirstBuffer = audioPlayer.TimeInSamples;

        audioPlayer.Pause();
        audioPlayer.IsPausing.Should().BeTrue();

        output.RenderNext();
        audioPlayer.TimeInSamples.Should().Be(positionAfterFirstBuffer);
        output.CapturedBuffers[^1].Should().OnlyContain(x => x == 0f);
    }

    [Fact]
    public void Resume_ContinuesFromPausedPosition()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);

        audioPlayer.Play(source);
        output.RenderNext();
        var positionBeforePause = audioPlayer.TimeInSamples;

        audioPlayer.Pause();
        output.RenderNext();

        audioPlayer.Resume();
        output.RenderNext();

        var resumedBuffer = output.CapturedBuffers[^1];

        // 一時停止していたフレーム数はスキップされず、一時停止直前の続きの波形になっているはず
        // (Pan=0 の constant-power スケール √2/2 が乗算される)
        resumedBuffer[0]
            .Should()
            .BeApproximately(
                RampAudioSource.ExpectedMonoValue(positionBeforePause) * CenterPanScale,
                0.0001f
            );
    }

    [Fact]
    public void Stop_SetsIsPlayingFalseAndResetsPosition()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);

        audioPlayer.Play(source);
        output.RenderNext();

        audioPlayer.Stop();

        audioPlayer.IsPlaying.Should().BeFalse();
        audioPlayer.TimeInSamples.Should().Be(0);
    }

    [Fact]
    public void Play_ProducesExpectedWaveform_WithGainApplied()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000, channels: 2);
        audioPlayer.Gain = 0.5f;

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        for (var frame = 0; frame < 10; frame++)
        {
            // Pan=0 の constant-power スケール √2/2 が乗算される
            var expectedL = RampAudioSource.ExpectedStereoLeft(frame) * 0.5f * CenterPanScale;
            var expectedR = RampAudioSource.ExpectedStereoRight(frame) * 0.5f * CenterPanScale;
            buffer[frame * 2].Should().BeApproximately(expectedL, 0.0001f);
            buffer[(frame * 2) + 1].Should().BeApproximately(expectedR, 0.0001f);
        }
    }

    [Fact]
    public void Play_MonoSource_UpmixesToEqualLeftAndRight()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000, channels: 1);

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        for (var frame = 0; frame < 10; frame++)
            buffer[frame * 2].Should().Be(buffer[(frame * 2) + 1]);
    }

    [Fact]
    public void Seek_WhilePlaying_JumpsToRequestedPositionOnNextBuffer()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);

        audioPlayer.Play(source);
        output.RenderNext();

        audioPlayer.TimeInSamples = 50_000;
        output.RenderNext();

        var buffer = output.CapturedBuffers[^1];
        buffer[0]
            .Should()
            .BeApproximately(RampAudioSource.ExpectedMonoValue(50_000) * CenterPanScale, 0.0001f);
    }

    [Fact]
    public void Seek_BeforePlaying_StartsPlaybackFromRequestedPosition()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);

        audioPlayer.TimeInSamples = 1_234;
        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        buffer[0]
            .Should()
            .BeApproximately(RampAudioSource.ExpectedMonoValue(1_234) * CenterPanScale, 0.0001f);
    }

    [Fact]
    public void Loop_WrapsWithinSingleBuffer_WithoutSilenceGap()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);

        // バッファサイズ(デフォルト1024フレーム)よりずっと短いソースにより、1バッファ内で何度もループ境界をまたぐ
        using var source = new RampAudioSource(frames: 7);

        audioPlayer.Play(source, loop: 0);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        for (var frame = 0; frame < buffer.Length / 2; frame++)
        {
            var expected = RampAudioSource.ExpectedMonoValue(frame % 7) * CenterPanScale;
            buffer[frame * 2].Should().BeApproximately(expected, 0.0001f);
        }
    }

    [Fact]
    public void Loop_RaisesLoopedEvent()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 7);
        var loopedCount = 0;
        audioPlayer.Loop += (_, _) => loopedCount++;

        audioPlayer.Play(source, loop: 0);
        output.RenderNext();

        // 1024フレームバッファを7フレーム周期でループするので、100回以上ループするはず
        loopedCount.Should().BeGreaterThan(100);
    }

    [Fact]
    public void Loop_NotSpecified_ProducesSilenceAfterSourceEnds()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 7);

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];

        // 7フレーム分だけ波形が出て、以降は無音になっているはず
        for (var frame = 7; frame < buffer.Length / 2; frame++)
        {
            buffer[frame * 2].Should().Be(0f);
            buffer[(frame * 2) + 1].Should().Be(0f);
        }
    }

    [Fact]
    public void Stop_WithFadeSeconds_AmplitudeDecreasesMonotonicallyToZero()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);
        var originalGain = 1f;
        audioPlayer.Gain = originalGain;

        audioPlayer.Play(source);
        output.RenderNext();

        // BufferSize(1024フレーム) / SampleRate(44100Hz) ≈ 23ms より長いフェード時間を指定し、
        // フェード完了までに複数バッファかかるようにする（0.1秒 ≈ 4.3バッファ）
        audioPlayer.Stop(time: 0.1f);
        output.RenderNext(6);

        var samples = output.GetAllSamples();

        // 最初のバッファ(フェード開始前)を除いた範囲で振幅の単調減少を検証する
        var bufferSizeInSamples = output.CapturedBuffers[0].Length;
        var previousAbs = float.MaxValue;
        for (var i = bufferSizeInSamples; i < samples.Length; i += 2)
        {
            var abs = MathF.Abs(samples[i]);
            abs.Should().BeLessThanOrEqualTo(previousAbs + 0.0001f);
            previousAbs = abs;
        }

        previousAbs.Should().Be(0f);
        audioPlayer.Gain.Should().Be(originalGain);
    }

    [Fact]
    public void Stop_WithFadeSeconds_IsPlayingRemainsTrueDuringFade()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);

        audioPlayer.Play(source);
        output.RenderNext();

        audioPlayer.Stop(time: 10f);
        output.RenderNext();

        audioPlayer.IsPlaying.Should().BeTrue();
    }

    [Fact]
    public void SourceReachesEnd_RaisesFinishPlayingAndStopsPlayback()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 5);
        var finished = false;
        audioPlayer.FinishPlaying += (_, _) => finished = true;

        audioPlayer.Play(source);
        output.RenderNext();

        finished.Should().BeTrue();
        audioPlayer.IsPlaying.Should().BeFalse();

        output.RenderNext();
        output.CapturedBuffers[^1].Should().OnlyContain(x => x == 0f);
    }

    [Fact]
    public void Pan_FullLeft_MutesRightChannel()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);
        audioPlayer.Pan = -1f;

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        buffer[1].Should().BeApproximately(0f, 0.0001f);
        buffer[0].Should().NotBe(0f);
    }

    [Fact]
    public void Pan_FullRight_MutesLeftChannel()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);
        audioPlayer.Pan = 1f;

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        buffer[0].Should().BeApproximately(0f, 0.0001f);
        buffer[1].Should().NotBe(0f);
    }

    [Fact]
    public void Pan_Centered_LeftEqualsRight_WithConstantPowerScale()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 1f);
        audioPlayer.Pan = 0f;

        audioPlayer.Play(source);
        output.RenderNext();

        var buffer = output.CapturedBuffers[0];
        buffer[0].Should().Be(buffer[1]);

        // constant-power パンでは中央時に √2/2 倍のスケールがかかる
        buffer[0].Should().BeApproximately(MathF.Sqrt(2f) / 2f, 0.0001f);
    }

    [Fact]
    public void InfiniteStreamSource_PlaysAndStopsNormally()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new ConstantAudioSource(value: 0.5f);

        audioPlayer.Invoking(x => x.Play(source)).Should().NotThrow();
        output.RenderNext();

        audioPlayer.IsPlaying.Should().BeTrue();
        var buffer = output.CapturedBuffers[0];

        // Pan=0 の constant-power スケール √2/2 が乗算される
        buffer.Should().OnlyContain(x => Math.Abs(x - (0.5f * CenterPanScale)) < 0.0001f);

        audioPlayer.Invoking(x => x.Stop()).Should().NotThrow();
        audioPlayer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task PlayAsync_CompletesWhenSourceReachesEnd()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 5);

        var playTask = audioPlayer.PlayAsync(source);
        output.RenderNext();

        await playTask;
    }

    [Fact]
    public async Task PlayAsync_CompletesWhenReplacedByAnotherPlay()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source1 = new RampAudioSource(frames: 100_000);
        using var source2 = new RampAudioSource(frames: 100_000);

        var firstPlayTask = audioPlayer.PlayAsync(source1);
        output.RenderNext();

        audioPlayer.Play(source2);

        await firstPlayTask;
    }

    [Fact]
    public void PlayThenStop_RaisesStartPlayingThenStopPlaying_InOrder()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 100_000);
        var events = new System.Collections.Generic.List<string>();
        audioPlayer.StartPlaying += (_, _) => events.Add("Start");
        audioPlayer.StopPlaying += (_, _) => events.Add("Stop");
        audioPlayer.FinishPlaying += (_, _) => events.Add("Finish");

        audioPlayer.Play(source);
        output.RenderNext();
        audioPlayer.Stop();
        output.RenderNext();

        events.Should().Equal("Start", "Stop");
    }

    [Fact]
    public void SourceReachesEnd_RaisesFinishPlayingOnly_NotStopPlaying()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var source = new RampAudioSource(frames: 5);
        var events = new System.Collections.Generic.List<string>();
        audioPlayer.StartPlaying += (_, _) => events.Add("Start");
        audioPlayer.StopPlaying += (_, _) => events.Add("Stop");
        audioPlayer.FinishPlaying += (_, _) => events.Add("Finish");

        audioPlayer.Play(source);
        output.RenderNext();

        events.Should().Equal("Start", "Finish");
    }

    // Private helpers / mocks

    /// <summary>
    /// フレーム番号から一意に決まる値を生成するランプ波のモックソース。既知波形として検証に用いる。
    /// </summary>
    private sealed class RampAudioSource(int frames, int channels = 1) : IAudioSource, IDisposable
    {
        public int? Frames { get; } = frames;

        public int Channels { get; } = channels;

        public int SampleRate => 44100;

        public static float ExpectedMonoValue(long frame) => ((frame % 1000) - 500) / 500f;

        public static float ExpectedStereoLeft(long frame) => ExpectedMonoValue(frame);

        public static float ExpectedStereoRight(long frame) => -ExpectedMonoValue(frame);

        public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
        {
            var totalFrames = Frames!.Value;
            var remaining = Math.Max(0, totalFrames - offsetFrames);
            var framesToFill = Math.Min(remaining, buffer.Length / Channels);

            for (var i = 0; i < framesToFill; i++)
            {
                var frame = offsetFrames + i;
                if (Channels == 1)
                {
                    buffer[i] = ExpectedMonoValue(frame);
                }
                else
                {
                    buffer[i * Channels] = ExpectedStereoLeft(frame);
                    buffer[(i * Channels) + 1] = ExpectedStereoRight(frame);
                }
            }

            var isFinished = offsetFrames + framesToFill >= totalFrames;
            return (framesToFill, isFinished);
        }

        public void Dispose() { }
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
