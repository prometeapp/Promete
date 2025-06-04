using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Promete.Graphics;
using Promete.Internal;
using Promete.Nodes;
using Promete.Nodes.Renderer;
using Promete.Windowing;

namespace Promete;

/// <summary>
/// Promete のアプリケーションを表します。
/// </summary>
public sealed class PrometeApp : IDisposable
{

    /// <summary>
    /// 現在読み込まれているシーンのルートコンテナを取得します。
    /// </summary>
    public Container? Root => _currentScene?.Root;

    /// <summary>
    /// シーンのルートコンテナよりも背面に描画されるコンテナを取得します。
    /// このコンテナに登録されたノードは、シーンが切り替わっても破棄されません。
    /// </summary>
    public Container GlobalBackground { get; } = [];

    /// <summary>
    /// シーンのルートコンテナよりも前面に描画されるコンテナを取得します。
    /// このコンテナに登録されたノードは、シーンが切り替わっても破棄されません。
    /// </summary>
    public Container GlobalForeground { get; } = [];

    /// <summary>
    /// 現在の背景色を取得または設定します。
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Black;

    /// <summary>
    /// 実行中のPromete ウィンドウを取得します。
    /// </summary>
    public IWindow Window { get; }

    /// <summary>
    /// フレームバッファがサポートされているかどうかを取得します。
    /// </summary>
    public bool IsFrameBufferSupported => _provider.GetService<IFrameBufferProvider>() is not null;

    private Scene? _currentScene;
    private int _statusCode;

    private static PrometeApp? _current;

    private readonly Thread _mainThread;
    private readonly ConcurrentQueue<Action> _nextFrameQueue = new();
    private readonly Stack<Scene> _sceneStack = new();

    private readonly ServiceProvider _provider;
    private readonly ServiceCollection _services;
    private readonly Dictionary<Type, NodeRendererBase?> _renderers = new();
    private readonly Dictionary<Type, Type> _rendererTypes;

    private PrometeApp(ServiceCollection services, Dictionary<Type, Type> rendererTypes)
    {
        _mainThread = Thread.CurrentThread;

        _services = services;
        _rendererTypes = rendererTypes;
        RegisterAllScenes();
        services.AddSingleton(this);
        services.AddSingleton<FrameBufferManager>();

        _provider = services.BuildServiceProvider();

        Current = this;
        Window = _provider.GetService<IWindow>() ??
                 throw new InvalidOperationException("There is no IWindow-implemented service in the system.");
    }

    /// <summary>
    /// 実行中の <see cref="PrometeApp" /> を取得します。
    /// <exception cref="InvalidOperationException">Prometeが初期化されていない。</exception>
    /// </summary>
    public static PrometeApp Current
    {
        get => _current ?? throw new InvalidOperationException("Promete is not initialized.");
        private set => _current = value;
    }

    /// <summary>
    /// アプリケーションのリソースを破棄します。
    /// </summary>
    public void Dispose()
    {
        _provider.Dispose();
    }

    /// <summary>
    /// Promete アプリケーションを作成します。
    /// </summary>
    public static PrometeAppBuilder Create()
    {
        return new PrometeAppBuilder();
    }

    /// <summary>
    /// Promete アプリケーションを実行します。
    /// </summary>
    /// <typeparam name="TScene">実行時に呼び出されるシーン。</typeparam>
    /// <returns>終了ステータスコード。</returns>
    public int Run<TScene>() where TScene : Scene
    {
        return Run<TScene>(WindowOptions.Default);
    }

    /// <summary>
    /// Promete アプリケーションを実行します。
    /// </summary>
    /// <typeparam name="TScene">実行時に呼び出されるシーン。</typeparam>
    /// <param name="opts">ウィンドウのオプション。</param>
    /// <returns>終了ステータスコード。</returns>
    public int Run<TScene>(WindowOptions opts) where TScene : Scene
    {
        Window.Start += OnStart<TScene>;
        Window.Update += OnUpdate;
        Window.Render += OnRender;
        Window.Destroy += OnDestroy;
        Window.Run(opts);
        return _statusCode;
    }

