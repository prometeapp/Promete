using FluentAssertions;
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Backends.Headless;
using Promete.Backends.SilkNetCommon;
using Promete.GLDesktop;
using Promete.Graphics;
using Promete.Graphics.Rendering.GL;
using Promete.Graphics.Rendering.GL.Runners;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;

namespace Promete.Test;

/// <summary>
/// <see cref="GLBackendBase"/> を派生したバックエンドが、GL の部品とランナーを組み立てられることを確かめるテスト。
/// GL のコンテキストは作らないので、GL を呼ぶ処理は実行しない。
/// </summary>
public class GLBackendBaseTests
{
    [Fact]
    public void BuildWithGLBackend_ShouldProvideGLComponents()
    {
        using var app = PrometeApp.Create().BuildWithGLBackend<FakeGLBackend>();

        app.TextureFactory.Should().BeOfType<GLTextureFactory>();
        app.GetPlugin<IRenderTextureProvider>().Should().BeOfType<GLRenderTextureProvider>();
        app.GetPlugin<IShaderFactory>().Should().BeOfType<GLShaderFactory>();
        app.GetPlugin<IScreenBlitter>().Should().BeOfType<GLScreenBlitter>();
    }

    [Fact]
    public void BuildWithGLBackend_ShouldResolveRunnersWithNonDesktopView()
    {
        using var app = PrometeApp.Create().BuildWithGLBackend<FakeGLBackend>();

        var act = () =>
        {
            app.GetPlugin<GLMaskedContainerHelper>();
            app.GetPlugin<GLDrawTextureBatchedCommandRunner>();
            app.GetPlugin<GLDrawPrimitiveCommandRunner>();
            app.GetPlugin<GLBeginTrimCommandRunner>();
            app.GetPlugin<GLEndTrimCommandRunner>();
            app.GetPlugin<GLBeginStencilMaskCommandRunner>();
            app.GetPlugin<GLBeginAlphaMaskCommandRunner>();
            app.GetPlugin<GLEndMaskCommandRunner>();
            app.GetPlugin<GLDrawPieTextureCommandRunner>();
        };

        act.Should().NotThrow("ランナーはデスクトップ以外の IGLGameView でも生成できるはず");
    }

    /// <summary>
    /// デスクトップ以外の GL バックエンドを模したもの。GL のコンテキストは持たない。
    /// </summary>
    internal sealed class FakeGLBackend : GLBackendBase
    {
        private readonly HeadlessTimeProvider _time = new();
        private FakeGLGameView _view = null!;

        public override void OnInitialize(PrometeApp app, WindowOptions windowOptions)
        {
            InitializeGL(app);
            _view = new FakeGLGameView { Size = windowOptions.Size };
            InitializeGLView(_view);
        }

        public override ITimeProvider SetupTimeProvider() => _time;

        public override IGameView SetupGameView() => _view;

        public override InputProvider SetupInputProvider() => new HeadlessInputProvider();

        public override void OnStart(PrometeApp app) { }

        public override void OnExit(PrometeApp app) { }
    }

    internal sealed class FakeGLGameView : HeadlessGameView, IGLGameView
    {
        public Silk.NET.OpenGL.GL GL => null!;

        public VectorInt FramebufferSize => Size;
    }
}
