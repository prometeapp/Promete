using System;
using System.Threading.Tasks;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// OpenAL を用いる、既定の <see cref="IAudioProvider"/> の実装です。
/// </summary>
internal sealed class OpenALAudioProvider : IAudioProvider
{
    private OpenALAudioProvider() { }

    /// <summary>
    /// 共有のインスタンスを取得します。
    /// </summary>
    public static OpenALAudioProvider Shared { get; } = new();

    public IAudioOutput CreateOutput()
    {
        return new OpenALAudioOutput(AudioDevice.Acquire(), ownsDevice: true);
    }

    public async ValueTask PlayOneShotAsync(IAudioSource source, float gain, float pitch, float pan)
    {
        var frames =
            source.Frames
            ?? throw new ArgumentException(
                "PlayOneShot requires AudioSource which has determined length."
            );

        // 共有デバイスは参照カウントで管理されているため、プレイヤーが生きている間は同じデバイスが使われる
        using var device = AudioDevice.Acquire();
        var al = device.Al;
        var floatBuffer = new float[frames * source.Channels];
        source.FillSamples(floatBuffer, 0);
        var buffer = ToInt16Buffer(floatBuffer);
        using var alSrc = new ALSource(al);
        using var alBuf = new ALBuffer(al);
        var bufferFormat = GetBufferFormat(source);

        al.BufferData(alBuf.Handle, bufferFormat, buffer, source.SampleRate);
        al.SourceQueueBuffers(alSrc.Handle, new uint[] { alBuf.Handle });
        al.SetSourceProperty(alSrc.Handle, SourceFloat.Gain, gain);
        al.SetSourceProperty(alSrc.Handle, SourceFloat.Pitch, pitch);
        var x = pan;
        var z = MathF.Abs(pan) < 1.0f ? -MathF.Sqrt(1.0f - (pan * pan)) : 0.0f;
        al.SetSourceProperty(alSrc.Handle, SourceVector3.Position, x, 0, z);
        al.SetSourceProperty(alSrc.Handle, SourceBoolean.SourceRelative, true);
        al.SetSourceProperty(alSrc.Handle, SourceFloat.MaxDistance, 1);
        al.SetSourceProperty(alSrc.Handle, SourceFloat.ReferenceDistance, 0.5f);

        al.SourcePlay(alSrc.Handle);

        int buffersProcessed;
        do
        {
            al.GetSourceProperty(
                alSrc.Handle,
                GetSourceInteger.BuffersProcessed,
                out buffersProcessed
            );
            await Task.Delay(1);
        } while (buffersProcessed < 1);
    }

    private static BufferFormat GetBufferFormat(IAudioSource source)
    {
        return source.Channels switch
        {
            1 => BufferFormat.Mono16,
            2 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException("Unsupported format."),
        };
    }

    /// <summary>
    /// float PCM (-1.0～1.0) を16bit整数PCMの新しい配列へクランプ付きで変換します。
    /// </summary>
    private static short[] ToInt16Buffer(ReadOnlySpan<float> source)
    {
        var result = new short[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var clamped = Math.Clamp(source[i], -1f, 1f);
            result[i] = (short)(clamped * short.MaxValue);
        }

        return result;
    }
}
