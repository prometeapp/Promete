using System;
using System.Threading;

namespace Promete.Audio;

/// <summary>
/// オーディオ再生の状態機械と PCM 生成ロジックを保持する、常駐レンダーループの中核です。
/// OpenAL やスレッドなど実行環境に依存しない純粋なロジックとして実装されており、
/// <see cref="IAudioOutput"/> の実装（<see cref="CaptureAudioOutput"/> など）と組み合わせて
/// 決定的なテストが行えます。
/// </summary>
internal sealed class AudioRenderPipeline
{
    private const int DefaultSampleRate = 44100;

    private readonly Lock _lock = new();

    private AudioRenderPipelineState _state = AudioRenderPipelineState.Stopped;
    private IAudioSource? _source;
    private long _cursorFrames;
    private long? _loopStartFrames;
    private float _gain = 1f;
    private float _pan;
    private float _fadeSeconds;
    private float _fadeElapsedSeconds;
    private float _fadeStartGainScale = 1f;

    private readonly System.Collections.Generic.Queue<PendingCommand> _pendingCommands = new();
    private float[] _sourceBuffer = [];

    /// <summary>
    /// 再生が開始されたときに、レンダースレッドから発生します。
    /// </summary>
    public event Action? StartPlaying;

    /// <summary>
    /// 再生が（終端到達以外の理由で）停止したときに、レンダースレッドから発生します。
    /// </summary>
    public event Action? StopPlaying;

    /// <summary>
    /// オーディオソースが終端に達し、再生が終了したときに、レンダースレッドから発生します。
    /// </summary>
    public event Action? FinishPlaying;

    /// <summary>
    /// ループ再生の巻き戻しが発生したときに、レンダースレッドから発生します。
    /// </summary>
    public event Action? Looped;

    private enum PendingCommandKind
    {
        Play,
        Stop,
        Seek,
    }

    /// <summary>
    /// このプレイヤーが再生中（フェードアウト中を含む）かどうかを取得します。
    /// </summary>
    public bool IsPlaying
    {
        get
        {
            lock (_lock)
            {
                return _state
                    is AudioRenderPipelineState.Playing
                        or AudioRenderPipelineState.Pausing
                        or AudioRenderPipelineState.FadingOut;
            }
        }
    }

    /// <summary>
    /// このプレイヤーが一時停止中かどうかを取得します。
    /// </summary>
    public bool IsPausing
    {
        get
        {
            lock (_lock)
            {
                return _state == AudioRenderPipelineState.Pausing;
            }
        }
    }

    /// <summary>
    /// 現在の再生カーソル位置をフレーム単位で取得します。
    /// </summary>
    public long PositionInFrames
    {
        get
        {
            lock (_lock)
            {
                return _cursorFrames;
            }
        }
    }

    /// <summary>
    /// 出力すべきサンプリング周波数を取得します。カレントソースが設定されていない場合は 44100 を返します。
    /// </summary>
    public int SampleRate
    {
        get
        {
            lock (_lock)
            {
                return _source?.SampleRate ?? DefaultSampleRate;
            }
        }
    }

    /// <summary>
    /// 音量を取得または設定します。フェードアウト中でもこの値自体は書き換わりません。
    /// </summary>
    public float Gain
    {
        get
        {
            lock (_lock)
            {
                return _gain;
            }
        }
        set
        {
            lock (_lock)
            {
                _gain = value;
            }
        }
    }

    /// <summary>
    /// パンを取得または設定します。範囲は -1.0 (左) ～ 1.0 (右) です。
    /// </summary>
    public float Pan
    {
        get
        {
            lock (_lock)
            {
                return _pan;
            }
        }
        set
        {
            lock (_lock)
            {
                _pan = value;
            }
        }
    }

    /// <summary>
    /// 再生を開始するコマンドを発行します。適用はバッファ境界（次の <see cref="Render"/> 冒頭）で行われますが、
    /// <see cref="IsPlaying"/> はこの呼び出し直後から <c>true</c> になります。
    /// </summary>
    /// <param name="source">再生するオーディオソース。</param>
    /// <param name="loopStartFrames">ループ開始位置（フレーム単位）。ループしない場合は <c>null</c>。</param>
    /// <param name="startFrames">再生を開始するフレーム位置。ソースの長さでクランプされます。</param>
    public void Play(IAudioSource source, long? loopStartFrames, long startFrames = 0)
    {
        lock (_lock)
        {
            _pendingCommands.Enqueue(
                new PendingCommand
                {
                    Kind = PendingCommandKind.Play,
                    Source = source,
                    LoopStartFrames = loopStartFrames,
                    SeekFrames = startFrames,
                }
            );

            // 観測可能な状態はコマンド発行時点で即座に反映する
            _state = AudioRenderPipelineState.Playing;
        }
    }

