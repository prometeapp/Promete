using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Promete.Windowing.GLDesktop;

/// <summary>
/// A implementation of <see cref="IWindow" /> for the desktop environment.
/// </summary>
public sealed class OpenGLDesktopWindow(PrometeApp app) : IWindow
{
    private int _frameCount;
    private GL? _gl;
    private int _prevSecondFps;
    private int _prevSecondUps;

    private int _scale = 1;
    private byte[] _screenshotBuffer = [];
    private VectorInt _size = (640, 480);
    private TextureFactory? _textureFactory;
    private int _updateCount;
    private float _timeScale = 1f;

    public GL GL => _gl ?? throw new InvalidOperationException("window is not loaded");

    public Silk.NET.Windowing.IWindow NativeWindow { get; private set; }

    public VectorInt Location
    {
        get => (NativeWindow.Position.X, NativeWindow.Position.Y);
        set => NativeWindow.Position = new Vector2D<int>(value.X, value.Y);
    }

    public VectorInt Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            UpdateWindowSize();
        }
    }

    public VectorInt ActualSize =>
        new VectorInt(NativeWindow.FramebufferSize.X, NativeWindow.FramebufferSize.Y) / Scale;

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
            UpdateWindowSize();
        }
    }

    public int ActualWidth => ActualSize.X;

    public int ActualHeight => ActualSize.Y;

    public bool IsVisible
    {
        get => NativeWindow.IsVisible;
        set => NativeWindow.IsVisible = value;
    }

    public bool IsFocused { get; private set; }

    public bool IsFullScreen
    {
        get => NativeWindow.WindowState == WindowState.Fullscreen;
        set => NativeWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
    }

    public bool TopMost
    {
        get => NativeWindow.TopMost;
        set => NativeWindow.TopMost = value;
    }

    public string Title
    {
        get => NativeWindow.Title;
        set
        {
            if (NativeWindow.Title == value) return;
            NativeWindow.Title = value;
        }
    }

    public long TotalFrame { get; private set; }

    public float TotalTime { get; private set; }

    public float TotalTimeWithoutScale { get; private set; }

    public float DeltaTime { get; private set; }

    public long FramePerSeconds { get; private set; }

    public long UpdatePerSeconds { get; private set; }

    public int TargetFps
    {
        get => (int)NativeWindow.FramesPerSecond;
        set => NativeWindow.FramesPerSecond = value;
    }

    public int TargetUps
    {
        get => (int)NativeWindow.UpdatesPerSecond;
        set => NativeWindow.UpdatesPerSecond = value;
    }

    public int RefreshRate
    {
        get => (int)NativeWindow.FramesPerSecond;
        set => NativeWindow.FramesPerSecond = value;
    }

    public float TimeScale
    {
        get => _timeScale;
        set => _timeScale = Math.Max(0, value);
    }

    public bool IsVsyncMode
    {
        get => NativeWindow.VSync;
        set => NativeWindow.VSync = value;
    }

    public float PixelRatio => NativeWindow.Size.X == 0 ? 1 : NativeWindow.FramebufferSize.X / NativeWindow.Size.X;

    public WindowMode Mode
    {
        get => NativeWindow.WindowBorder switch
        {
            WindowBorder.Fixed => WindowMode.Fixed,
            WindowBorder.Hidden => WindowMode.NoFrame,
            WindowBorder.Resizable => WindowMode.Resizable,
            _ => throw new InvalidOperationException("unexpected window state")
        };
        set => NativeWindow.WindowBorder = value switch
        {
            WindowMode.Fixed => WindowBorder.Fixed,
            WindowMode.NoFrame => WindowBorder.Hidden,
            WindowMode.Resizable => WindowBorder.Resizable,
            _ => throw new ArgumentException(null, nameof(value))
        };
    }

    public IInputContext? _RawInputContext { get; private set; }

    public TextureFactory TextureFactory =>
        _textureFactory ?? throw new InvalidOperationException("window is not loaded");

    public Texture2D TakeScreenshot()
    {
        return TextureFactory.LoadFromImageSharpImage(TakeScreenshotAsImage());
    }

    public async Task SaveScreenshotAsync(string path, CancellationToken ct = default)
    {
        var img = TakeScreenshotAsImage();
        await img.SaveAsPngAsync(path, ct);
    }

    public void Run(WindowOptions opts)
    {
        var options = Silk.NET.Windowing.WindowOptions.Default;
        options.Position = new Vector2D<int>(opts.Location.X, opts.Location.Y);
        options.Size = new Vector2D<int>(opts.Size.X, opts.Size.Y) * opts.Scale;
        options.Title = opts.Title;
        options.WindowBorder = opts.Mode switch
        {
            WindowMode.Fixed => WindowBorder.Fixed,
            WindowMode.NoFrame => WindowBorder.Hidden,
            WindowMode.Resizable => WindowBorder.Resizable,
            _ => throw new ArgumentException(null, nameof(opts.Mode))
        };
        options.WindowState = opts.IsFullScreen ? WindowState.Fullscreen : WindowState.Normal;
        options.FramesPerSecond = opts.TargetFps;
        options.UpdatesPerSecond = opts.TargetUps;
        options.VSync = opts.IsVsyncMode;

        _screenshotBuffer = new byte[options.Size.X * options.Size.Y * 4];

        NativeWindow = Window.Create(options);
        NativeWindow.Load += OnLoad;
        NativeWindow.Resize += OnResize;
        NativeWindow.FileDrop += OnFileDrop;
        NativeWindow.Render += OnRenderFrame;
        NativeWindow.Update += OnUpdateFrame;
        NativeWindow.Closing += OnUnload;
        NativeWindow.FocusChanged += v => IsFocused = v;
        NativeWindow.Run();
    }

    public void Exit()
    {
        NativeWindow.Close();
    }

    public event Action? Start;
    public event Action? Update;
    public event Action? Render;
    public event Action? Destroy;
    public event Action<FileDroppedEventArgs>? FileDropped;
    public event Action? Resize;
    public event Action? PreUpdate;
    public event Action? PostUpdate;

    private unsafe Image TakeScreenshotAsImage()
    {
        fixed (byte* buffer = _screenshotBuffer)
        {
            _gl?.ReadPixels(0, 0, (uint)(ActualWidth * _scale), (uint)(ActualHeight * _scale), PixelFormat.Rgba,
                PixelType.UnsignedByte, buffer);
        }

        var img = Image.LoadPixelData<Rgba32>(_screenshotBuffer, ActualWidth * _scale, ActualHeight * _scale);
        img.Mutate(i => i.Flip(FlipMode.Vertical));
        return img;
    }

    private void OnLoad()
    {
        _gl = NativeWindow.CreateOpenGL();
        _RawInputContext = NativeWindow.CreateInput();
        _textureFactory = new OpenGLTextureFactory(_gl, app);
        UpdateWindowSize();

        Start?.Invoke();
    }

    private void OnResize(Vector2D<int> vec)
    {
        _gl?.Viewport(NativeWindow.FramebufferSize);
        Size = (VectorInt)((Vector)ActualSize / PixelRatio);
        Resize?.Invoke();
    }

    private void OnFileDrop(string[] files)
    {
        FileDropped?.Invoke(new FileDroppedEventArgs(files));
    }

    private void OnRenderFrame(double delta)
    {
        if (_gl == null) return;
        // 画面の初期化
        _gl.ClearColor(app.BackgroundColor);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        CalculateFps();
        Render?.Invoke();
        TotalTime += (float)delta * TimeScale;
        TotalTimeWithoutScale += (float)delta;
        TotalFrame++;
    }

    private void OnUpdateFrame(double delta)
    {
        var deltaTime = (float)delta;
        DeltaTime = deltaTime * TimeScale;

        CalculateUps();

        PreUpdate?.Invoke();
        Update?.Invoke();
        PostUpdate?.Invoke();
    }

    private void OnUnload()
    {
        Destroy?.Invoke();
    }

    private void CalculateUps()
    {
        _updateCount++;
        if (Environment.TickCount - _prevSecondUps <= 1000) return;

        UpdatePerSeconds = _updateCount;
        _updateCount = 0;
        _prevSecondUps = Environment.TickCount;
    }

    private void CalculateFps()
    {
        _frameCount++;
        if (Environment.TickCount - _prevSecondFps <= 1000) return;

        FramePerSeconds = _frameCount;
        _frameCount = 0;
        _prevSecondFps = Environment.TickCount;
    }

    private void UpdateWindowSize()
    {
        NativeWindow.Size = new Vector2D<int>(Size.X, Size.Y) * _scale;
        _screenshotBuffer = new byte[NativeWindow.FramebufferSize.X * NativeWindow.FramebufferSize.Y * _scale * 4];
    }
}
