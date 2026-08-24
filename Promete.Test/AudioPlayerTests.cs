using FluentAssertions;
using Promete.Audio;

namespace Promete.Test;

/// <summary>
/// 実際の Vorbis 音源を <see cref="CaptureAudioOutput"/> 経由で再生し、
/// 実デバイス・実時間なしで <see cref="AudioPlayer"/> の基本操作を検証するテストです。
/// </summary>
public class AudioPlayerTests
{
    [Fact]
    public void VorbisAudioSource_CanLoad()
    {
        var initialize = () => new VorbisAudioSource("./assets/amaebi.ogg");

        initialize.Should().NotThrow();
    }

    [Fact]
    public void PlayAndStop()
    {
        using var audioPlayer = new AudioPlayer(new CaptureAudioOutput());
        using var audioSource = new VorbisAudioSource("./assets/amaebi.ogg");

        audioPlayer.Invoking(x => x.Play(audioSource)).Should().NotThrow();
        audioPlayer.IsPlaying.Should().BeTrue();

        audioPlayer.Stop();
        audioPlayer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void PauseAndResume()
    {
        using var audioPlayer = new AudioPlayer(new CaptureAudioOutput());
        using var audioSource = new VorbisAudioSource("./assets/amaebi.ogg");

        audioPlayer.Play(audioSource);
        audioPlayer.IsPausing.Should().BeFalse();

        audioPlayer.Pause();
        audioPlayer.IsPlaying.Should().BeTrue();
        audioPlayer.IsPausing.Should().BeTrue();

        audioPlayer.Resume();
        audioPlayer.IsPlaying.Should().BeTrue();
        audioPlayer.IsPausing.Should().BeFalse();

        audioPlayer.Stop();
    }

    [Fact]
    public void PauseAndStop()
    {
        using var audioPlayer = new AudioPlayer(new CaptureAudioOutput());
        using var audioSource = new VorbisAudioSource("./assets/amaebi.ogg");

        audioPlayer.Play(audioSource);
        audioPlayer.IsPausing.Should().BeFalse();

        audioPlayer.Pause();
        audioPlayer.IsPlaying.Should().BeTrue();
        audioPlayer.IsPausing.Should().BeTrue();

        audioPlayer.Stop();
        audioPlayer.IsPlaying.Should().BeFalse();
        audioPlayer.IsPausing.Should().BeFalse();
    }

    [Fact]
    public void IsPlayingShouldBeTrueWhenPlayTwice()
    {
        var output = new CaptureAudioOutput();
        using var audioPlayer = new AudioPlayer(output);
        using var audioSource = new VorbisAudioSource("./assets/amaebi.ogg");

        audioPlayer.Play(audioSource);
        audioPlayer.IsPlaying.Should().BeTrue();

        // 再生途中まで進めてから再度 Play する
        output.RenderNext(10);
        audioPlayer.Play(audioSource);
        audioPlayer.IsPlaying.Should().BeTrue();

        output.RenderNext(10);
        audioPlayer.IsPlaying.Should().BeTrue();

        audioPlayer.Stop();
    }
}
