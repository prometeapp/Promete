namespace Promete.Backends.SilkNetCommon;

public class SilkNetCommonTimeProvider : ITimeProvider
{
    /// <summary>
    /// FPS / UPS を集計する間隔（秒）。
    /// </summary>
    private const float CountingInterval = 1f;

    private readonly Silk.NET.Windowing.IWindow _window;

    private long _renderFrameCount;
    private float _renderElapsedTime;
    private long _updateFrameCount;
    private float _updateElapsedTime;

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

        // 1秒間に描画されたフレーム数を数え、区切りごとに FPS として反映する
        _renderFrameCount++;
        _renderElapsedTime += (float)delta;
        if (_renderElapsedTime >= CountingInterval)
        {
            FramePerSeconds = _renderFrameCount;
            _renderFrameCount = 0;
            _renderElapsedTime -= CountingInterval;
        }
    }

    private void OnUpdate(double delta)
    {
        var deltaTime = (float)delta;
        DeltaTime = deltaTime * TimeScale;

        // 1秒間に更新された回数を数え、区切りごとに UPS として反映する
        _updateFrameCount++;
        _updateElapsedTime += deltaTime;
        if (_updateElapsedTime >= CountingInterval)
        {
            UpdatePerSeconds = _updateFrameCount;
            _updateFrameCount = 0;
            _updateElapsedTime -= CountingInterval;
        }
    }
}
