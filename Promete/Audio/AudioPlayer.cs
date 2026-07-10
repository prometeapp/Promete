using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// オーディオデータを再生するためのクラスです。
/// </summary>
public class AudioPlayer : IDisposable
{
    private readonly AL _al;
    private readonly ALContext _alc;
    private readonly nint _context;
    private readonly nint _device;

    private float _gain;
    private float _pan;
    private int _time;
    private int _timeInSamples;
    private (int value, bool isMs)? _seekRequest;
    private CancellationTokenSource? _currentTokenSource;

    private readonly PrometeApp _app = PrometeApp.Current;

    /// <summary>
    ///     この <see cref="AudioPlayer" /> の新しいインスタンスを初期化します。
    /// </summary>
    public unsafe AudioPlayer()
    {
        _al = AL.GetApi(true);
        _alc = ALContext.GetApi(true);

        _al.DistanceModel(DistanceModel.None);

        var d = _alc.OpenDevice("");
        var c = _alc.CreateContext(d, null);
        _alc.MakeContextCurrent(c);
        _device = (nint)d;
        _context = (nint)c;
        Gain = 1;
    }

    /// <summary>
    /// オーディオソースが終端に達したことにより、再生が終了したときに発生します。
    /// <see cref="Stop"/> メソッド等を呼んでも、このイベントは発生しません。その場合は <see cref="StopPlaying"/> イベントを購読してください。
    /// </summary>
    public event EventHandler? FinishPlaying;

    /// <summary>
    /// オーディオの再生が開始したときに発生します。
    /// </summary>
    public event EventHandler? StartPlaying;

    /// <summary>
    /// オーディオの再生が停止したときに発生します。<see cref="Stop"/> メソッドを呼ばれた場合などに発生します。
    /// ソースが終端に達した場合は、代わりに <see cref="FinishPlaying"/> イベントが発生します。
    /// </summary>
    public event EventHandler? StopPlaying;

    /// <summary>
    /// オーディオソースが終端に達し、ループ再生が行われた瞬間に発生します。
    /// </summary>
    public event EventHandler? Loop;

