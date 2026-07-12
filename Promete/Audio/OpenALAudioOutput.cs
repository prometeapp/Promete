using System;
using System.Threading;
using Promete.Audio.Internal;
using Silk.NET.OpenAL;

namespace Promete.Audio;

/// <summary>
/// OpenAL の実デバイスへ PCM を出力する <see cref="IAudioOutput"/> 実装です。
/// 専用スレッド上でトリプルバッファリングを行い、レンダーコールバックが生成した PCM を再生し続けます。
/// </summary>
public sealed class OpenALAudioOutput : IAudioOutput
{
    /// <summary>
    /// AL_EXT_FLOAT32 拡張が定義する AL_FORMAT_STEREO_FLOAT32 のフォーマット定数です。
    /// Silk.NET.OpenAL には対応する enum メンバーが存在しないため、値を直接指定します。
    /// </summary>
    private const int AlFormatStereoFloat32 = 0x10011;

    private const int MinBufferCount = 2;

    private readonly AudioDevice _device;
    private readonly AL _al;
    private readonly object _threadGate = new();
    private readonly AutoResetEvent _wakeEvent = new(false);

    private Thread? _thread;
    private volatile bool _stopRequested;
    private volatile bool _flushRequested;
    private volatile uint _sourceHandle;
    private AudioRenderCallback? _render;
    private Func<int>? _getSampleRate;
    private int _channels;
    private int _bufferSizeInFrames;
    private int _bufferCount = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenALAudioOutput"/> class.
    /// この <see cref="OpenALAudioOutput" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="device">出力先の ALC デバイス・コンテキストを保持する <see cref="AudioDevice"/>。</param>
    public OpenALAudioOutput(AudioDevice device)
    {
        _device = device;
        _al = device.Al;
    }

    /// <summary>
    /// このプレイヤーのピッチを取得または設定します。値は次回のバッファ充填時に AL ソースへ反映されます。
    /// </summary>
    public float Pitch { get; set; } = 1;

    /// <summary>
    /// キュー済みでまだ再生されていないフレーム数を取得します。
    /// AL ソースのキュー済みバッファ数と再生オフセットから算出します。出力停止中は 0 を返します。
    /// </summary>
    public long PendingFrames
    {
        get
        {
            var handle = _sourceHandle;
            if (handle == 0)
                return 0;

            _al.GetSourceProperty(handle, GetSourceInteger.BuffersQueued, out var queuedCount);
            _al.GetSourceProperty(handle, GetSourceInteger.SampleOffset, out var sampleOffset);
            return Math.Max(0, ((long)queuedCount * _bufferSizeInFrames) - sampleOffset);
        }
    }

