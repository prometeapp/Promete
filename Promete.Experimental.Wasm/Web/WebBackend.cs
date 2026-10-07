using Generated;
using Promete.Backends;
using Promete.Backends.SilkNetCommon;
using Promete.GLDesktop;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// ブラウザ (WebGL2) 上で Promete を動かすバックエンドです。
/// </summary>
/// <remarks>
/// ブラウザの描画ループは JavaScript (requestAnimationFrame) が持つので、<see cref="OnStart"/> は
/// すぐに戻り、毎フレーム <see cref="Frame"/> が呼ばれます。
/// </remarks>
public sealed class WebBackend : BackendBase
{
    private PrometeApp _app = null!;
    private GL _gl = null!;
    private WebTimeProvider _time = null!;
    private WebGameView _view = null!;
    private GLTextureFactory _textureFactory = null!;
    private GLRenderTextureProvider _renderTextureProvider = null!;
    private GLShaderFactory _shaderFactory = null!;
    private GLScreenBlitter _screenBlitter = null!;
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
        ConfigureFonts();
        _gl = GL.GetApi(new WebGlInterop.NativeContext());

        _time = new WebTimeProvider { TargetFps = opts.TargetFps, TargetUps = opts.TargetUps };
        _view = new WebGameView(_gl, opts);
        _textureFactory = new GLTextureFactory(app) { GL = _gl };
        _renderTextureProvider = new GLRenderTextureProvider(app) { GL = _gl };
        _shaderFactory = new GLShaderFactory { GL = _gl };
        _screenBlitter = new GLScreenBlitter(_view, _renderTextureProvider);

        Current = this;
    }

    /// <summary>
    /// FreeType を使えないブラウザでも <see cref="Font"/> が動くよう、グリフの供給元を Canvas2D に差し替えます。
    /// ファイルから作るフォントは、事前に JavaScript が FontFace として登録したパス名のフォントを使います。
    /// </summary>
    private static void ConfigureFonts()
    {
        var defaultSource = new CanvasGlyphSource("sans-serif");
        Font.FileSourceFactory = (path, _) => new CanvasGlyphSource(path);
        Font.DefaultFontFactory = (size, style, isAntialiased) =>
            Font.FromGlyphSource(defaultSource, size, style, isAntialiased);
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _view;

    public override InputProvider SetupInputProvider() => new WebInputProvider();

    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;

    public override TextureFactoryBase SetupTextureFactory() => _textureFactory;

    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;

    public override IShaderFactory SetupShaderFactory() => _shaderFactory;

    public override void OnStart(PrometeApp app)
    {
        _gl.Viewport(0, 0, (uint)_view.FramebufferSize.X, (uint)_view.FramebufferSize.Y);
        _screenBlitter.InitializeScreenRenderTexture();
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

        _gl.ClearColor(_app.BackgroundColor);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _app.OnUpdate();
        WebInputContext.Instance.EndFrame();
        _app.OnRender();
    }
}
