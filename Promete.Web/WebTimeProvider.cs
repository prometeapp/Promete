using Promete.Backends;

namespace Promete.Web;

/// <summary>
/// ブラウザの requestAnimationFrame を基準にした <see cref="ITimeProvider"/> です。
/// </summary>
internal sealed class WebTimeProvider : ITimeProvider
{
    private const float CountingInterval = 1f;

    private long _frameCount;
    private float _elapsedTime;

    public float TotalTime { get; private set; }

    public float TotalTimeWithoutScale { get; private set; }

    public float DeltaTime { get; private set; }

    public long FramePerSeconds { get; private set; }

    public long UpdatePerSeconds { get; private set; }

    public long TotalFrame { get; private set; }

    public int TargetFps { get; set; } = 60;

    public int TargetUps { get; set; } = 60;

    // requestAnimationFrame はディスプレイのリフレッシュに同期するため、常に有効で変更できない
    public bool IsVsyncMode
    {
        get => true;
        set { }
    }

    public float TimeScale { get; set; } = 1f;

    internal void Tick(double delta)
    {
        TotalTime += (float)delta * TimeScale;
        TotalTimeWithoutScale += (float)delta;
        DeltaTime = (float)delta * TimeScale;
        TotalFrame++;

        _frameCount++;
        _elapsedTime += (float)delta;
        if (_elapsedTime < CountingInterval)
            return;

        FramePerSeconds = _frameCount;
        UpdatePerSeconds = _frameCount;
        _frameCount = 0;
        _elapsedTime -= CountingInterval;
    }
}
