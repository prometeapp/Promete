using Promete.Audio;

namespace Promete.Web.Audio;

/// <summary>
/// Web Audio へ出力する <see cref="IAudioProvider"/> です。
/// </summary>
internal sealed class WebAudioProvider : IAudioProvider
{
    public IAudioOutput CreateOutput() => new WebAudioOutput();

    public async ValueTask PlayOneShotAsync(IAudioSource source, float gain, float pitch, float pan)
    {
        var frames =
            source.Frames
            ?? throw new ArgumentException(
                "PlayOneShot requires AudioSource which has determined length."
            );

        var samples = new float[frames * source.Channels];
        source.FillSamples(samples, 0);
        await WebAudioOutput.PlayOneShot(
            samples,
            source.Channels,
            source.SampleRate,
            gain,
            pitch,
            pan
        );
    }
}
