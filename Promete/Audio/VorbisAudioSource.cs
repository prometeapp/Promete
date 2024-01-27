using System;
using System.IO;
using NVorbis;

namespace Promete.Audio;

/// <summary>
/// An audio source that represents data in Ogg Vorbis format.
/// </summary>
public class VorbisAudioSource : IAudioSource, IDisposable
{
	/// <summary>
	/// Get the total number of samples.
	/// </summary>
	/// <returns></returns>
	public int? Samples => (int)reader.TotalSamples * reader.Channels;

	/// <summary>
	/// Get channels.
	/// </summary>
	public int Channels => reader.Channels;

	/// <summary>
	/// Get sample bits.
	/// </summary>
	public int Bits => 16;

	/// <summary>
	/// Get sample rate.
	/// </summary>
	public int SampleRate => reader.SampleRate;

	private readonly short[] store;

	public VorbisAudioSource(string path)
		: this(File.OpenRead(path))
	{
	}

	public VorbisAudioSource(Stream stream)
	{
		reader = new VorbisReader(stream);

		var temp = new float[Samples ?? 0];
		store = new short[Samples ?? 0];

		reader.ReadSamples(temp, 0, temp.Length);

		var tempSpan = temp.AsSpan();
		var storeSpan = store.AsSpan();

		unchecked
		{
			for (var i = 0; i < tempSpan.Length; i++)
			{
				storeSpan[i] = (short)(tempSpan[i] * short.MaxValue);
			}
		}
	}

	public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
	{
		var actualReadSize = Math.Min(buffer.Length, store.Length - offset);
		Buffer.BlockCopy(store, offset * sizeof(short), buffer, 0, actualReadSize * sizeof(short));
		return (actualReadSize, actualReadSize < buffer.Length);
	}

	/// <summary>
	/// Dispose this object.
	/// </summary>
	public void Dispose()
	{
		reader.Dispose();
		GC.SuppressFinalize(this);
	}

	private readonly VorbisReader reader;
}
