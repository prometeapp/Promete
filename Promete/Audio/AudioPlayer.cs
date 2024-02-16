using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// Provides audio source playback functionality.
/// </summary>
public class AudioPlayer : IDisposable
{
	/// <summary>
	/// Initialize a new instance of <see cref="AudioPlayer"/> .
	/// </summary>
	public unsafe AudioPlayer()
	{
		try
		{
			al = AL.GetApi();
			alc = ALContext.GetApi();
		}
		catch (FileNotFoundException)
		{
			// OpenALがOSに存在しない場合、OpenAL Softを使用する
			al = AL.GetApi(true);
			alc = ALContext.GetApi(true);
		}
		al.DistanceModel(DistanceModel.None);

		var d = alc.OpenDevice("");
		var c = alc.CreateContext(d, null);
		alc.MakeContextCurrent(c);
		device = (nint)d;
		context = (nint)c;
		Gain = 1;
	}

	/// <summary>
	/// Get or set volume.
	/// </summary>
	/// <value>Volume range in 0.0 ~ 1.0.</value>
	public float Gain
	{
		get => gain;
		set => gain = Math.Clamp(value, 0f, 1f);
	}

	/// <summary>
	/// Get or set pan.
	/// </summary>
	/// <value>Volume range in -1.0 ~ 1.0.</value>
	public float Pan
	{
		get => pan;
		set => pan = Math.Clamp(value, -1f, 1f);
	}

	/// <summary>
	/// Get or set pitch of this player.
	/// </summary>
	/// <value>Pitch ratio value. Default is 1.</value>
	public float Pitch { get; set; } = 1;

	/// <summary>
	/// Get whether this player is playing。
	/// </summary>
	public bool IsPlaying { get; private set; }

	/// <summary>
	/// Get current playing time of this player in milliseconds.
	/// </summary>
	public int Time { get; private set; }

	/// <summary>
	/// Get current playing time of this player in samples.
	/// </summary>
	public int TimeInSamples { get; private set; }

	/// <summary>
	/// Get length of loaded audio in milliseconds.
	/// </summary>
	public int Length { get; private set; }

	/// <summary>
	/// Get length of loaded audio in samples.
	/// </summary>
	public int LengthInSamples { get; private set; }

	public int BufferSize { get; set; } = 10000;

	private float gain;
	private float pan;
	private StopTokenSource? stopToken;

	private readonly AL al;
	private readonly ALContext alc;
	private readonly nint context;
	private readonly nint device;

	/// <summary>
	/// Start playing.
	/// </summary>
	/// <param name="source">A <see cref="IAudioSource"/> to play.</param>
	/// <param name="loop">Sample number of loop point. To disable loop, specify<c>null</c>.</param>
	public async ValueTask PlayAsync(IAudioSource source, int? loop = default)
	{
		stopToken?.Stop();
		stopToken = new StopTokenSource();
		await PlayAsync(source, loop, stopToken);
	}

	/// <summary>
	/// Start playing.
	/// </summary>
	/// <param name="source">A <see cref="IAudioSource"/> to play.</param>
	/// <param name="loop">Sample number of loop point. To disable loop, specify<c>null</c>.</param>
	public async void Play(IAudioSource source, int? loop = default)
	{
		if (IsPlaying)
		{
			Stop();
		}

		stopToken = new StopTokenSource();
		await PlayAsync(source, loop, stopToken);
	}

	/// <summary>
	/// Stop playing.
	/// </summary>
	/// <param name="time">Fade-out time. Specify 0 to stop soon.</param>
	public void Stop(float time = 0)
	{
		if (time == 0)
		{
			stopToken?.Stop();
		}
		else
		{
			Task.Run(async () =>
			{
				var firstGain = Gain;
				Stopwatch w = new();
				w.Start();
				while (Gain > 0)
				{
					var current = (w.ElapsedMilliseconds / 1000f) / time;
					Gain = MathHelper.Lerp(current, firstGain, 0);
					await Task.Delay(1);
				}

				stopToken?.Stop();
				w.Stop();
				while (IsPlaying)
					await Task.Delay(10);
				Gain = 1;
			});
		}

		Time = TimeInSamples = 0;
	}

	public async void PlayOneShot(IAudioSource source, float _gain = 1, float pitch = 1, float pan = 0)
	{
		await PlayOneShotAsync(source, _gain, pitch, pan);
	}