    /// <summary>
    /// 再生を停止するコマンドを発行します。
    /// </summary>
    /// <param name="fadeSeconds">フェードアウトにかける時間（秒）。0 以下の場合は即時停止します。</param>
    public void Stop(float fadeSeconds = 0)
    {
        lock (_lock)
        {
            _pendingCommands.Enqueue(
                new PendingCommand { Kind = PendingCommandKind.Stop, FadeSeconds = fadeSeconds }
            );

            _state =
                fadeSeconds > 0 && _state != AudioRenderPipelineState.Stopped
                    ? AudioRenderPipelineState.FadingOut
                    : AudioRenderPipelineState.Stopped;
        }
    }

    /// <summary>
    /// 一時停止するコマンドを発行します。再生中でない場合は何もしません。
    /// </summary>
    public void Pause()
    {
        lock (_lock)
        {
            if (_state != AudioRenderPipelineState.Playing)
                return;

            _state = AudioRenderPipelineState.Pausing;
        }
    }

    /// <summary>
    /// 一時停止を解除するコマンドを発行します。一時停止中でない場合は何もしません。
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            if (_state != AudioRenderPipelineState.Pausing)
                return;

            _state = AudioRenderPipelineState.Playing;
        }
    }

    /// <summary>
    /// 指定したフレーム位置へシークするコマンドを発行します。
    /// </summary>
    /// <param name="frames">シーク先のフレーム位置。</param>
    public void Seek(long frames)
    {
        lock (_lock)
        {
            _pendingCommands.Enqueue(
                new PendingCommand
                {
                    Kind = PendingCommandKind.Seek,
                    SeekFrames = Math.Max(0, frames),
                }
            );

            // Time の get は即座にシーク後の値を返す必要があるため、カーソルも即時更新する
            _cursorFrames = Math.Max(0, frames);
        }
    }

    /// <summary>
    /// 1バッファ分の interleaved float PCM（ステレオ固定）を生成します。<see cref="AudioRenderCallback"/> として使用します。
    /// </summary>
    /// <param name="buffer">書き込み先のバッファ。要素数は「フレーム数 × 2」である必要があります。</param>
    public void Render(Span<float> buffer)
    {
        Action? startPlaying = null;
        Action? stopPlaying = null;
        Action? finishPlaying = null;
        var loopedCount = 0;

        lock (_lock)
        {
            ApplyPendingCommands(ref startPlaying, ref stopPlaying);
            RenderLocked(buffer, ref stopPlaying, ref finishPlaying, ref loopedCount);
        }

        startPlaying?.Invoke();
        stopPlaying?.Invoke();
        finishPlaying?.Invoke();
        for (var i = 0; i < loopedCount; i++)
            Looped?.Invoke();
    }

    private void ApplyPendingCommands(ref Action? startPlaying, ref Action? stopPlaying)
    {
        while (_pendingCommands.TryDequeue(out var command))
        {
            switch (command.Kind)
            {
                case PendingCommandKind.Play:
                    if (_source is not null)
                        stopPlaying = () => StopPlaying?.Invoke();
                    _source = command.Source;
                    _loopStartFrames = command.LoopStartFrames;
                    _cursorFrames = ClampToSourceLength(command.SeekFrames);
                    _fadeSeconds = 0;
                    _fadeElapsedSeconds = 0;
                    startPlaying = () => StartPlaying?.Invoke();
                    break;

                case PendingCommandKind.Stop:
                    if (command.FadeSeconds > 0 && _source is not null)
                    {
                        _fadeSeconds = command.FadeSeconds;
                        _fadeElapsedSeconds = 0;
                        _fadeStartGainScale = CurrentFadeScale();
                    }
                    else
                    {
                        if (_source is not null)
                            stopPlaying = () => StopPlaying?.Invoke();
                        _source = null;
                        _cursorFrames = 0;
                        _fadeSeconds = 0;
                        _fadeElapsedSeconds = 0;
                    }

                    break;
                case PendingCommandKind.Seek:
                    _cursorFrames = ClampToSourceLength(command.SeekFrames);
                    break;
            }
        }
    }

    /// <summary>
    /// フレーム位置をカレントソースの長さ内にクランプします。長さ未確定のソースではそのまま返します。
    /// </summary>
    private long ClampToSourceLength(long frames)
    {
        frames = Math.Max(0, frames);
        if (_source?.Frames is not { } totalFrames)
            return frames;
        return Math.Min(frames, Math.Max(0, totalFrames - 1));
    }

    private float CurrentFadeScale()
    {
        if (_state != AudioRenderPipelineState.FadingOut || _fadeSeconds <= 0)
            return 1f;
        var progress = Math.Clamp(_fadeElapsedSeconds / _fadeSeconds, 0f, 1f);
        return _fadeStartGainScale * (1f - progress);
    }

    private void RenderLocked(
        Span<float> buffer,
        ref Action? stopPlaying,
        ref Action? finishPlaying,
        ref int loopedCount
    )
    {
        var frameCount = buffer.Length / 2;
        var frame = 0;
        var zeroFillStreak = 0;

        // バッファ内でソース終端をまたいでループ再生を継続できるよう、セグメント単位で充填する
        while (frame < frameCount)
        {
            if (
                _state is AudioRenderPipelineState.Stopped or AudioRenderPipelineState.Pausing
                || _source is null
            )
            {
                buffer[(frame * 2)..].Clear();
                return;
            }

            var source = _source;
            var channels = source.Channels;
            var remainingFrames = frameCount - frame;
            var requiredSamples = remainingFrames * channels;
            if (_sourceBuffer.Length < requiredSamples)
                _sourceBuffer = new float[requiredSamples];

            var (filledFrames, isFinished) = source.FillSamples(
                _sourceBuffer.AsSpan(0, requiredSamples),
                (int)Math.Min(_cursorFrames, int.MaxValue)
            );

            WriteFramesLocked(buffer, frame, filledFrames, channels, ref stopPlaying);
            frame += filledFrames;
            if (_state == AudioRenderPipelineState.Stopped)
                return;

            // ループ開始位置が終端以降にある等、読み進められないまま巻き戻しが繰り返される場合のガード
            zeroFillStreak = filledFrames == 0 ? zeroFillStreak + 1 : 0;
            if (zeroFillStreak >= 2)
            {
                buffer[(frame * 2)..].Clear();
                return;
            }

            if (isFinished)
            {
                if (_loopStartFrames is { } loopStart)
                {
                    _cursorFrames = ClampToSourceLength(loopStart);
                    loopedCount++;
                }
                else
                {
                    _state = AudioRenderPipelineState.Stopped;
                    _source = null;
                    _cursorFrames = 0;
                    finishPlaying = () => FinishPlaying?.Invoke();
                }
            }
            else if (filledFrames < remainingFrames)
            {
                // 終端ではないのに要求量を満たせない（非同期デコードの追い越し等）。
                // カーソルは進めず、残りを無音で埋めて次のバッファで追いつくのを待つ
                buffer[(frame * 2)..].Clear();
                return;
            }
        }
    }

    /// <summary>
    /// ソースから読み出したセグメントをステレオ化し、ゲイン・フェード・パンを適用して出力バッファへ書き込みます。
    /// フェードアウトが完了した場合は停止状態へ遷移します。
    /// </summary>
    private void WriteFramesLocked(
        Span<float> buffer,
        int startFrame,
        int filledFrames,
        int channels,
        ref Action? stopPlaying
    )
    {
        var sampleRate = _source?.SampleRate ?? DefaultSampleRate;
        var fadeSecondsPerFrame = sampleRate > 0 ? 1f / sampleRate : 0f;

        var theta = (_pan + 1f) * MathF.PI / 4f;
        var panL = MathF.Cos(theta);
        var panR = MathF.Sin(theta);

        for (var i = 0; i < filledFrames; i++)
        {
            float l,
                r;
            if (channels == 1)
            {
                l = r = _sourceBuffer[i];
            }
            else
            {
                l = _sourceBuffer[i * channels];
                r = _sourceBuffer[(i * channels) + 1];
            }

            var gainScale = 1f;
            if (_state == AudioRenderPipelineState.FadingOut)
            {
                gainScale = CurrentFadeScale();
                _fadeElapsedSeconds += fadeSecondsPerFrame;
            }

            var effectiveGain = _gain * gainScale;
            var frame = startFrame + i;
            buffer[frame * 2] = l * effectiveGain * panL;
            buffer[(frame * 2) + 1] = r * effectiveGain * panR;

            _cursorFrames++;

            if (_state == AudioRenderPipelineState.FadingOut && gainScale <= 0f)
            {
                _state = AudioRenderPipelineState.Stopped;
                _source = null;
                _cursorFrames = 0;
                _fadeSeconds = 0;
                _fadeElapsedSeconds = 0;
                stopPlaying = () => StopPlaying?.Invoke();

                // 以降のフレームは無音のまま
                if (frame + 1 < buffer.Length / 2)
                    buffer[((frame + 1) * 2)..].Clear();
                return;
            }
        }
    }

    private struct PendingCommand
    {
        public PendingCommandKind Kind;
        public IAudioSource? Source;
        public long? LoopStartFrames;
        public float FadeSeconds;
        public long SeekFrames;
    }
}
