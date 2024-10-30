using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// オーディオデータを再生するためのクラスです。
/// </summary>
public class AudioPlayer : IDisposable
{
	/// <summary>
	/// この <see cref="AudioPlayer"/> の新しいインスタンスを初期化します。
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
	/// 音量を取得または設定します。
	/// </summary>
	/// <value>音量の範囲は 0.0 ～ 1.0 です。</value>
	public float Gain
	{
		get => gain;
		set => gain = Math.Clamp(value, 0f, 1f);
	}

	/// <summary>
	/// パンを取得または設定します。
	/// <value>パンの範囲は -1.0 ～ 1.0 です。</value>
	/// </summary>
	public float Pan
	{
		get => pan;
		set => pan = Math.Clamp(value, -1f, 1f);
	}

	/// <summary>
	/// このプレイヤーのピッチを取得または設定します。
	/// </summary>
	/// <value>ピッチ比率の値。デフォルトは 1 です。</value>
	public float Pitch { get; set; } = 1;

	/// <summary>
	/// このプレイヤーが再生中かどうかを取得します。
	/// </summary>
	public bool IsPlaying { get; private set; }

	/// <summary>
	/// 再生中の音源の現在の再生位置をミリ秒単位で取得します。
	/// </summary>
	public int Time { get; private set; }

	/// <summary>
	/// 再生中の音源の現在の再生位置をサンプル単位で取得します。
	/// </summary>
	public int TimeInSamples { get; private set; }

	/// <summary>
	/// 再生中の音源の長さをミリ秒単位で取得します。
	/// </summary>
	public int Length { get; private set; }

	/// <summary>
	/// 再生中の音源の長さをサンプル単位で取得します。
	/// </summary>
	public int LengthInSamples { get; private set; }

	/// <summary>
	/// このプレイヤーが一時停止中かどうかを取得します。
	/// </summary>
	public bool IsPausing { get; private set; }

	public int BufferSize { get; set; } = 10000;

	private float gain;
	private float pan;
	private StopTokenSource? stopToken;

	private readonly AL al;
	private readonly ALContext alc;
	private readonly nint context;
	private readonly nint device;

	/// <summary>
	/// 再生を開始します。
	/// </summary>
	/// <param name="source">再生する音源。</param>
	/// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
	public async ValueTask PlayAsync(IAudioSource source, int? loop = default)
	{
		stopToken?.Stop();
		stopToken = new StopTokenSource();
		await PlayAsync(source, loop, stopToken);
	}

	/// <summary>
	/// 再生を開始します。
	/// </summary>
	/// <param name="source">再生する音源。</param>
	/// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
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
	/// 再生を停止します。
	/// </summary>
	/// <param name="time">フェードアウトにかかる時間（秒単位）。0を指定した場合は即時停止します。</param>
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

	/// <summary>
	/// 指定した音源をその場で再生します。
	/// </summary>
	/// <param name="source"></param>
	/// <param name="_gain"></param>
	/// <param name="pitch"></param>
	/// <param name="pan"></param>
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

		LengthInSamples = source.Samples / source.Channels ?? 0;
		Length = (int)(LengthInSamples / (float)source.SampleRate * 1000);

		using var alSource = new ALSource(al);
		using var buffer1 = new ALBuffer(al);
		using var buffer2 = new ALBuffer(al);
		int bufferSampleIndex1 = 0, bufferSampleIndex2 = 0;
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

		var prevOffset = 0;

		// 再生ループ
		while (true)
		{
			// 現時点のステータスを取得
			al.SetSourceProperty(alSource.Handle, SourceFloat.Pitch, Pitch);
			al.SetSourceProperty(alSource.Handle, SourceFloat.Gain, Gain);
			al.SetSourceProperty(alSource.Handle, SourceVector3.Position, pan, 0, 0);
			al.GetSourceProperty(alSource.Handle, GetSourceInteger.BuffersProcessed, out var processedCount);

			// ソースが現在再生しているバッファのサンプル位置を取得し、TimeInSamplesを更新
			al.GetSourceProperty(alSource.Handle, GetSourceInteger.Buffer, out var currentBuffer);
			al.GetSourceProperty(alSource.Handle, GetSourceInteger.SampleOffset, out var offset);
			var sampleOffset = currentBuffer == buffer1.Handle ? bufferSampleIndex1 : bufferSampleIndex2;
			TimeInSamples = (sampleOffset + offset) / source.Channels;
			Time = TimeInSamples * 1000 / source.SampleRate;

			await Task.Delay(1).ConfigureAwait(false);

			// 外部から再生停止が要求された場合、再生を終了する
			if (st.IsStopRequested)
			{
				IsPlaying = false;
				break;
			}

			// バッファが全て処理されるまで待機
			if (processedCount == 0) continue;

			// 処理中のバッファがなくなった場合、キューへの詰め直しを行う
			DequeueBuffer(nextBufferIndex == 0 ? buffer1 : buffer2);
			QueueData();

			// ソースの再生状態が停止している場合、再生を再開する
			al.GetSourceProperty(alSource.Handle, GetSourceInteger.SourceState, out var state);
			if (state != (int)SourceState.Playing)
				al.SourcePlay(alSource.Handle);

			// まだ再生が終了していない場合は処理を続行
			if (!isFinished) continue;

			// ループ再生が無効の場合、再生を終了する
			if (loop is not {} loopStartSample) break;

			// ループ再生の開始位置にシーク
			currentSample = loopStartSample * source.Channels;
			TimeInSamples = loopStartSample;
			Time = TimeInSamples * 1000 / source.SampleRate;
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
			if (nextBufferIndex == 0)
				bufferSampleIndex1 = currentSample;
			else
				bufferSampleIndex2 = currentSample;
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