	/// <summary>
	/// Play specified <see cref="IAudioSource"/> instantly.
	/// </summary>
	/// <param name="source"><see cref="IAudioSource"/> to play.</param>
	/// <param name="_gain">再生する音量。</param>
	/// <param name="pitch">再生時のピッチ。</param>
	/// <returns></returns>
	public async ValueTask PlayOneShotAsync(IAudioSource source, float _gain = 1, float pitch = 1, float pan = 0)
	{
		if (source.Samples is null)
			throw new ArgumentException("PlayOneShot requires AudioSource which has determined length.");
		var buffer = new short[source.Samples.Value];
		source.FillSamples(buffer, 0);
		using var alSrc = new ALSource(al);
		using var alBuf = new ALBuffer(al);
		al.BufferData(alBuf.Handle, BufferFormat.Stereo16, buffer, source.SampleRate);
		al.SourceQueueBuffers(alSrc.Handle, [alBuf.Handle]);

		al.SourcePlay(alSrc.Handle);
		al.SetSourceProperty(alSrc.Handle, SourceFloat.Gain, _gain);
		al.SetSourceProperty(alSrc.Handle, SourceFloat.Pitch, pitch);
		al.SetSourceProperty(alSrc.Handle, SourceVector3.Position, pan, 0, -MathF.Sqrt(1f - pan * pan));
		al.SetSourceProperty(alSrc.Handle, SourceBoolean.SourceRelative, true);
		al.SetSourceProperty(alSrc.Handle, SourceFloat.MaxDistance, 1);
		al.SetSourceProperty(alSrc.Handle, SourceFloat.ReferenceDistance, 0.5f);

		int buffersProcessed;
		do
		{
			al.GetSourceProperty(alSrc.Handle, GetSourceInteger.BuffersProcessed, out buffersProcessed);
			await Task.Delay(1);
		} while (buffersProcessed < 1);
	}

	/// <summary>
	/// Dispose.
	/// </summary>
	public unsafe void Dispose()
	{
		alc.DestroyContext((Context*)context);
		alc.CloseDevice((Device*)device);
		al.Dispose();
		alc.Dispose();

		GC.SuppressFinalize(this);
	}

	private async ValueTask PlayAsync(IAudioSource source, int? loop, StopTokenSource st)
	{
		var samples = new short[BufferSize];
		TimeInSamples = Time = 0;

		LengthInSamples = source.Samples ?? 0;
		Length = (int)(LengthInSamples / (float)source.SampleRate * 1000);

		using var alSource = new ALSource(al);
		using var buffer1 = new ALBuffer(al);
		using var buffer2 = new ALBuffer(al);
		var currentSample = 0;
		var nextBufferIndex = 0;

		uint[] singleArray = [0];

		int sampleSize;
		bool isFinished;

		QueueData();
		QueueData();

		al.SourcePlay(alSource.Handle);
		IsPlaying = true;

		al.SetSourceProperty(alSource.Handle, SourceBoolean.SourceRelative, true);
		al.SetSourceProperty(alSource.Handle, SourceFloat.MaxDistance, 1);
		al.SetSourceProperty(alSource.Handle, SourceFloat.ReferenceDistance, 0.5f);

		while (true)
		{
			al.SetSourceProperty(alSource.Handle, SourceFloat.Pitch, Pitch);
			al.SetSourceProperty(alSource.Handle, SourceFloat.Gain, Gain);
			al.SetSourceProperty(alSource.Handle, SourceVector3.Position, pan, 0, 0);

			al.GetSourceProperty(alSource.Handle, GetSourceInteger.BuffersProcessed, out var processedCount);

			await Task.Delay(1).ConfigureAwait(false);

			if (st.IsStopRequested)
			{
				IsPlaying = false;
				break;
			}

			if (processedCount == 0) continue;

			DequeueBuffer(nextBufferIndex == 0 ? buffer1 : buffer2);
			QueueData();

			al.GetSourceProperty(alSource.Handle, GetSourceInteger.SourceState, out var state);
			if (state != (int)SourceState.Playing)
				al.SourcePlay(alSource.Handle);

			if (!isFinished) continue;
			if (loop is not {} loopStartSample) break;
			currentSample = loopStartSample * source.Channels;
		}

		if (!st.IsStopRequested)
		{
			int processed;
			do
			{
				al.GetSourceProperty(alSource.Handle, GetSourceInteger.BuffersProcessed, out processed);
				await Task.Yield();
			} while (processed < 2);
		}

		IsPlaying = false;
		return;

		void EnqueueBuffer(ALBuffer alBuffer)
		{
			singleArray[0] = alBuffer.Handle;
			al.SourceQueueBuffers(alSource.Handle, singleArray);
		}

		void DequeueBuffer(ALBuffer alBuffer)
		{
			singleArray[0] = alBuffer.Handle;
			al.SourceUnqueueBuffers(alSource.Handle, singleArray);
		}

		void QueueData()
		{
			(sampleSize, isFinished) = source.FillSamples(samples, currentSample);
			currentSample += sampleSize;
			var nextBuffer = nextBufferIndex == 0 ? buffer1 : buffer2;

			if (isFinished)
			{
				if (sampleSize == 0) return;
				BufferExactSizeDataUnsafely();
			}
			else
			{
				al.BufferData(nextBuffer.Handle, BufferFormat.Stereo16, samples, source.SampleRate);
			}

			EnqueueBuffer(nextBuffer);

			nextBufferIndex ^= 1;
			return;

			unsafe void BufferExactSizeDataUnsafely()
			{
				fixed (short* samplePtr = samples)
				{
					al.BufferData(nextBuffer.Handle, BufferFormat.Stereo16, samplePtr, sampleSize * sizeof(short), source.SampleRate);
				}
			}
		}
	}
}
