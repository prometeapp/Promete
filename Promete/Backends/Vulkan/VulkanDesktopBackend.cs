using System;
using Promete.Backends.Headless;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Graphics.Rendering.Vulkan;
using Promete.Windowing;
using Promete.Windowing.Headless;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using IWindow = Silk.NET.Windowing.IWindow;
using WindowOptions = Promete.Windowing.WindowOptions;

namespace Promete.Backends.Vulkan;

/// <summary>
/// Vulkan を使用するデスクトップバックエンドです。
/// </summary>
/// <remarks>
/// 現在は Phase 1（骨格）段階の実装です。ウィンドウ表示・クリアカラー描画・リサイズ対応のみを行い、
/// テクスチャ・シェーダー・RenderTexture・画面ブリットは暫定的に Headless 実装を流用しています。
/// 描画コマンドのランナーは未登録のため、ノードは描画されません。
/// 詳細は VULKAN_PORTING_PLAN.md を参照してください。
/// </remarks>
public class VulkanDesktopBackend : BackendBase
{
    private SilkNetCommonTimeProvider _time = null!;
    private IWindow _nativeWindow = null!;
    private PrometeApp _app = null!;
    private VulkanDesktopGameView _gameView = null!;
    private VulkanContext? _context;
    private HeadlessRenderTextureProvider _renderTextureProvider = null!;
    private HeadlessScreenBlitter _screenBlitter = null!;

    public override void OnInitialize(PrometeApp app, WindowOptions opts)
    {
        _app = app;
        var silkOptions = Silk.NET.Windowing.WindowOptions.DefaultVulkan;
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
            _context?.Dispose();
        };

        _time = new SilkNetCommonTimeProvider(_nativeWindow);
        _gameView = new VulkanDesktopGameView(_app, _nativeWindow);
        _renderTextureProvider = new HeadlessRenderTextureProvider();
        _screenBlitter = new HeadlessScreenBlitter(_renderTextureProvider, _gameView);
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _gameView;

    public override InputProvider SetupInputProvider() => new(_nativeWindow);

    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;

    // TODO: Phase 2 で Vulkan 実装に置き換える
    public override TextureFactoryBase SetupTextureFactory() => new HeadlessTextureFactory();

    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;

    // TODO: Phase 2 で shaderc による Vulkan 実装に置き換える
    public override IShaderFactory SetupShaderFactory() => new HeadlessShaderFactory();

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
        _context = new VulkanContext(_nativeWindow);
        _context.Initialize(_nativeWindow.Title);
    }

    private void OnRenderFrame(double delta)
    {
        // ノード走査とコマンドキュー処理（ランナー未登録のため現状は収集のみ）
        _app.OnRender();

        // TODO: Phase 3 でコマンドキューの実行結果を統合する。現状はクリアカラーのみ描画する
        _context?.DrawFrame(_app.BackgroundColor);
    }
}
