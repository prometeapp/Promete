using System;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Vulkan;
using Promete.Graphics.Rendering.Vulkan.Runners;
using Promete.Windowing;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using IWindow = Silk.NET.Windowing.IWindow;
using WindowOptions = Promete.Windowing.WindowOptions;

namespace Promete.Backends.Vulkan;

/// <summary>
/// Vulkan を使用するデスクトップバックエンドです。
/// </summary>
/// <remarks>
/// 実験的なバックエンドです。スプライト・プリミティブ・トリム・RenderTexture の描画に対応しています。
/// カスタムシェーダー・マスク・扇形テクスチャ・ポストプロセスは未対応です。
/// 詳細は VULKAN_PORTING_PLAN.md を参照してください。
/// </remarks>
public class VulkanDesktopBackend : BackendBase
{
    private SilkNetCommonTimeProvider _time = null!;
    private IWindow _nativeWindow = null!;
    private PrometeApp _app = null!;
    private VulkanDesktopGameView _gameView = null!;
    private VulkanContext _context = null!;
    private VulkanResourceManager _resources = null!;
    private VulkanShaderManager _shaderManager = null!;
    private VulkanMaterialSystem _materialSystem = null!;
    private VulkanPipelineProvider _pipelines = null!;
    private VulkanTextureFactory _textureFactory = null!;
    private VulkanRenderTextureProvider _renderTextureProvider = null!;
    private VulkanScreenBlitter _screenBlitter = null!;
    private VulkanDrawTextureBatchedCommandRunner? _textureRunner;
    private VulkanDrawPieTextureCommandRunner? _pieRunner;
    private VulkanMaskedContainerHelper? _maskHelper;

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
        _nativeWindow.Closing += OnClosing;

        // インスタンスフックが登録されていれば使う (バリデーションレイヤー等)
        _app.TryGetPlugin<IVulkanInstanceHook>(out var instanceHook);
        _context = new VulkanContext(_nativeWindow, instanceHook);
        _resources = new VulkanResourceManager(_context);
        _shaderManager = new VulkanShaderManager(_context);
        _materialSystem = new VulkanMaterialSystem(_context, _shaderManager, _resources);
        _pipelines = new VulkanPipelineProvider(_context, _resources, _shaderManager, _materialSystem);
        _time = new SilkNetCommonTimeProvider(_nativeWindow);
        _gameView = new VulkanDesktopGameView(_app, _nativeWindow);
        _textureFactory = new VulkanTextureFactory(_app, _resources);
        _renderTextureProvider = new VulkanRenderTextureProvider(_context, _resources);
        _screenBlitter = new VulkanScreenBlitter(
            _context,
            _resources,
            _pipelines,
            _renderTextureProvider,
            _shaderManager,
            _materialSystem,
            _gameView
        );
        _gameView.AttachRenderingResources(
            _context,
            _resources,
            _renderTextureProvider,
            _screenBlitter,
            _textureFactory
        );
    }

    public override ITimeProvider SetupTimeProvider() => _time;

    public override IGameView SetupGameView() => _gameView;

    public override InputProvider SetupInputProvider() => new(_nativeWindow);

    public override IScreenBlitter SetupScreenBlitter() => _screenBlitter;

    public override TextureFactoryBase SetupTextureFactory() => _textureFactory;

    public override IRenderTextureProvider SetupRenderTextureProvider() => _renderTextureProvider;

    public override IShaderFactory SetupShaderFactory() =>
        new VulkanShaderFactory(_shaderManager, _pipelines);

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
        _context.Initialize(_nativeWindow.Title);
        _screenBlitter.InitializeScreenRenderTexture();

        // ランナーをコマンドキューへ登録する
        _textureRunner = new VulkanDrawTextureBatchedCommandRunner(
            _context,
            _resources,
            _pipelines,
            _shaderManager,
            _materialSystem
        );
        _pieRunner = new VulkanDrawPieTextureCommandRunner(
            _context,
            _resources,
            _pipelines,
            _shaderManager,
            _materialSystem
        );
        var queue = _app.GetPlugin<RenderCommandQueue>();
        _maskHelper = new VulkanMaskedContainerHelper(
            _app,
            queue,
            _context,
            _resources,
            _pipelines,
            _renderTextureProvider
        );
        queue.RegisterRunnerRange(
            _textureRunner,
            _pieRunner,
            new VulkanDrawPrimitiveCommandRunner(_context, _pipelines, _shaderManager, _materialSystem),
            new VulkanBeginTrimCommandRunner(_context),
            new VulkanEndTrimCommandRunner(_context),
            new VulkanBeginStencilMaskCommandRunner(_context, _maskHelper),
            new VulkanBeginAlphaMaskCommandRunner(_maskHelper),
            new VulkanEndMaskCommandRunner(_context)
        );
    }

    private void OnClosing()
    {
        _app.OnDestroy();

        if (!_context.IsInitialized)
            return;
        _context.WaitIdle();
        _maskHelper?.Dispose();
        _textureRunner?.Dispose();
        _pieRunner?.Dispose();
        _pipelines.Dispose();
        _materialSystem.Dispose();
        _shaderManager.Dispose();
        _resources.Dispose();
        _context.Dispose();
    }

    private void OnRenderFrame(double delta)
    {
        if (!_context.IsInitialized)
            return;
        if (!_context.BeginFrame())
            return;

        // ノード走査 → コマンドキュー実行 (ScreenRenderTexture へのキャプチャ) → ブリット
        _app.OnRender();

        _context.EndFrame();
    }
}
