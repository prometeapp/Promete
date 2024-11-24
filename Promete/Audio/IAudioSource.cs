namespace Promete.Audio;

/// <summary>
/// Defines the specifications of sound sources that can be handled by the Promete API.
/// </summary>
public interface IAudioSource
{
    /// <summary>
    /// Get samples of this <see cref="IAudioSource" />. Return <c>null</c> if unspecified.
    /// </summary>
    public int? Samples { get; }

    /// <summary>
    /// Get the number of channels.
    /// </summary>
    public int Channels { get; }

    /// <summary>
    /// Get the number of quantization bits.
    /// </summary>
    public int Bits { get; }

    /// <summary>
    /// Get the sampling frequency.
    /// </summary>
    public int SampleRate { get; }

    /// <summary>
    /// サンプルデータを配列に読み込みます。
    /// </summary>
    public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset);
}
