using System;
using Promete.GLDesktop;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL;

namespace Promete.Backends.GL;

/// <summary>
/// OpenGL (ES) を用いるバックエンドの基底クラスです。
/// </summary>
/// <remarks>
/// テクスチャ、RenderTexture、シェーダー、画面への転送といった GL に共通する部品を提供します。
/// 派生クラスは、GL のコンテキスト、ウィンドウ (<see cref="IGLGameView"/>)、入力、時間、ゲームループを実装します。
/// <list type="number">
/// <item><see cref="BackendBase.OnInitialize"/> で <see cref="InitializeGL"/> を呼び、<see cref="IGLGameView"/> を作って <see cref="InitializeGLView"/> を呼びます。</item>
/// <item>GL のコンテキストができたら、<see cref="OnGLContextCreated"/> を呼びます。</item>
/// <item>フレームごとに <see cref="PrometeApp.OnUpdate"/> を呼び、描画では <see cref="RenderFrame"/> を呼びます。</item>
/// </list>
/// ランナーの登録には <see cref="GLBackendAppExtension.BuildWithGLBackend{TBackend}"/> を使います。
/// </remarks>
public abstract class GLBackendBase : BackendBase
{
    private GLTextureFactory _textureFactory = null!;
    private GLRenderTextureProvider _renderTextureProvider = null!;
    private GLShaderFactory _shaderFactory = null!;
    private GLScreenBlitter _screenBlitter = null!;
    private Silk.NET.OpenGL.GL? _gl;

    public override TextureFactoryBase SetupTextureFactory() => _textureFactory;

    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;

    public override IShaderFactory SetupShaderFactory() => _shaderFactory;

    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;

    /// <summary>
    /// テクスチャ、RenderTexture、シェーダーの部品を生成します。<see cref="BackendBase.OnInitialize"/> の中で呼び出してください。
    /// </summary>
    /// <remarks>
    /// この時点では、GL のコンテキストが無くてもかまいません。
    /// 生成したテクスチャファクトリーは、<see cref="SetupTextureFactory"/> で取得できます。
    /// </remarks>
    /// <param name="app">アプリケーション。</param>
    protected void InitializeGL(PrometeApp app)
    {
        _textureFactory = new GLTextureFactory(app);
        _renderTextureProvider = new GLRenderTextureProvider(app);
        _shaderFactory = new GLShaderFactory();
    }

    /// <summary>
    /// 描画先のビューに画面を転送する部品を生成します。<see cref="InitializeGL"/> の後、<see cref="BackendBase.OnInitialize"/> の中で呼び出してください。
    /// </summary>
    /// <param name="view">描画先のビュー。</param>
    protected void InitializeGLView(IGLGameView view)
    {
        _screenBlitter = new GLScreenBlitter(view, _renderTextureProvider);
    }

    /// <summary>
    /// GL のコンテキストを部品に設定し、画面のキャプチャ先を準備します。
    /// GL のコンテキストができた後、<see cref="PrometeApp.OnStart"/> より前に 1 度だけ呼び出してください。
    /// </summary>
    /// <param name="gl">GL のコンテキスト。</param>
    protected void OnGLContextCreated(Silk.NET.OpenGL.GL gl)
    {
        _gl = gl;
        _textureFactory.GL = gl;
        _renderTextureProvider.GL = gl;
        _shaderFactory.GL = gl;
        _screenBlitter.InitializeScreenRenderTexture();
    }

    /// <summary>
    /// 画面を背景色でクリアし、フレームをレンダリングします。
    /// </summary>
    /// <param name="app">アプリケーション。</param>
    /// <exception cref="InvalidOperationException"><see cref="OnGLContextCreated"/> が呼び出されていない。</exception>
    protected void RenderFrame(PrometeApp app)
    {
        var gl =
            _gl
            ?? throw new InvalidOperationException(
                "GL のコンテキストが設定されていません。OnGLContextCreated を先に呼び出してください。"
            );
        gl.ClearColor(app.BackgroundColor);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        app.OnRender();
    }
}
