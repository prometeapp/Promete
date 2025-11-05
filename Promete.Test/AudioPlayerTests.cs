using FluentAssertions;
using Promete.Audio;
using Promete.Headless;

namespace Promete.Test;

public class AudioPlayerTests
{
    [Fact]
    public void VorbisAudioSource_CanLoad()
    {
        var initialize = () => new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        initialize.Should().NotThrow();
    }

    [Fact]
    public void PlayAndStop()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        using var audioPlayer = new AudioPlayer();
        using var audioSource = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        audioPlayer.Invoking(x => x.Play(audioSource)).Should().NotThrow();
        audioPlayer.IsPlaying.Should().BeTrue();

        audioPlayer.Stop();
        audioPlayer.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public void PauseAndResume()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        using var audioPlayer = new AudioPlayer();
        using var audioSource = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

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
        using var audioPlayer = new AudioPlayer();
        using var audioSource = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

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
    public async Task IsPlayingShouldBeTrueWhenPlayTwice()
    {
        using var audioPlayer = new AudioPlayer();
        using var audioSource = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");

        audioPlayer.Play(audioSource);
        audioPlayer.IsPlaying.Should().BeTrue();
        await Task.Delay(2000);
        audioPlayer.Play(audioSource);
        audioPlayer.IsPlaying.Should().BeTrue();

        await Task.Delay(2000);
        audioPlayer.Stop();
    }
}