    /// <summary>
    /// 指定したステータスコードで Promete アプリケーションを終了します。
    /// </summary>
    /// <param name="status">ステータスコード。</param>
    public void Exit(int status = 0)
    {
        _statusCode = status;
        Window.Exit();
    }

    /// <summary>
    /// 次のフレームに、指定した処理を実行するように予約します。
    /// </summary>
    /// <param name="action">次のフレームに実行する処理。</param>
    public void NextFrame(Action action)
    {
        _nextFrameQueue.Enqueue(action);
    }

    /// <summary>
    /// 指定した型のプラグインを取得します。
    /// </summary>
    /// <typeparam name="T">指定対象のプラグインを表す型。</typeparam>
    /// <returns>プラグインのインスタンス。</returns>
    /// <exception cref="ArgumentException">指定したプラグインが登録されていない。</exception>
    public T GetPlugin<T>() where T : class
    {
        return _provider.GetService<T>() ?? throw new ArgumentException($"The plugin \"{typeof(T)}\" is not registered.");
    }

    /// <summary>
    /// 指定した型のプラグインを取得します。
    /// </summary>
    /// <param name="type">指定対象のプラグインを表す型。</param>
    /// <returns>プラグインのインスタンス。</returns>
    /// <exception cref="ArgumentException">指定したプラグインが登録されていない。</exception>
    public object GetPlugin(Type type)
    {
        return TryGetPlugin(type, out var plugin) ? plugin : throw new ArgumentException($"The plugin \"{type}\" is not registered.");
    }

    /// <summary>
    /// 指定した型のプラグインを取得を試みます。
    /// </summary>
    /// <typeparam name="T">指定対象のプラグインを表す型。</typeparam>
    /// <param name="plugin">取得したプラグインのインスタンス。</param>
    /// <returns>プラグインが取得できた場合は <see langword="true" />、それ以外の場合は <see langword="false" />。</returns>
    public bool TryGetPlugin<T>([NotNullWhen(true)] out T? plugin) where T : class
    {
        plugin = _provider.GetService<T>();
        return plugin is not null;
    }

    /// <summary>
    /// 指定した型のプラグインを取得を試みます。
    /// </summary>
    /// <param name="type">指定対象のプラグインを表す型。</param>
    /// <param name="plugin">取得したプラグインのインスタンス。</param>
    /// <returns>プラグインが取得できた場合は <see langword="true" />、それ以外の場合は <see langword="false" />。</returns>
    public bool TryGetPlugin(Type type, [NotNullWhen(true)] out object? plugin)
    {
        plugin = _provider.GetService(type);
        return plugin is not null;
    }

    /// <summary>
    /// シーンを読み込みます。現在読み込まれているシーンがある場合、そのシーンは破棄されます。
    /// </summary>
    /// <typeparam name="TScene">読み込むシーン。</typeparam>
    /// <exception cref="ArgumentException">指定したシーンが存在しない。</exception>
    public void LoadScene<TScene>() where TScene : Scene
    {
        LoadScene(typeof(TScene));
    }

    /// <summary>
    /// シーンを読み込みます。現在読み込まれているシーンがある場合、そのシーンは破棄されます。
    /// </summary>
    /// <param name="typeScene">読み込むシーン。</param>
    /// <exception cref="ArgumentException">指定したシーンが存在しない。</exception>
    public void LoadScene(Type typeScene)
    {
        _currentScene?.OnDestroy();
        _currentScene = GetScene(typeScene);
        SceneWillChange?.Invoke();
        _currentScene.OnStart();
    }

