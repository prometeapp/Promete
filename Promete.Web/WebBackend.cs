using Promete.Audio;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics.Fonts;
using Promete.Web.Audio;
using Promete.Web.Fonts;
using Promete.Web.Generated;
using Promete.Web.Input;
using Promete.Windowing;
using Silk.NET.OpenGL;

namespace Promete.Web;

/// <summary>
/// ブラウザ (WebGL2) 上で Promete を動かすバックエンドです。
/// </summary>
/// <remarks>
/// 描画ループは JavaScript (<c>requestAnimationFrame</c>) が持ちます。<see cref="OnStart"/> はループを開始してすぐに戻り、
/// 以降は毎フレーム <see cref="Frame"/> が呼ばれます。
/// </remarks>
internal sealed class WebBackend : GLBackendBase
{
    private PrometeApp _app = null!;
    private GL _gl = null!;
    private WebTimeProvider _time = null!;
    private WebGameView _view = null!;
    private double _lastFrameMs = -1;
    private bool _isExitRequested;

    /// <summary>
    /// <see cref="WebAppExtension.BuildWithWeb"/> から渡された、次に初期化するバックエンドの設定です。
    /// </summary>
    internal static WebOptions PendingOptions { get; set; } = WebOptions.Default;

    internal static WebBackend? Current { get; private set; }

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
        _app = app;

        var selector = PendingOptions.CanvasSelector;
        var context = WebGlInterop.CreateContext(selector);
        if (context <= 0)
            throw new InvalidOperationException(
                $"canvas \"{selector}\" に WebGL2 のコンテキストを作成できませんでした: {context}"
            );

        InputInterop.Attach(selector);
        CalliSignatures.Register();
        _gl = GL.GetApi(new WebGlInterop.NativeContext());

        _time = new WebTimeProvider { TargetFps = opts.TargetFps, TargetUps = opts.TargetUps };
        InitializeGL(app);
        _view = new WebGameView(_gl, opts);
        InitializeGLView(_view);

        Current = this;
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _view;

    public override InputProvider SetupInputProvider() => new WebInputProvider();

    public override IFontProvider SetupFontProvider() => new CanvasFontProvider();

    public override IAudioProvider SetupAudioProvider() => new WebAudioProvider();

    public override void OnStart(PrometeApp app)
    {
        _gl.Viewport(0, 0, (uint)_view.FramebufferSize.X, (uint)_view.FramebufferSize.Y);
        OnGLContextCreated(_gl);
        app.OnStart();
        PrometeWeb.StartLoop();
    }

    public override void OnExit(PrometeApp app)
    {
        _isExitRequested = true;
    }

    /// <summary>
    /// 1 フレーム分の、入力の取り込み、更新、描画を行います。
    /// </summary>
    /// <param name="timeMs">ブラウザが渡すタイムスタンプ (ミリ秒)。</param>
    /// <returns>ゲームを続ける場合は <see langword="true"/>。</returns>
    internal bool Frame(double timeMs)
    {
        if (_isExitRequested)
        {
            _app.OnDestroy();
            Current = null;
            return false;
        }

        var delta = _lastFrameMs < 0 ? 0 : (timeMs - _lastFrameMs) / 1000.0;
        _lastFrameMs = timeMs;
        _time.Tick(delta);

        WebAudioOutput.PumpAll();
        WebInputContext.Instance.Apply(InputInterop.Drain());

        _app.OnUpdate();
        RenderFrame(_app);
        return true;
    }
}