    /// <summary>
    /// 出力を開始します。専用の背景スレッドを起動し、マルチバッファリングで再生を継続します。
    /// </summary>
    /// <param name="render">1バッファ分の interleaved float PCM を生成するコールバック。</param>
    /// <param name="channels">出力するチャンネル数。</param>
    /// <param name="sampleRate">出力を開始する時点のサンプリング周波数。</param>
    /// <param name="bufferSizeInFrames">1回のコールバックで生成するフレーム数。</param>
    /// <param name="getSampleRate">現在要求されているサンプリング周波数を取得するコールバック。</param>
    /// <param name="bufferCount">先行キューするバッファ数。最小 2 にクランプされます。</param>
    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames,
        Func<int>? getSampleRate = null,
        int bufferCount = 3
    )
    {
        lock (_threadGate)
        {
            StopThread();

            _render = render;
            _channels = channels;
            _bufferSizeInFrames = bufferSizeInFrames;
            _bufferCount = Math.Max(MinBufferCount, bufferCount);
            _getSampleRate = getSampleRate;
            _stopRequested = false;
            _flushRequested = false;

            _thread = new Thread(() => RunLoop(sampleRate))
            {
                IsBackground = true,
                Name = "Promete OpenAL Audio Output",
            };
            _thread.Start();
        }
    }

    /// <summary>
    /// 出力を停止します。背景スレッドの終了を待ってから戻ります。
    /// </summary>
    public void Stop()
    {
        lock (_threadGate)
        {
            StopThread();
        }
    }

    /// <summary>
    /// キュー済みの未再生バッファを破棄し、レンダースレッドを即時起床させて再充填・再生します。
    /// 再生開始コマンド直後に呼ぶことで、先行キューされた無音の排出待ちを解消します。
    /// </summary>
    public void Flush()
    {
        _flushRequested = true;
        _wakeEvent.Set();
    }

    /// <summary>
    /// リソースを解放します。<see cref="Stop"/> と同様に、背景スレッドの終了を待機してから AL リソースを破棄します。
    /// </summary>
    public void Dispose()
    {
        Stop();
        _wakeEvent.Dispose();
    }

    private static ALBuffer FindBuffer(ALBuffer[] buffers, uint handle)
    {
        foreach (var buf in buffers)
        {
            if (buf.Handle == handle)
                return buf;
        }

        throw new InvalidOperationException(
            "Unqueued buffer handle was not found among the tracked buffers."
        );
    }

    private void StopThread()
    {
        if (_thread is null)
            return;

        _stopRequested = true;
        _wakeEvent.Set();
        _thread.Join();
        _thread = null;
        _render = null;
        _getSampleRate = null;
    }

    private void RunLoop(int initialSampleRate)
    {
        using var alSource = new ALSource(_al);
        var buffers = new ALBuffer[_bufferCount];
        for (var i = 0; i < _bufferCount; i++)
            buffers[i] = new ALBuffer(_al);

        try
        {
            var sampleRate = initialSampleRate;
            var floatBuffer = new float[_bufferSizeInFrames * _channels];
            var pcmBuffer = _device.SupportsFloat32
                ? null
                : new short[_bufferSizeInFrames * _channels];

            foreach (var buf in buffers)
                FillAndBuffer(alSource, buf, sampleRate, floatBuffer, pcmBuffer);

            QueueAll(alSource, buffers);
            _al.SourcePlay(alSource.Handle);
            _sourceHandle = alSource.Handle;

            var waitMs = CalculateWaitMilliseconds(sampleRate);

            while (!_stopRequested)
            {
                if (_flushRequested)
                {
                    _flushRequested = false;

                    _al.SourceStop(alSource.Handle);
                    UnqueueAll(alSource);

                    foreach (var buf in buffers)
                        FillAndBuffer(alSource, buf, sampleRate, floatBuffer, pcmBuffer);

                    QueueAll(alSource, buffers);
                    _al.SourcePlay(alSource.Handle);
                    continue;
                }

                var currentSampleRate = _getSampleRate?.Invoke() ?? sampleRate;
                if (currentSampleRate != sampleRate && currentSampleRate > 0)
                {
                    sampleRate = currentSampleRate;
                    waitMs = CalculateWaitMilliseconds(sampleRate);

                    _al.SourceStop(alSource.Handle);
                    UnqueueAll(alSource);

                    foreach (var buf in buffers)
                        FillAndBuffer(alSource, buf, sampleRate, floatBuffer, pcmBuffer);

                    QueueAll(alSource, buffers);
                    _al.SourcePlay(alSource.Handle);
                    continue;
                }

                _al.GetSourceProperty(
                    alSource.Handle,
                    GetSourceInteger.BuffersProcessed,
                    out var processedCount
                );

                for (var i = 0; i < processedCount; i++)
                {
                    var processedHandle = UnqueueOne(alSource);
                    var processedBuffer = FindBuffer(buffers, processedHandle);
                    FillAndBuffer(alSource, processedBuffer, sampleRate, floatBuffer, pcmBuffer);
                    QueueOne(alSource, processedBuffer);
                }

                _al.GetSourceProperty(alSource.Handle, GetSourceInteger.SourceState, out var state);
                if (state != (int)SourceState.Playing)
                    _al.SourcePlay(alSource.Handle);

                _wakeEvent.WaitOne(waitMs);
            }

            _al.SourceStop(alSource.Handle);
            UnqueueAll(alSource);
        }
        finally
        {
            _sourceHandle = 0;
            foreach (var buf in buffers)
                buf.Dispose();
        }
    }

    private void FillAndBuffer(
        ALSource alSource,
        ALBuffer buffer,
        int sampleRate,
        float[] floatBuffer,
        short[]? pcmBuffer
    )
    {
        _render?.Invoke(floatBuffer);
        _al.SetSourceProperty(alSource.Handle, SourceFloat.Pitch, Pitch);

        if (pcmBuffer is null)
        {
            unsafe
            {
                fixed (float* samplePtr = floatBuffer)
                {
                    _al.BufferData(
                        buffer.Handle,
                        (BufferFormat)AlFormatStereoFloat32,
                        samplePtr,
                        floatBuffer.Length * sizeof(float),
                        sampleRate
                    );
                }
            }
        }
        else
        {
            for (var i = 0; i < floatBuffer.Length; i++)
            {
                var clamped = Math.Clamp(floatBuffer[i], -1f, 1f);
                pcmBuffer[i] = (short)(clamped * short.MaxValue);
            }

            _al.BufferData(buffer.Handle, BufferFormat.Stereo16, pcmBuffer, sampleRate);
        }
    }

    private void QueueAll(ALSource alSource, ALBuffer[] buffers)
    {
        foreach (var buf in buffers)
            QueueOne(alSource, buf);
    }

    private void QueueOne(ALSource alSource, ALBuffer buffer)
    {
        var handles = new[] { buffer.Handle };
        _al.SourceQueueBuffers(alSource.Handle, handles);
    }

    private uint UnqueueOne(ALSource alSource)
    {
        var handles = new uint[1];
        _al.SourceUnqueueBuffers(alSource.Handle, handles);
        return handles[0];
    }

    private void UnqueueAll(ALSource alSource)
    {
        _al.GetSourceProperty(alSource.Handle, GetSourceInteger.BuffersQueued, out var queuedCount);
        var handle = new uint[1];
        for (var i = 0; i < queuedCount; i++)
            _al.SourceUnqueueBuffers(alSource.Handle, handle);
    }

    private int CalculateWaitMilliseconds(int sampleRate)
    {
        if (sampleRate <= 0)
            return 1;
        var bufferSeconds = _bufferSizeInFrames / (float)sampleRate;
        var waitMs = (int)(bufferSeconds * 1000f / 4f);
        return Math.Max(1, waitMs);
    }
}
