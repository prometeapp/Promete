using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

	/// <summary>
	/// 読み込まれているサンプルのサイズを取得します。
	/// </summary>
	public int LoadedSize => loadedSize;

	/// <summary>
	/// 全てのサンプルが読み込まれているかどうかを取得します。
	/// </summary>
	public bool IsLoadingFinished => LoadedSize == Samples;

	private int loadedSize;

	private readonly short[] store;
	private readonly CancellationTokenSource _cts = new();
	private readonly VorbisReader reader;

	public VorbisAudioSource(string path)
		: this(File.OpenRead(path))
	{
	}

	public VorbisAudioSource(Stream stream)
	{
		reader = new VorbisReader(stream);

		var temp = new float[10000];
		store = new short[Samples ?? 0];

		// 別スレッドで非同期にデータを読み込む
		Task.Run(async () =>
		{
			unchecked
			{
				while (true)
				{
					// 10000サンプルずつ読み込む
					var readSamples = reader.ReadSamples(temp, 0, temp.Length);
					if (readSamples == 0) break;

					// 各サンプルを16bit shortに変換
					for (var i = 0; i < readSamples; i++)
					{
						if (_cts.Token.IsCancellationRequested) return;

						store[loadedSize++] = (short)(temp[i] * short.MaxValue);
					}
					await Task.Delay(1, _cts.Token);
				}
			}
		});
	}

	public (int loadedSize, bool isFinished) FillSamples(short[] buffer, int offset)
	{
		var actualReadSize = Math.Min(buffer.Length, LoadedSize - offset);
		Buffer.BlockCopy(store, offset * sizeof(short), buffer, 0, actualReadSize * sizeof(short));
		return (actualReadSize, IsLoadingFinished && actualReadSize < buffer.Length);
	}

	/// <summary>
	/// Dispose this object.
	/// </summary>
	public void Dispose()
	{
		reader.Dispose();
		_cts.Cancel();
		GC.SuppressFinalize(this);
	}
}
