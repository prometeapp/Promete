using FluentAssertions;
using Promete.Audio;
using Promete.Backends.Headless;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// <see cref="AudioPlayer"/> が、バックエンドの <see cref="IAudioProvider"/> を使って音声を出力することを確かめるテスト。
/// </summary>
public class AudioProviderTests
{
    [Fact]
    public void DefaultConstructor_ShouldUseAndOwnProviderOutput()
    {
        using var app = PrometeApp.Create().Build<AudioProviderBackend>(null);

        var player = new AudioPlayer();
        RecordingAudioProvider.Outputs.Should().ContainSingle();
        var output = RecordingAudioProvider.Outputs[0];
        output.IsStarted.Should().BeTrue("プロバイダーの出力に対して出力が開始されるはず");

        player.Dispose();

        output
            .IsDisposed.Should()
            .BeTrue("AudioPlayer が生成した出力は AudioPlayer が破棄するはず");
    }

    [Fact]
    public async Task PlayOneShotAsync_ShouldDelegateToProviderWithEffectiveGain()
    {
        using var app = PrometeApp.Create().Build<AudioProviderBackend>(null);
        using var player = new AudioPlayer(new CaptureAudioOutput()) { Gain = 0.5f };
        var source = new SilentAudioSource();

        await player.PlayOneShotAsync(source, gain: 0.8f, pitch: 1.5f, pan: -0.25f);
        await player.PlayOneShotAsync(source, gain: 0.8f, followsMasterGain: true);

        RecordingAudioProvider.OneShots.Should().Equal((0.8f, 1.5f, -0.25f), (0.4f, 1f, 0f));
    }

    [Fact]
    public void DefaultBackend_ShouldProvideOpenALProvider()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        app.GetPlugin<IAudioProvider>().Should().BeSameAs(OpenALAudioProvider.Shared);
    }

    internal sealed class AudioProviderBackend : HeadlessBackend
    {
        public override IAudioProvider SetupAudioProvider()
        {
            RecordingAudioProvider.Outputs.Clear();
            RecordingAudioProvider.OneShots.Clear();
            return new RecordingAudioProvider();
        }
    }

    internal sealed class RecordingAudioProvider : IAudioProvider
    {
        public static List<RecordingOutput> Outputs { get; } = [];

        public static List<(float Gain, float Pitch, float Pan)> OneShots { get; } = [];

        public IAudioOutput CreateOutput()
        {
            var output = new RecordingOutput();
            Outputs.Add(output);
            return output;
        }

        public ValueTask PlayOneShotAsync(IAudioSource source, float gain, float pitch, float pan)
        {
            OneShots.Add((gain, pitch, pan));
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class RecordingOutput : IAudioOutput
    {
        public bool IsStarted { get; private set; }

        public bool IsDisposed { get; private set; }

        public float Pitch { get; set; } = 1;

        public long PendingFrames => 0;

        public void Start(
            AudioRenderCallback render,
            int channels,
            int sampleRate,
            int bufferSizeInFrames,
            Func<int>? getSampleRate = null,
            int bufferCount = 3
        )
        {
            IsStarted = true;
        }

        public void Stop() { }

        public void Flush() { }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class SilentAudioSource : IAudioSource
    {
        public int? Frames => 10;

        public int Channels => 1;

        public int SampleRate => 44100;

        public (int FilledFrames, bool IsFinished) FillSamples(Span<float> buffer, int offsetFrames)
        {
            buffer.Clear();
            return (buffer.Length, true);
        }
    }
}
