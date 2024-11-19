using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Promete.Elements;
using Promete.Elements.Renderer;
using Promete.Internal;
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
	public Container? Root => currentScene?.Root;

	/// <summary>
	/// 現在の背景色を取得または設定します。
	/// </summary>
	public Color BackgroundColor { get; set; } = Color.Black;

	/// <summary>
	/// 実行中のPromete ウィンドウを取得します。
	/// </summary>
	public IWindow Window { get; }

	/// <summary>
	/// 実行中の <see cref="PrometeApp"/> を取得します。
	/// <exception cref="InvalidOperationException">Prometeが初期化されていない。</exception>
	/// </summary>
	public static PrometeApp Current
	{
		get => _current ?? throw new InvalidOperationException("Promete is not initialized.");
		private set => _current = value;
	}

	private Scene? currentScene;
	private int statusCode = 0;

	private readonly ServiceCollection services;
	private readonly ServiceProvider provider;
	private readonly ConcurrentQueue<Action> nextFrameQueue = new();
	private readonly Dictionary<Type, Type> rendererTypes;
	private readonly Dictionary<Type, ElementRendererBase?> renderers = new();
	private readonly Thread mainThread;

	private static PrometeApp? _current;

	private PrometeApp(ServiceCollection services, Dictionary<Type, Type> rendererTypes)
	{
		mainThread = Thread.CurrentThread;

		this.services = services;
		this.rendererTypes = rendererTypes;
		RegisterAllScenes();
		services.AddSingleton(this);

		provider = services.BuildServiceProvider();

		Current = this;
		Window = provider.GetService<IWindow>() ?? throw new InvalidOperationException("There is no IWindow-implemented service in the system.");
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
		Window.Run(opts);
		return statusCode;
	}



	/// <summary>
	/// 指定したステータスコードで Promete アプリケーションを終了します。
	/// </summary>
	/// <param name="status">ステータスコード。</param>
	public void Exit(int status = 0)
	{
		statusCode = status;
		Window.Exit();
	}

	/// <summary>
	/// 次のフレームに、指定した処理を実行するように予約します。
	/// </summary>
	/// <param name="action">次のフレームに実行する処理。</param>
	public void NextFrame(Action action)
	{
		nextFrameQueue.Enqueue(action);
	}

	/// <summary>
	/// 指定した型のプラグインを取得します。
	/// </summary>
	/// <typeparam name="T">指定対象のプラグインを表す型。</typeparam>
	/// <returns>プラグインが見つかった場合はそのインスタンス。見つからなかった場合は <see langword="null"/>。</returns>
	public T? GetPlugin<T>() where T : class
	{
		return provider.GetService<T>();
	}

	/// <summary>
	/// 指定した型のプラグインを取得します。
	/// </summary>
	/// <param name="type">指定対象のプラグインを表す型。</param>
	/// <returns>プラグインが見つかった場合はそのインスタンス。見つからなかった場合は <see langword="null"/>。</returns>
	public object? GetPlugin(Type type)
	{
		return provider.GetService(type);
	}

	public void Dispose()
	{
		provider.Dispose();
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
		currentScene?.OnDestroy();

		currentScene = provider.GetService(typeScene) as Scene ?? throw new ArgumentException($"The scene \"{typeScene}\" is not registered.");
		SceneWillChange?.Invoke();
		currentScene.OnStart();
	}

	/// <summary>
	/// 指定した Element を描画します。
	/// </summary>
	/// <param name="element">描画対象の Element。</param>
	public void RenderElement(ElementBase element)
	{
		element.BeforeRender();
		var renderer = ResolveRenderer(element);
		renderer?.Render(element);
	}

	/// <summary>
	/// 指定した Element を更新します。
	/// </summary>
	/// <param name="element">更新対象の Element。</param>
	public void UpdateElement(ElementBase element)
	{
		element.Update();
	}

	/// <summary>
	/// このメソッドが呼び出されたスレッドがメインスレッドであるかどうかを取得します。
	/// </summary>
	/// <returns></returns>
	public bool IsMainThread()
	{
		return Thread.CurrentThread == mainThread;
	}

	/// <summary>
	/// このメソッドが呼び出されたスレッドがメインスレッドでない場合、<see cref="InvalidOperationException"/> をスローします。
	/// </summary>
	/// <exception cref="InvalidOperationException"></exception>
	public void ThrowIfNotMainThread()
	{
		if (IsMainThread()) return;
		throw new InvalidOperationException("This method must be called from the main thread.");
	}

	private void OnStart<TScene>() where TScene : Scene
	{
		foreach (var (elementType, rendererType) in rendererTypes)
		{
			renderers[elementType] = provider.GetService(rendererType) as ElementRendererBase ?? throw new ArgumentException($"The renderer \"{rendererType}\" is not registered.");
		}

		LoadScene<TScene>();
	}

	private void OnUpdate()
	{
		currentScene?.OnUpdate();

		ProcessNextFrameQueue();
	}

	private void ProcessNextFrameQueue()
	{
		while (!nextFrameQueue.IsEmpty)
		{
			if (!nextFrameQueue.TryDequeue(out var task)) return;
			task();
		}
	}

	private ElementRendererBase? ResolveRenderer(ElementBase el)
	{
		var elType = el.GetType();
		if (renderers.TryGetValue(elType, out var renderer)) return renderer;

		// el の型が登録されていない場合、el の型の親クラスの型が登録されているかを確認する
		var alternativeRendererType = renderers.Keys.FirstOrDefault(k => elType.IsSubclassOf(k));
		if (alternativeRendererType is null)
		{
			LogHelper.Warn($"The renderer for \"{elType}\" is not registered.");
			renderers[elType] = null;
			return null;
		}

		renderers[elType] = renderers[alternativeRendererType];
		return renderers[elType];
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
			services.AddTransient(type);
		}
	}

	public sealed class PrometeAppBuilder
	{
		private readonly ServiceCollection services;
		private readonly Dictionary<Type, Type> rendererTypes = new();

		internal PrometeAppBuilder()
		{
			services = new ServiceCollection();
		}

		public PrometeAppBuilder Use<T>() where T : class
		{
			services.AddSingleton<T>();
			return this;
		}

		public PrometeAppBuilder Use<TPlugin, TImpl>() where TPlugin : class where TImpl : class, TPlugin
		{
			services.AddSingleton<TPlugin, TImpl>();
			return this;
		}

		public PrometeAppBuilder UseRenderer<TElement, TRenderer>()
			where TRenderer : ElementRendererBase
			where TElement : ElementBase
		{
			rendererTypes[typeof(TElement)] = typeof(TRenderer);
			return Use<TRenderer>();
		}

		public PrometeApp Build<TWindow>() where TWindow : IWindow
		{
			services.AddSingleton(typeof(IWindow), typeof(TWindow));
			return new PrometeApp(services, rendererTypes);
		}
	}

	public event Action? SceneWillChange;
}
