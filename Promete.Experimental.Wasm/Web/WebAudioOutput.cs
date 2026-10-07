using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Promete.Audio;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// Web Audio へ出力する <see cref="IAudioOutput"/> です。
/// </summary>
/// <remarks>
/// ブラウザでは .NET が単一スレッドなので、専用スレッドでの常駐レンダーはできません。
/// 代わりに毎フレーム <see cref="PumpAll"/> を呼び、JavaScript 側のキューが一定量を下回ったら
/// 1 バッファ分を生成して追加します。
/// </remarks>
public sealed partial class WebAudioOutput : IAudioOutput
{
    private const string Module = "webAudio";
    private const double TargetQueueSeconds = 0.2;
    private const int MaxBuffersPerPump = 8;

    private AudioRenderCallback? _render;
    private float[] _buffer = [];
    private int _channels;
    private int _sampleRate;
    private int _bufferFrames;

    public static WebAudioOutput? Current { get; private set; }

    public float Pitch { get; set; } = 1f;

    public long PendingFrames => (long)(QueuedSeconds() * _sampleRate);

    /// <summary>Web Audio 用の JavaScript モジュールを読み込みます。使用前に 1 回呼びます。</summary>
    /// <param name="moduleUrl">webAudio.js の絶対 URL。</param>
    /// <returns>読み込みの完了を表すタスク。</returns>
    public static Task ImportModule(string moduleUrl) => JSHost.ImportAsync(Module, moduleUrl);

    /// <summary>登録済みの出力に、必要なら 1 フレーム分のレンダリングを行わせます。</summary>
    public static void PumpAll() => Current?.Pump();

    /// <summary>AudioContext の状態 (suspended / running など) を返します。</summary>
    /// <returns>状態を表す文字列。</returns>
    public static string GetState() => State();

    /// <summary>AudioContext の経過時間 (秒) を返します。</summary>
    /// <returns>経過時間。</returns>
    public static double GetCurrentTime() => CurrentTime();

    /// <summary>直近の出力の RMS を返します。</summary>
    /// <returns>RMS 値。</returns>
    public static double GetRms() => Rms();

    public void Start(
        AudioRenderCallback render,
        int channels,
        int sampleRate,
        int bufferSizeInFrames,
        Func<int>? getSampleRate = null,
        int bufferCount = 3
    )
    {
        _render = render;
        _channels = channels;
        _sampleRate = sampleRate;
        _bufferFrames = bufferSizeInFrames;
        _buffer = new float[bufferSizeInFrames * channels];

        StartNative(sampleRate, channels);
        Current = this;
    }

    public void Stop()
    {
        if (Current == this)
            Current = null;
        StopNative();
        _render = null;
    }

    public void Flush() => FlushNative();

    public void Dispose() => Stop();

    private void Pump()
    {
        if (_render is null)
            return;

        for (var i = 0; i < MaxBuffersPerPump && QueuedSeconds() < TargetQueueSeconds; i++)
        {
            _render(_buffer);
            Enqueue(MemoryMarshal.AsBytes(_buffer.AsSpan()), _bufferFrames, Pitch);
        }
    }

    [JSImport("start", Module)]
    private static partial void StartNative(int sampleRate, int channels);

    [JSImport("enqueue", Module)]
    private static partial void Enqueue(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> bytes,
        int frames,
        double pitch
    );

    [JSImport("queuedSeconds", Module)]
    private static partial double QueuedSeconds();

    [JSImport("flush", Module)]
    private static partial void FlushNative();

    [JSImport("stop", Module)]
    private static partial void StopNative();

    [JSImport("state", Module)]
    private static partial string State();

    [JSImport("currentTime", Module)]
    private static partial double CurrentTime();

    [JSImport("rms", Module)]
    private static partial double Rms();
}
