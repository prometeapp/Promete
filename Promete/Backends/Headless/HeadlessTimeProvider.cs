namespace Promete.Backends.Headless;

public class HeadlessTimeProvider : ITimeProvider
{
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
        FramePerSeconds = delta > 0 ? (long)(1.0 / delta) : TargetFps;
        UpdatePerSeconds = FramePerSeconds;
    }
}
