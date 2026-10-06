namespace Promete.Backends.Headless;

public class HeadlessTimeProvider : ITimeProvider
{
    /// <summary>
    /// FPS / UPS を集計する間隔（秒）。
    /// </summary>
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
    public float TimeScale { get; set; } = 1f;

    internal void Tick(double delta)
    {
        TotalTime += (float)delta * TimeScale;
        TotalTimeWithoutScale += (float)delta;
        DeltaTime = (float)delta * TimeScale;
        TotalFrame++;

        // 1秒間に処理されたフレーム数を数え、区切りごとに FPS / UPS として反映する
        _frameCount++;
        _elapsedTime += (float)delta;
        if (_elapsedTime >= CountingInterval)
        {
            FramePerSeconds = _frameCount;
            UpdatePerSeconds = _frameCount;
            _frameCount = 0;
            _elapsedTime -= CountingInterval;
        }
    }
}
