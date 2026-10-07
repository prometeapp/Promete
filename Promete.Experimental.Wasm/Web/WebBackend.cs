using Generated;
using Promete.Audio;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Silk.NET.OpenGL;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// ブラウザ (WebGL2) 上で Promete を動かすバックエンドです。
/// </summary>
/// <remarks>
/// ブラウザの描画ループは JavaScript (requestAnimationFrame) が持つので、<see cref="OnStart"/> は
/// すぐに戻り、毎フレーム <see cref="Frame"/> が呼ばれます。
/// </remarks>
public sealed class WebBackend : GLBackendBase
{
    private PrometeApp _app = null!;
    private GL _gl = null!;
    private WebTimeProvider _time = null!;
    private WebGameView _view = null!;
    private double _lastFrameMs = -1;
    private bool _isExitRequested;

    /// <summary>描画先の canvas を指す CSS セレクタを取得または設定します。</summary>
    public static string CanvasSelector { get; set; } = "#canvas";

    internal static WebBackend? Current { get; private set; }

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
        _app = app;

        var context = WebGlInterop.CreateContext(CanvasSelector);
        if (context <= 0)
            throw new InvalidOperationException(
                $"WebGL2 コンテキストを作成できませんでした: {context}"
            );

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
    }

    public override void OnExit(PrometeApp app)
    {
        _isExitRequested = true;
    }

    /// <summary>requestAnimationFrame から毎フレーム呼ばれ、更新と描画を 1 回ずつ行います。</summary>
    /// <param name="timeMs">ブラウザが渡すタイムスタンプ (ミリ秒)。</param>
    internal void Frame(double timeMs)
    {
        if (_isExitRequested)
            return;

        var delta = _lastFrameMs < 0 ? 0 : (timeMs - _lastFrameMs) / 1000.0;
        _lastFrameMs = timeMs;
        _time.Tick(delta);

        WebAudioOutput.PumpAll();

        _app.OnUpdate();
        WebInputContext.Instance.EndFrame();
        RenderFrame(_app);
    }
}
