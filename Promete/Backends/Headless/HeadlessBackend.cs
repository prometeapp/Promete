using System.Threading;
using System.Timers;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.Headless;
using Timer = System.Timers.Timer;

namespace Promete.Backends.Headless;

public class HeadlessBackend : BackendBase
{
    private HeadlessTimeProvider _time = null!;
    private HeadlessGameView _gameView = null!;
    private HeadlessRenderTextureProvider _renderTextureProvider = null!;
    private HeadlessScreenBlitter _screenBlitter = null!;
    private PrometeApp _app = null!;
    private bool _isExitRequested;
    private Timer? _timer;

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
        _app = app;
        _gameView = new HeadlessGameView
        {
            Location = opts.Location,
            Size = opts.Size,
            Title = opts.Title,
            Scale = opts.Scale,
            IsFullScreen = opts.IsFullScreen,
            Mode = opts.Mode,
        };
        _time = new HeadlessTimeProvider
        {
            TargetFps = opts.TargetFps,
            TargetUps = opts.TargetUps,
        };
        _renderTextureProvider = new HeadlessRenderTextureProvider();
        _screenBlitter = new HeadlessScreenBlitter(_renderTextureProvider, _gameView);
    }

    public override ITimeProvider SetupTimeProvider() => _time;
    public override IGameView SetupGameView() => _gameView;
    public override InputProvider SetupInputProvider() => new HeadlessInputProvider();
    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;
    public override TextureFactoryBase SetupTextureFactory() => new HeadlessTextureFactory();
    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;
    public override IShaderFactory SetupShaderFactory() => new HeadlessShaderFactory();

    public override void OnStart(PrometeApp app)
    {
        var interval = 1000.0 / _time.TargetUps;
        _timer = new Timer(interval);
        _timer.Elapsed += OnTimerElapsed;
        app.OnStart();
        _timer.Start();
        while (!_isExitRequested) Thread.Sleep(1000);
        _timer.Stop();
    }

    public override void OnExit(PrometeApp app)
    {
        _isExitRequested = true;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var delta = 1.0 / _time.TargetUps;
        _time.Tick(delta);
        _app.OnUpdate();
        if (_isExitRequested) _timer?.Stop();
    }
}