    /// <summary>
    /// 現在のシーンをプッシュし、新たなシーンを読み込みます。
    /// </summary>
    /// <typeparam name="TScene">読み込むシーン。</typeparam>
    public void PushScene<TScene>()
    {
        PushScene(typeof(TScene));
    }

    /// <summary>
    /// 現在のシーンをプッシュし、新たなシーンを読み込みます。
    /// </summary>
    /// <param name="typeScene">読み込むシーン。</param>
    public void PushScene(Type typeScene)
    {
        if (_currentScene != null)
        {
            _sceneStack.Push(_currentScene);
            _currentScene.OnPause();
        }
        _currentScene = GetScene(typeScene);
        SceneWillChange?.Invoke();
        _currentScene.OnStart();
    }

    /// <summary>
    /// 現在のシーンを破棄し、スタックからシーンを1つポップし、復帰させます。
    /// </summary>
    /// <returns>スタックからシーンをポップできた場合は <see langword="true" />。それ以外の場合は <see langword="false" />。</returns>
    public bool PopScene()
    {
        if (_sceneStack.Count == 0) return false;

        _currentScene?.OnDestroy();
        _currentScene = _sceneStack.Pop();
        SceneWillChange?.Invoke();
        _currentScene.OnResume();
        return true;
    }

    /// <summary>
    /// スタックにある全てのシーンを破棄します。
    /// </summary>
    public void ClearSceneStack()
    {
        while (_sceneStack.TryPop(out var scene))
        {
            scene.OnDestroy();
        }
    }

    /// <summary>
    /// 指定した <see cref="Node" /> を描画します。
    /// </summary>
    /// <param name="node">描画対象のノード。</param>
    public void RenderNode(Node node)
    {
        // ノードが非表示あるいは破棄されている場合は描画しない
        if (!node.IsVisible || node.IsDestroyed) return;

        node.BeforeRender();
        var renderer = ResolveRenderer(node);
        renderer?.Render(node);
    }

    /// <summary>
    /// 指定した <see cref="Node" /> を更新します。
    /// </summary>
    /// <param name="node">更新対象のノード。</param>
    public void UpdateNode(Node node)
    {
        node.Update();
    }

    /// <summary>
    /// このメソッドが呼び出されたスレッドがメインスレッドであるかどうかを取得します。
    /// </summary>
    /// <returns>メインスレッドの場合は <see langword="true" />、それ以外の場合は <see langword="false" />。</returns>
    public bool IsMainThread()
    {
        return Thread.CurrentThread == _mainThread;
    }

    /// <summary>
    /// このメソッドが呼び出されたスレッドがメインスレッドでない場合、<see cref="InvalidOperationException" /> をスローします。
    /// </summary>
    /// <exception cref="InvalidOperationException">メインスレッド以外から呼び出された場合。</exception>
    public void ThrowIfNotMainThread()
    {
        if (IsMainThread()) return;
        throw new InvalidOperationException("This method must be called from the main thread.");
    }

    private void OnStart<TScene>() where TScene : Scene
    {
        foreach (var (nodeType, rendererType) in _rendererTypes)
            _renderers[nodeType] = _provider.GetService(rendererType) as NodeRendererBase ??
                                   throw new ArgumentException($"The renderer \"{rendererType}\" is not registered.");

        LoadScene<TScene>();
    }

    private void OnUpdate()
    {
        UpdateNode(GlobalBackground);
        if (Root != null) UpdateNode(Root);
        UpdateNode(GlobalForeground);
        _currentScene?.OnUpdate();

        ProcessNextFrameQueue();
    }

    private void OnRender()
    {
        RenderNode(GlobalBackground);
        if (Root != null) RenderNode(Root);
        RenderNode(GlobalForeground);
    }

    private void OnDestroy()
    {
        _currentScene?.OnDestroy();
        ClearSceneStack();
    }

    private void ProcessNextFrameQueue()
    {
        while (!_nextFrameQueue.IsEmpty)
        {
            if (!_nextFrameQueue.TryDequeue(out var task)) return;
            task();
        }
    }

