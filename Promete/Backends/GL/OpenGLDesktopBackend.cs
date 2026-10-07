using System;
using Promete.Backends.SilkNetCommon;
using Promete.GLDesktop;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using Silk.NET.Windowing.Sdl;
using IWindow = Silk.NET.Windowing.IWindow;
using WindowOptions = Promete.Windowing.WindowOptions;

namespace Promete.Backends.GL;

public class OpenGLDesktopBackend : GLBackendBase
{
    private SilkNetCommonTimeProvider _time = null!;
    private IWindow _nativeWindow = null!;
    private PrometeApp _app = null!;
    private OpenGLDesktopGameView _gameView = null!;
    private Silk.NET.OpenGL.GL _gl = null!;

    /// <summary>
    /// Silk.NET のバックエンドを明示的に登録する。
    /// </summary>
    /// <remarks>
    /// Silk.NET は既定では Assembly.Load でバックエンドのアセンブリ名を探索するが、
    /// トリマーはその文字列を追えないため、トリム時にはバックエンドのアセンブリが
    /// まるごと削除され、NativeAOT では探索自体が機能しない。
    /// 登録順はリフレクション探索と同じ GLFW → SDL に揃えてあるので、
    /// どのバックエンドが選ばれるかは従来と変わらない。
    /// </remarks>
    private static void RegisterSilkBackends()
    {
        // いずれも冪等。ShouldLoadFirstPartyPlatforms は二度目の呼び出しで
        // 例外を投げるため使わない。
        GlfwWindowing.RegisterPlatform();
        SdlWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();
        SdlInput.RegisterPlatform();
    }

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
        RegisterSilkBackends();
        _app = app;
        var silkOptions = Silk.NET.Windowing.WindowOptions.Default;
        silkOptions.Position = new Vector2D<int>(opts.Location.X, opts.Location.Y);
        silkOptions.Size = new Vector2D<int>(opts.Size.X, opts.Size.Y) * opts.Scale;
        silkOptions.Title = opts.Title;
        silkOptions.WindowBorder = opts.Mode switch
        {
            WindowMode.Fixed => WindowBorder.Fixed,
            WindowMode.NoFrame => WindowBorder.Hidden,
            WindowMode.Resizable => WindowBorder.Resizable,
            _ => throw new ArgumentException(null, nameof(opts)),
        };
        silkOptions.WindowState = opts.IsFullScreen ? WindowState.Fullscreen : WindowState.Normal;
        silkOptions.FramesPerSecond = opts.TargetFps;
        silkOptions.UpdatesPerSecond = opts.TargetUps;
        silkOptions.VSync = opts.IsVsyncMode;

        _nativeWindow = Window.Create(silkOptions);

        _nativeWindow.Load += OnLoad;
        _nativeWindow.Render += OnRenderFrame;
        _nativeWindow.Update += _ => _app.OnUpdate();
        _nativeWindow.Closing += () =>
        {
            app.OnDestroy();
            _gl.Dispose();
        };

        InitializeGL(_app);
        _time = new SilkNetCommonTimeProvider(_nativeWindow);
        _gameView = new OpenGLDesktopGameView(_app, _nativeWindow, SetupTextureFactory());
        InitializeGLView(_gameView);
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _gameView;

    public override InputProvider SetupInputProvider() => new(_nativeWindow);

    public override void OnStart(PrometeApp app)
    {
        _nativeWindow.Run();
    }

    public override void OnExit(PrometeApp app)
    {
        _nativeWindow.Close();
    }

    private void OnLoad()
    {
        _gl = _nativeWindow.CreateOpenGL();
        _gameView.GL = _gl;
        OnGLContextCreated(_gl);
    }

    private void OnRenderFrame(double delta)
    {
        RenderFrame(_app);
    }
}
