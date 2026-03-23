using System;
using Promete.Backends.SilkNetCommon;
using Promete.GLDesktop;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using IWindow = Silk.NET.Windowing.IWindow;
using WindowOptions = Promete.Windowing.WindowOptions;

namespace Promete.Backends.GL;

public class OpenGLDesktopBackend : BackendBase
{
    private SilkNetCommonTimeProvider _time = null!;
    private IWindow _nativeWindow = null!;
    private PrometeApp _app = null!;
    private OpenGLDesktopGameView _gameView = null!;
    private Silk.NET.OpenGL.GL _gl = null!;
    private GLTextureFactory _textureFactory = null!;
    private GLRenderTextureProvider _renderTextureProvider = null!;
    private GLShaderFactory _shaderFactory = null!;
    private GLScreenBlitter _screenBlitter = null!;

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
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
            _ => throw new ArgumentException(null, nameof(opts))
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

        _textureFactory = new GLTextureFactory(_app);
        _time = new SilkNetCommonTimeProvider(_nativeWindow);
        _renderTextureProvider = new GLRenderTextureProvider(_app);
        _shaderFactory = new GLShaderFactory();
        _gameView = new OpenGLDesktopGameView(_app, _nativeWindow, _textureFactory);
        _screenBlitter = new GLScreenBlitter(_gameView, _renderTextureProvider);
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _gameView;

    public override InputProvider SetupInputProvider() => new(_nativeWindow);

    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;

    public override TextureFactoryBase SetupTextureFactory() => _textureFactory;

    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;

    public override IShaderFactory SetupShaderFactory() => _shaderFactory;

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
        _textureFactory.GL = _gl;
        _renderTextureProvider.GL = _gl;
        _shaderFactory.GL = _gl;
        _screenBlitter.InitializeScreenRenderTexture();
    }

    private void OnRenderFrame(double delta)
    {
        // 画面の初期化
        _gl.ClearColor(_app.BackgroundColor);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _app.OnRender();
    }
}
