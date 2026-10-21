using System;
using System.Diagnostics;
using System.Threading;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.Headless;

namespace Promete.Backends.Headless;

/// <summary>
/// グラフィックスを描画しないヘッドレスのバックエンド。
/// </summary>
/// <remarks>
/// 更新は <see cref="PrometeApp.Run()"/> を呼んだスレッドで行います。
/// そのため、<c>OnUpdate</c> からのシーン遷移や <c>AssetScope.Scene</c> の指定は、Run を呼んだスレッドで動きます。
/// </remarks>
public class HeadlessBackend : BackendBase
{
    private HeadlessTimeProvider _time = null!;
    private HeadlessGameView _gameView = null!;
    private HeadlessRenderTextureProvider _renderTextureProvider = null!;
    private HeadlessScreenBlitter _screenBlitter = null!;
    private volatile bool _isExitRequested;

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
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
            IsVsyncMode = opts.IsVsyncMode,
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

    /// <summary>
    /// 終了が要求されるまで、<see cref="PrometeApp.OnUpdate"/> を <c>TargetUps</c> の間隔で呼び続けます。
    /// 更新は呼び出したスレッドで同期的に行われ、更新の例外はそのまま呼び出し元へ伝わります。
    /// </summary>
    public override void OnStart(PrometeApp app)
    {
        if (_time.TargetUps <= 0)
            throw new ArgumentOutOfRangeException(
                "TargetUps",
                "TargetUps は 1 以上を指定してください。"
            );

        var delta = 1.0 / _time.TargetUps;
        var interval = TimeSpan.FromSeconds(delta);
        app.OnStart();

        var stopwatch = Stopwatch.StartNew();
        var nextUpdate = TimeSpan.Zero;
        while (!_isExitRequested)
        {
            var wait = nextUpdate - stopwatch.Elapsed;
            if (wait > TimeSpan.Zero)
                Thread.Sleep(wait);
            if (_isExitRequested)
                break;

            _time.Tick(delta);
            app.OnUpdate();

            nextUpdate += interval;
            var now = stopwatch.Elapsed;
            if (nextUpdate < now)
                nextUpdate = now;
        }
    }

    public override void OnExit(PrometeApp app)
    {
        _isExitRequested = true;
    }
}
