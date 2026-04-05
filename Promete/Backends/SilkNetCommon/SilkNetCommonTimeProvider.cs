namespace Promete.Backends.SilkNetCommon;

public class SilkNetCommonTimeProvider : ITimeProvider
{
    private readonly Silk.NET.Windowing.IWindow _window;

    public SilkNetCommonTimeProvider(Silk.NET.Windowing.IWindow window)
    {
        _window = window;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
    }

    public float TotalTime { get; private set; }
    public float TotalTimeWithoutScale { get; private set; }
    public float DeltaTime { get; private set; }
    public long FramePerSeconds { get; private set; }
    public long UpdatePerSeconds { get; private set; }
    public long TotalFrame { get; private set; }

    public int TargetFps
    {
        get => (int)_window.FramesPerSecond;
        set => _window.FramesPerSecond = value;
    }

    public int TargetUps
    {
        get => (int)_window.UpdatesPerSecond;
        set => _window.UpdatesPerSecond = value;
    }

    public float TimeScale { get; set; } = 1;

    private void OnRender(double delta)
    {
        // 画面の初期化
        TotalTime += (float)delta * TimeScale;
        TotalTimeWithoutScale += (float)delta;
        TotalFrame++;
        FramePerSeconds = (int)(1 / delta);
    }

    private void OnUpdate(double delta)
    {
        var deltaTime = (float)delta;
        DeltaTime = deltaTime * TimeScale;

        UpdatePerSeconds = (int)(1 / delta);
    }
}