    /// <summary>
    ///     音量を取得または設定します。
    /// </summary>
    /// <value>音量の範囲は 0.0 ～ 1.0 です。</value>
    public float Gain
    {
        get => _gain;
        set => _gain = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    ///     パンを取得または設定します。
    ///     <value>パンの範囲は -1.0 ～ 1.0 です。</value>
    /// </summary>
    public float Pan
    {
        get => _pan;
        set => _pan = Math.Clamp(value, -1f, 1f);
    }

    /// <summary>
    ///     このプレイヤーのピッチを取得または設定します。
    /// </summary>
    /// <value>ピッチ比率の値。デフォルトは 1 です。</value>
    public float Pitch { get; set; } = 1;

    /// <summary>
    ///     このプレイヤーが再生中かどうかを取得します。
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    ///     再生中の音源の現在の再生位置をミリ秒単位で取得または設定します。
    ///     設定すると、その位置へシークします。範囲外の値は音源の長さにクランプされます。
    ///     再生していないときに設定した場合、次回再生時の開始位置になります。この値は <see cref="Stop"/> によって 0 にリセットされます。
    ///     一時停止中に設定した場合、シークは一時停止の解除後に反映されます。
    /// </summary>
    public int Time
    {
        get => _time;
        set
        {
            var clamped = Math.Max(0, value);
            _seekRequest = (clamped, true);
            _time = clamped;
        }
    }

    /// <summary>
    ///     再生中の音源の現在の再生位置をサンプル単位で取得または設定します。
    ///     設定すると、その位置へシークします。範囲外の値は音源の長さにクランプされます。
    ///     再生していないときに設定した場合、次回再生時の開始位置になります。この値は <see cref="Stop"/> によって 0 にリセットされます。
    ///     一時停止中に設定した場合、シークは一時停止の解除後に反映されます。
    /// </summary>
    public int TimeInSamples
    {
        get => _timeInSamples;
        set
        {
            var clamped = Math.Max(0, value);
            _seekRequest = (clamped, false);
            _timeInSamples = clamped;
        }
    }

    /// <summary>
    ///     再生中の音源の長さをミリ秒単位で取得します。
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    ///     再生中の音源の長さをサンプル単位で取得します。
    /// </summary>
    public int LengthInSamples { get; private set; }

    /// <summary>
    ///     このプレイヤーが一時停止中かどうかを取得します。
    /// </summary>
    public bool IsPausing { get; private set; }

    /// <summary>
    ///     オーディオバッファのサイズを取得または設定します。
    /// </summary>
    public int BufferSize { get; set; } = 10000;

    /// <summary>
    ///     リソースを解放します。
    /// </summary>
    public unsafe void Dispose()
    {
        Stop();
        _alc.DestroyContext((Context*)_context);
        _alc.CloseDevice((Device*)_device);
        _al.Dispose();
        _alc.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     再生を開始します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
    public async ValueTask PlayAsync(IAudioSource source, int? loop = null)
    {
        if (_currentTokenSource is not null)
        {
            await _currentTokenSource.CancelAsync();
        }
        _currentTokenSource = new CancellationTokenSource();
        await PlayAsync(source, loop, _currentTokenSource.Token);
    }

    /// <summary>
    ///     再生を開始します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
    public async void Play(IAudioSource source, int? loop = null)
    {
        if (IsPlaying)
            Stop();

        _currentTokenSource = new CancellationTokenSource();
        await PlayAsync(source, loop, _currentTokenSource.Token);
    }

    /// <summary>
    ///     一時停止します。
    /// </summary>
    public void Pause()
    {
        if (!IsPlaying)
            return;
        IsPausing = true;
    }

    /// <summary>
    ///     一時停止を解除します。
    /// </summary>
    public void Resume()
    {
        if (!IsPausing)
            return;
        IsPausing = false;
    }

    /// <summary>
    ///     再生を停止します。
    /// </summary>
    /// <param name="time">フェードアウトにかかる時間（秒単位）。0を指定した場合は即時停止します。</param>
    public void Stop(float time = 0)
    {
        if (time == 0)
            _currentTokenSource?.Cancel();
        else
            Task.Run(async () =>
            {
                var firstGain = Gain;
                Stopwatch w = new();
                w.Start();
                while (Gain > 0)
                {
                    var current = w.ElapsedMilliseconds / 1000f / time;
                    Gain = MathHelper.Lerp(current, firstGain, 0);
                    await Task.Delay(1);
                }

                if (_currentTokenSource is not null)
                {
                    await _currentTokenSource.CancelAsync();
                }

                w.Stop();
                while (IsPlaying)
                    await Task.Delay(10);
                Gain = 1;
            });

        _time = _timeInSamples = 0;
        _seekRequest = null;
        IsPlaying = false;
        IsPausing = false;
    }

    /// <summary>
    ///     指定した音源をその場で再生します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="gain">再生する音量。</param>
    /// <param name="pitch">再生時のピッチ。</param>
    /// <param name="pan">再生時のパン。</param>
    public async void PlayOneShot(
        IAudioSource source,
        float gain = 1,
        float pitch = 1,
        float pan = 0
    )
    {
        await PlayOneShotAsync(source, gain, pitch, pan);
    }

    /// <summary>
    ///     指定した音源をその場で再生します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="gain">再生する音量。</param>
    /// <param name="pitch">再生時のピッチ。</param>
    /// <param name="pan">再生時のパン。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async ValueTask PlayOneShotAsync(
        IAudioSource source,
        float gain = 1,
        float pitch = 1,
        float pan = 0
    )
    {
        if (source.Frames is null)
            throw new ArgumentException(
                "PlayOneShot requires AudioSource which has determined length."
            );
        var floatBuffer = new float[source.Frames.Value * source.Channels];
        source.FillSamples(floatBuffer, 0);
        var buffer = ToInt16Buffer(floatBuffer);
        using var alSrc = new ALSource(_al);
        using var alBuf = new ALBuffer(_al);
        var bufferFormat = GetBufferFormat(source);

        _al.BufferData(alBuf.Handle, bufferFormat, buffer, source.SampleRate);
        _al.SourceQueueBuffers(alSrc.Handle, new uint[] { alBuf.Handle });
        _al.SetSourceProperty(alSrc.Handle, SourceFloat.Gain, gain);
        _al.SetSourceProperty(alSrc.Handle, SourceFloat.Pitch, pitch);
        var x = pan;
        var z = MathF.Abs(_pan) < 1.0f ? -MathF.Sqrt(1.0f - _pan * _pan) : 0.0f;
        _al.SetSourceProperty(alSrc.Handle, SourceVector3.Position, x, 0, z);
        _al.SetSourceProperty(alSrc.Handle, SourceBoolean.SourceRelative, true);
        _al.SetSourceProperty(alSrc.Handle, SourceFloat.MaxDistance, 1);
        _al.SetSourceProperty(alSrc.Handle, SourceFloat.ReferenceDistance, 0.5f);

        _al.SourcePlay(alSrc.Handle);

        int buffersProcessed;
        do
        {
            _al.GetSourceProperty(
                alSrc.Handle,
                GetSourceInteger.BuffersProcessed,
                out buffersProcessed
            );
            await Task.Delay(1);
        } while (buffersProcessed < 1);
    }

    private async ValueTask PlayAsync(IAudioSource source, int? loop, CancellationToken token)
    {
        try
        {
            var samples = new short[BufferSize];
            var floatSamples = new float[BufferSize];

            LengthInSamples = source.Frames ?? 0;
            Length = (int)(LengthInSamples / (float)source.SampleRate * 1000);

            // 再生開始前にシークリクエストがあれば、それを開始位置とする
            var startSample = ConsumeSeekRequest(source) ?? 0;
            _timeInSamples = startSample;
            _time = (int)(startSample * 1000L / source.SampleRate);

            using var alSource = new ALSource(_al);
            using var buffer1 = new ALBuffer(_al);
            using var buffer2 = new ALBuffer(_al);
            int bufferSampleIndex1 = 0,
                bufferSampleIndex2 = 0;
            var currentSample = startSample * source.Channels;
            var nextBufferIndex = 0;
            var bufferFormat = GetBufferFormat(source);

            var singleArray = new uint[1];

            int sampleSize;
            bool isFinished;

            QueueData();
            QueueData();

            _al.SourcePlay(alSource.Handle);
            IsPlaying = true;

            _al.SetSourceProperty(alSource.Handle, SourceBoolean.SourceRelative, true);
            _al.SetSourceProperty(alSource.Handle, SourceFloat.MaxDistance, 1);
            _al.SetSourceProperty(alSource.Handle, SourceFloat.ReferenceDistance, 0.5f);

            _app.NextFrame(() => StartPlaying?.Invoke(this, EventArgs.Empty));

            // 再生ループ
            while (true)
            {
                // 現時点のステータスを取得
                _al.SetSourceProperty(alSource.Handle, SourceFloat.Pitch, Pitch);
                _al.SetSourceProperty(alSource.Handle, SourceFloat.Gain, Gain);
                var x = _pan;
                var z = MathF.Abs(_pan) < 1.0f ? -MathF.Sqrt(1.0f - _pan * _pan) : 0.0f;
                _al.SetSourceProperty(alSource.Handle, SourceVector3.Position, x, 0, z);
                _al.GetSourceProperty(
                    alSource.Handle,
                    GetSourceInteger.BuffersProcessed,
                    out var processedCount
                );

                // ソースが現在再生しているバッファのサンプル位置を取得し、TimeInSamplesを更新
                _al.GetSourceProperty(
                    alSource.Handle,
                    GetSourceInteger.Buffer,
                    out var currentBuffer
                );
                _al.GetSourceProperty(
                    alSource.Handle,
                    GetSourceInteger.SampleOffset,
                    out var offset
                );
                var sampleOffset =
                    currentBuffer == buffer1.Handle ? bufferSampleIndex1 : bufferSampleIndex2;
                _timeInSamples = (sampleOffset + offset) / source.Channels;
                _time = (int)(_timeInSamples * 1000L / source.SampleRate);

                // このスレッドがCPUを占有しないように待ち時間を挟む
                await Task.Delay(1, token).ConfigureAwait(false);

                // シークリクエストがある場合、読み出し位置を差し替えてバッファを詰め直す
                if (_seekRequest is not null)
                {
                    var seekTo = ConsumeSeekRequest(source)!.Value;

                    _al.SourceStop(alSource.Handle);
                    _al.GetSourceProperty(
                        alSource.Handle,
                        GetSourceInteger.BuffersQueued,
                        out var queuedCount
                    );
                    for (var i = 0; i < queuedCount; i++)
                        _al.SourceUnqueueBuffers(alSource.Handle, singleArray);

                    currentSample = seekTo * source.Channels;
                    nextBufferIndex = 0;
                    QueueData();
                    QueueData();
                    _al.SourcePlay(alSource.Handle);

                    _timeInSamples = seekTo;
                    _time = (int)(seekTo * 1000L / source.SampleRate);
                    continue;
                }

                // ポーズ中の場合、再生を一時停止する
                if (IsPausing)
                {
                    _al.SourcePause(alSource.Handle);
                    while (IsPausing)
                        await Task.Delay(1, token).ConfigureAwait(false);
                    _al.SourcePlay(alSource.Handle);
                }

                // バッファが全て処理されるまで待機
                if (processedCount == 0)
                    continue;

                // 処理中のバッファがなくなった場合、キューへの詰め直しを行う
                DequeueBuffer(nextBufferIndex == 0 ? buffer1 : buffer2);
                QueueData();

                // ソースの再生状態が停止している場合、再生を再開する
                _al.GetSourceProperty(alSource.Handle, GetSourceInteger.SourceState, out var state);
                if (state != (int)SourceState.Playing)
                    _al.SourcePlay(alSource.Handle);

                // まだ再生が終了していない場合は処理を続行
                if (!isFinished)
                    continue;

                // ループ再生が無効の場合、再生を終了する
                if (loop is not { } loopStartSample)
                    break;

                // ループ再生の開始位置にシーク
                currentSample = loopStartSample * source.Channels;
                _timeInSamples = loopStartSample;
                _time = (int)(loopStartSample * 1000L / source.SampleRate);

                _app.NextFrame(() => Loop?.Invoke(this, EventArgs.Empty));
            }

            // 停止を要求されずにループを抜けた場合、バッファを全て処理し終えるまで待機
            int processed;
            do
            {
                _al.GetSourceProperty(
                    alSource.Handle,
                    GetSourceInteger.BuffersProcessed,
                    out processed
                );
                await Task.Yield();
            } while (processed < 2);

            // また、再生が終了したことを通知する
            _app.NextFrame(() => FinishPlaying?.Invoke(this, EventArgs.Empty));

            IsPlaying = false;
            return;

            void EnqueueBuffer(ALBuffer alBuffer)
            {
                singleArray[0] = alBuffer.Handle;
                _al.SourceQueueBuffers(alSource.Handle, singleArray);
            }

            void DequeueBuffer(ALBuffer alBuffer)
            {
                singleArray[0] = alBuffer.Handle;
                _al.SourceUnqueueBuffers(alSource.Handle, singleArray);
            }

            void QueueData()
            {
                int filledFrames;
                (filledFrames, isFinished) = source.FillSamples(
                    floatSamples,
                    currentSample / source.Channels
                );
                sampleSize = filledFrames * source.Channels;
                ToInt16Buffer(floatSamples.AsSpan(0, sampleSize), samples);

                if (nextBufferIndex == 0)
                    bufferSampleIndex1 = currentSample;
                else
                    bufferSampleIndex2 = currentSample;
                currentSample += sampleSize;
                var nextBuffer = nextBufferIndex == 0 ? buffer1 : buffer2;

                if (isFinished)
                {
                    if (sampleSize == 0)
                        return;
                    BufferExactSizeDataUnsafely();
                }
                else
                {
                    _al.BufferData(nextBuffer.Handle, bufferFormat, samples, source.SampleRate);
                }

                EnqueueBuffer(nextBuffer);

                nextBufferIndex ^= 1;
                return;

                unsafe void BufferExactSizeDataUnsafely()
                {
                    fixed (short* samplePtr = samples)
                    {
                        _al.BufferData(
                            nextBuffer.Handle,
                            bufferFormat,
                            samplePtr,
                            sampleSize * sizeof(short),
                            source.SampleRate
                        );
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            // 再生停止要求のため、このまま終了
            _app.NextFrame(() => StopPlaying?.Invoke(this, EventArgs.Empty));
        }
    }

    /// <summary>
    /// 未消費のシークリクエストをサンプル単位に換算して取り出します。リクエストがなければ <c>null</c> を返します。
    /// </summary>
    private int? ConsumeSeekRequest(IAudioSource source)
    {
        if (_seekRequest is not { } request)
            return null;
        _seekRequest = null;
        var samples = request.isMs
            ? (int)((long)request.value * source.SampleRate / 1000)
            : request.value;
        return Math.Clamp(samples, 0, Math.Max(0, LengthInSamples - 1));
    }

    private BufferFormat GetBufferFormat(IAudioSource source)
    {
        return source.Channels switch
        {
            1 => BufferFormat.Mono16,
            2 => BufferFormat.Stereo16,
            _ => throw new NotSupportedException("Unsupported format."),
        };
    }

    /// <summary>
    /// float PCM (-1.0～1.0) を16bit整数PCMへクランプ付きで変換します。
    /// AudioPlayerの内部処理は暫定的にshortバッファを使い続けているための橋渡し用です（フェーズ3で置き換え予定）。
    /// </summary>
    private static void ToInt16Buffer(ReadOnlySpan<float> source, Span<short> destination)
    {
        for (var i = 0; i < source.Length; i++)
        {
            var clamped = Math.Clamp(source[i], -1f, 1f);
            destination[i] = (short)(clamped * short.MaxValue);
        }
    }

    /// <summary>
    /// float PCM (-1.0～1.0) を16bit整数PCMの新しい配列へクランプ付きで変換します。
    /// </summary>
    private static short[] ToInt16Buffer(ReadOnlySpan<float> source)
    {
        var result = new short[source.Length];
        ToInt16Buffer(source, result);
        return result;
    }
}