    private NodeRendererBase? ResolveRenderer(Node node)
    {
        var nodeType = node.GetType();
        if (_renderers.TryGetValue(nodeType, out var renderer)) return renderer;

        // ノードの型が登録されていない場合、親クラスの型が登録されているかを確認する
        var alternativeRendererType = _renderers.Keys.FirstOrDefault(k => nodeType.IsSubclassOf(k));
        if (alternativeRendererType is null)
        {
            LogHelper.Warn($"The renderer for \"{nodeType}\" is not registered.");
            _renderers[nodeType] = null;
            return null;
        }

        _renderers[nodeType] = _renderers[alternativeRendererType];
        return _renderers[nodeType];
    }

    private void RegisterAllScenes()
    {
        var asm = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("There is no entry assembly.");
        // Scene 派生クラスを全て取得する
        var types = asm.GetTypes();
        foreach (var type in types.Where(t => t.IsSubclassOf(typeof(Scene))))
        {
            // IgnoredSceneAttribute が付与されている場合は無視する
            if (type.GetCustomAttribute<IgnoredSceneAttribute>() is not null) continue;

            // Scene 派生クラスを登録する
            _services.AddTransient(type);
        }
    }

    private Scene GetScene(Type scene)
    {
        return _provider.GetService(scene) as Scene ??
               throw new ArgumentException($"The scene \"{scene.Name}\" is not registered.");
    }

    /// <summary>
    /// シーンが変更される直前に呼び出されるイベントです。
    /// </summary>
    public event Action? SceneWillChange;

    /// <summary>
    /// Promete アプリケーションを構築するためのビルダークラスです。
    /// </summary>
    public sealed class PrometeAppBuilder
    {
        private readonly Dictionary<Type, Type> _rendererTypes = [];
        private readonly ServiceCollection _services;

        internal PrometeAppBuilder()
        {
            _services = [];
        }

        /// <summary>
        /// 指定した型のプラグインを追加します。
        /// </summary>
        /// <typeparam name="T">追加するプラグインの型。</typeparam>
        /// <returns>このビルダーインスタンス。</returns>
        public PrometeAppBuilder Use<T>() where T : class
        {
            _services.AddSingleton<T>();
            return this;
        }

        /// <summary>
        /// 指定したインターフェースと実装型のプラグインを追加します。
        /// </summary>
        /// <typeparam name="TPlugin">プラグインのインターフェース型。</typeparam>
        /// <typeparam name="TImpl">プラグインの実装型。</typeparam>
        /// <returns>このビルダーインスタンス。</returns>
        public PrometeAppBuilder Use<TPlugin, TImpl>() where TPlugin : class where TImpl : class, TPlugin
        {
            _services.AddSingleton<TPlugin, TImpl>();
            return this;
        }

        /// <summary>
        /// 指定したノード型とレンダラー型のレンダラーを追加します。
        /// </summary>
        /// <typeparam name="TNode">ノードの型。</typeparam>
        /// <typeparam name="TRenderer">レンダラーの型。</typeparam>
        /// <returns>このビルダーインスタンス。</returns>
        public PrometeAppBuilder UseRenderer<TNode, TRenderer>()
            where TRenderer : NodeRendererBase
            where TNode : Node
        {
            _rendererTypes[typeof(TNode)] = typeof(TRenderer);
            return Use<TRenderer>();
        }

        /// <summary>
        /// Promete アプリケーションをビルドします。
        /// </summary>
        /// <typeparam name="TWindow">ウィンドウの型。</typeparam>
        /// <returns>構築されたアプリケーション。</returns>
        public PrometeApp Build<TWindow>() where TWindow : IWindow
        {
            _services.AddSingleton(typeof(IWindow), typeof(TWindow));
            return new PrometeApp(_services, _rendererTypes);
        }
    }
}
