using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Promete.Graphics;
using Silk.NET.Input;
using Timer = System.Timers.Timer;

namespace Promete.Windowing.Headless;

public class HeadlessWindow : IWindow
{
    private readonly Timer _timer = new(1000 / 60f);

    private bool _isExitRequested;

    private int _scale = 1;
    public VectorInt Location { get; set; }

    public VectorInt Size { get; set; }

    public int X
    {
        get => Location.X;
        set => Location = (value, Y);
    }

    public int Y
    {
        get => Location.Y;
        set => Location = (X, value);
    }

    public int Width
    {
        get => Size.X;
        set => Size = (value, Height);
    }

    public int Height
    {
        get => Size.Y;
        set => Size = (Width, value);
    }

    public int Scale
    {
        get => _scale;
        set
        {
            if (value is not 1 and not 2 and not 4 and not 8)
                throw new ArgumentOutOfRangeException(nameof(value), "Scale must be 1, 2, 4, or 8.");
            _scale = value;
        }
    }

    public VectorInt ActualSize => Size;

    public int ActualWidth => Width;
    public int ActualHeight => Height;
    public bool IsVisible { get; set; } = true;
    public bool IsFocused => true;
    public bool IsFullScreen { get; set; } = true;
    public float TotalTime { get; private set; }
    public float DeltaTime => 1f / FramePerSeconds;
    public long FramePerSeconds => TargetFps;
    public long UpdatePerSeconds => TargetUps;
    public long TotalFrame { get; private set; }
    public int RefreshRate { get; set; }
    public bool IsVsyncMode { get; set; }
    public int TargetFps { get; set; }
    public int TargetUps { get; set; }
    public float PixelRatio => 1;
    public string Title { get; set; }
    public WindowMode Mode { get; set; }
    public IInputContext? _RawInputContext { get; } = new DummyInputContext();
    public TextureFactory TextureFactory { get; } = new HeadlessTextureFactory();

    public void Run(WindowOptions opts)
    {
        Location = opts.Location;
        Size = opts.Size;
        Title = opts.Title;
        Scale = opts.Scale;
        IsFullScreen = opts.IsFullScreen;
        Mode = opts.Mode;
        TargetFps = opts.TargetFps;
        TargetUps = opts.TargetUps;
        IsVsyncMode = opts.IsVsyncMode;

        _timer.Elapsed += TimerOnElapsed;
        Start?.Invoke();
        _timer.Start();
        while (!_isExitRequested) Thread.Sleep(1000);
    }

    public void Exit()
    {
        _isExitRequested = true;
        Destroy?.Invoke();
    }

    public Texture2D TakeScreenshot()
    {
        // Do nothing
        return default;
    }

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default)
    {
        return Task.Delay(0, ct);
    }

    public event Action? Start;
    public event Action? Update;
    public event Action? Render;
    public event Action? Destroy;
    public event Action? PreUpdate;
    public event Action? PostUpdate;
    public event Action<FileDroppedEventArgs>? FileDropped;
    public event Action? Resize;

    private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        TotalTime += 1f / TargetFps;
        TotalFrame++;
        PreUpdate?.Invoke();
        Update?.Invoke();
        PostUpdate?.Invoke();
        Render?.Invoke();
        if (_isExitRequested) _timer.Stop();
    }
}
