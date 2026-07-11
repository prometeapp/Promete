using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Promete.Audio.Filters;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// オーディオデータを再生するためのクラスです。
/// </summary>
public class AudioPlayer : IDisposable
{
    private readonly AudioDevice? _audioDevice;
    private readonly IAudioOutput _output;
    private readonly AudioRenderPipeline _pipeline = new();
    private readonly PrometeApp? _app;
    private readonly bool _ownsOutput;
    private readonly ObservableCollection<IAudioFilter> _filters = [];

    private IAudioSource? _currentSource;
    private TaskCompletionSource? _playCompletion;
    private long? _requestedStartFrames;
    private bool _isOutputStarted;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
    /// この <see cref="AudioPlayer" /> の新しいインスタンスを初期化します。
    /// 共有 <see cref="AudioDevice"/> を取得し、実デバイスへの出力を即座に開始します（常駐レンダーループ）。
    /// </summary>
    public AudioPlayer()
    {
        _app = TryGetCurrentApp();
        _audioDevice = AudioDevice.Acquire();
        _output = new OpenALAudioOutput(_audioDevice);
        _ownsOutput = true;
        SubscribeFilterChanges();
        SubscribePipelineEvents();
        StartOutput();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioPlayer"/> class.
    /// この <see cref="AudioPlayer" /> の新しいインスタンスを、指定した <see cref="IAudioOutput"/> を使用して初期化します。
    /// 主にテスト用途です。この場合、共有デバイスの取得は行われません。
    /// </summary>
    /// <param name="output">出力先として使用する <see cref="IAudioOutput"/>。</param>
    public AudioPlayer(IAudioOutput output)
    {
        _app = TryGetCurrentApp();
        _audioDevice = null;
        _output = output;
        _ownsOutput = false;
        SubscribeFilterChanges();
        SubscribePipelineEvents();
        StartOutput();
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
        get => _pipeline.Gain;
        set => _pipeline.Gain = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    ///     パンを取得または設定します。
    ///     <value>パンの範囲は -1.0 ～ 1.0 です。</value>
    /// </summary>
    public float Pan
    {
        get => _pipeline.Pan;
        set => _pipeline.Pan = Math.Clamp(value, -1f, 1f);
    }

    /// <summary>
    ///     このプレイヤーのピッチを取得または設定します。
    /// </summary>
    /// <value>ピッチ比率の値。デフォルトは 1 です。</value>
    public float Pitch
    {
        get => _output.Pitch;
        set => _output.Pitch = value;
    }

    /// <summary>
    ///     このプレイヤーが再生中かどうかを取得します。フェードアウト中も <c>true</c> を返します。
    /// </summary>
    public bool IsPlaying => _pipeline.IsPlaying;

    /// <summary>
    ///     再生中の音源の現在の再生位置をミリ秒単位で取得または設定します。
    ///     設定すると、その位置へシークします。範囲外の値は音源の長さにクランプされます。
    ///     再生していないときに設定した場合、次回再生時の開始位置になります。この値は <see cref="Stop"/> によって 0 にリセットされます。
    ///     一時停止中に設定した場合、シークは一時停止の解除後に反映されます。
    /// </summary>
    public int Time
    {
        get => (int)(TimeInSamples * 1000L / Math.Max(1, _pipeline.SampleRate));
        set
        {
            var sampleRate = _pipeline.SampleRate;
            var frames = (long)value * sampleRate / 1000;
            TimeInSamples = (int)Math.Clamp(frames, 0, int.MaxValue);
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
        get =>
            IsPlaying
                ? (int)Math.Min(_pipeline.PositionInFrames, int.MaxValue)
                : (int)Math.Min(_requestedStartFrames ?? 0, int.MaxValue);
        set
        {
            var clamped = Math.Max(0, value);
            if (!IsPlaying)
            {
                // 再生していないときの設定は、次回再生時の開始位置として保持する（クランプは再生開始時に行う）
                _requestedStartFrames = clamped;
                return;
            }

            var max = LengthInSamples > 0 ? LengthInSamples - 1 : clamped;
            _pipeline.Seek(Math.Clamp(clamped, 0, Math.Max(0, max)));
        }
    }

    /// <summary>
    ///     再生中の音源の長さをミリ秒単位で取得します。
    /// </summary>
    public int Length =>
        (int)(
            (long)LengthInSamples
            * 1000
            / Math.Max(1, _currentSource?.SampleRate ?? _pipeline.SampleRate)
        );

    /// <summary>
    ///     再生中の音源の長さをサンプル単位で取得します。
    /// </summary>
    public int LengthInSamples => _currentSource?.Frames ?? 0;

    /// <summary>
    ///     このプレイヤーが一時停止中かどうかを取得します。
    /// </summary>
    public bool IsPausing => _pipeline.IsPausing;

    /// <summary>
    ///     オーディオバッファのサイズを取得または設定します。単位は1バッファあたりのフレーム数です。
    ///     デフォルトは 1024 フレーム（44.1kHz でバッファあたり約 23ms、トリプルバッファで実レイテンシ約 70ms）です。
    ///     出力開始後に変更しても、次回の出力開始まで反映されません。
    /// </summary>
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    ///     この <see cref="AudioPlayer"/> の出力段に適用する DSP フィルターのチェーンを取得します。
    ///     <c>Filters.Add(new DelayFilter())</c> のように追加・削除でき、変更は次のバッファから反映されます。
    ///     フィルターは再生停止中・無音中も毎バッファ呼び出され続けるため、ディレイの残響などが自然に鳴り切ります。
    /// </summary>
    public ObservableCollection<IAudioFilter> Filters => _filters;

    /// <summary>
    ///     再生を開始します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
    public void Play(IAudioSource source, int? loop = null)
    {
        // 前の再生を await しているタスクは、差し替え時点で完了扱いにする
        _playCompletion?.TrySetResult();

        var startFrames = _requestedStartFrames ?? 0;
        _requestedStartFrames = null;

        _currentSource = source;
        _playCompletion = new TaskCompletionSource();
        _pipeline.Play(source, loop, startFrames);
    }

    /// <summary>
    ///     再生を開始します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="loop">ループ開始位置（サンプル単位）。ループ再生を行わない場合は<c>null</c>を指定します。</param>
    /// <returns>再生が終了する（終端到達・<see cref="Stop"/>・別の音源への差し替え）まで待機するタスク。</returns>
    public ValueTask PlayAsync(IAudioSource source, int? loop = null)
    {
        Play(source, loop);
        return new ValueTask(_playCompletion!.Task);
    }

    /// <summary>
    ///     一時停止します。
    /// </summary>
    public void Pause()
    {
        _pipeline.Pause();
    }

    /// <summary>
    ///     一時停止を解除します。
    /// </summary>
    public void Resume()
    {
        _pipeline.Resume();
    }

    /// <summary>
    ///     再生を停止します。
    /// </summary>
    /// <param name="time">フェードアウトにかかる時間（秒単位）。0を指定した場合は即時停止します。</param>
    public void Stop(float time = 0)
    {
        _requestedStartFrames = null;
        _pipeline.Stop(time);
    }

    /// <summary>
    ///     指定した音源をその場で再生します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="gain">再生する音量。</param>
    /// <param name="pitch">再生時のピッチ。</param>
    /// <param name="pan">再生時のパン。</param>
    /// <param name="followsMasterGain">このプレイヤーの <see cref="Gain"/> を乗算するかどうか。デフォルトは <c>false</c>。</param>
    public async void PlayOneShot(
        IAudioSource source,
        float gain = 1,
        float pitch = 1,
        float pan = 0,
        bool followsMasterGain = false
    )
    {
        try
        {
            await PlayOneShotAsync(source, gain, pitch, pan, followsMasterGain);
        }
        catch (Exception e)
        {
            // fire-and-forget のため、例外が未観測のままプロセスを落とさないよう握りつぶしてログに残す
            System.Diagnostics.Debug.WriteLine($"AudioPlayer.PlayOneShot failed: {e}");
        }
    }

    /// <summary>
    ///     指定した音源をその場で再生します。
    /// </summary>
    /// <param name="source">再生する音源。</param>
    /// <param name="gain">再生する音量。</param>
    /// <param name="pitch">再生時のピッチ。</param>
    /// <param name="pan">再生時のパン。</param>
    /// <param name="followsMasterGain">このプレイヤーの <see cref="Gain"/> を乗算するかどうか。デフォルトは <c>false</c>。</param>
    /// <returns>非同期操作を表すタスク。</returns>
    public async ValueTask PlayOneShotAsync(
        IAudioSource source,
        float gain = 1,
        float pitch = 1,
        float pan = 0,
        bool followsMasterGain = false
    )
    {
        if (source.Frames is null)
            throw new ArgumentException(
                "PlayOneShot requires AudioSource which has determined length."
            );

        var device = _audioDevice ?? AudioDevice.Acquire();
        var al = device.Al;
        try
        {
            var floatBuffer = new float[source.Frames.Value * source.Channels];
            source.FillSamples(floatBuffer, 0);
            var buffer = ToInt16Buffer(floatBuffer);
            using var alSrc = new ALSource(al);
            using var alBuf = new ALBuffer(al);
            var bufferFormat = GetBufferFormat(source);

            var effectiveGain = followsMasterGain ? gain * Gain : gain;

            al.BufferData(alBuf.Handle, bufferFormat, buffer, source.SampleRate);
            al.SourceQueueBuffers(alSrc.Handle, new uint[] { alBuf.Handle });
            al.SetSourceProperty(alSrc.Handle, SourceFloat.Gain, effectiveGain);
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
        finally
        {
            if (_audioDevice is null)
                device.Dispose();
        }
    }

    /// <summary>
    ///     リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        if (_isOutputStarted)
            _output.Stop();

        if (_ownsOutput)
            _output.Dispose();

        _audioDevice?.Dispose();

        GC.SuppressFinalize(this);
    }

    private static PrometeApp? TryGetCurrentApp()
    {
        try
        {
            return PrometeApp.Current;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
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

    private void SubscribeFilterChanges()
    {
        _filters.CollectionChanged += (_, _) => _pipeline.SetFilters(_filters.ToArray());
    }

    private void SubscribePipelineEvents()
    {
        _pipeline.StartPlaying += () => Dispatch(() => StartPlaying?.Invoke(this, EventArgs.Empty));
        _pipeline.StopPlaying += () =>
        {
            _playCompletion?.TrySetResult();
            Dispatch(() => StopPlaying?.Invoke(this, EventArgs.Empty));
        };
        _pipeline.FinishPlaying += () =>
        {
            _playCompletion?.TrySetResult();
            Dispatch(() => FinishPlaying?.Invoke(this, EventArgs.Empty));
        };
        _pipeline.Looped += () => Dispatch(() => Loop?.Invoke(this, EventArgs.Empty));
    }

    private void Dispatch(Action action)
    {
        if (_app is not null)
            _app.NextFrame(action);
        else
            action();
    }

    private void StartOutput()
    {
        _output.Start(
            _pipeline.Render,
            2,
            _pipeline.SampleRate,
            BufferSize,
            () => _pipeline.SampleRate
        );
        _isOutputStarted = true;
    }
}
