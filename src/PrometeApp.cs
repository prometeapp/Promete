using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Promete.Elements;
using Promete.Internal;
using Promete.Windowing;

namespace Promete;

public sealed class PrometeApp : IDisposable
{
	public Container Root { get; } = new();
	public Color BackgroundColor { get; set; } = Color.Black;

	private Scene? currentScene;
	private int statusCode = 0;

	private readonly ServiceCollection services;
	private readonly ServiceProvider provider;
	private readonly Queue<Action> nextFrameQueue = new();

	private PrometeApp(ServiceCollection services)
	{
		SynchronizationContext.SetSynchronizationContext(new PrSynchronizationContext());

		this.services = services;
		RegisterAllScenes();
		services.AddSingleton(this);
		provider = services.BuildServiceProvider();
	}

	public static PrometeAppBuilder Create()
	{
		return new PrometeAppBuilder();
	}

	public int Run<TScene>() where TScene : Scene
	{
		var window = provider.GetService<IWindow>() ?? throw new InvalidOperationException("There is no IWindow-implemented service in the system.");

		window.Start += LoadScene<TScene>;
		window.Update += OnUpdate;
		window.Run();
		return statusCode;
	}

	public void Exit(int status = 0)
	{
		statusCode = status;
		var window = provider.GetService<IWindow>() ?? throw new InvalidOperationException("There is no IWindow-implemented service in the system.");

		window.Exit();
	}

	public void NextFrame(Action action)
	{
		nextFrameQueue.Enqueue(action);
	}

	public void Dispose()
	{
		provider.Dispose();
	}

	public void LoadScene<TScene>() where TScene : Scene
	{
		currentScene?.OnDestroy();

		currentScene = provider.GetService<TScene>() ?? throw new ArgumentException($"The scene \"{nameof(TScene)}\" is not registered.");
		currentScene.OnStart();
	}

	private void OnUpdate()
	{
		currentScene?.OnUpdate();

		ProcessNextFrameQueue();
	}

	private void ProcessNextFrameQueue()
	{
		while (nextFrameQueue.Count > 0)
		{
			nextFrameQueue.Dequeue()();
		}
	}

	private void RegisterAllScenes()
	{
		var asm = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("There is no entry assembly.");
		// Scene 派生クラスを全て取得する
		var types = asm.GetTypes();
		foreach (var type in types.Where(t => t.IsSubclassOf(typeof(Scene))))
		{
			// Scene 派生クラスを登録する
			services.AddTransient(type);
		}
	}

	public sealed class PrometeAppBuilder
	{
		private readonly ServiceCollection services;

		internal PrometeAppBuilder()
		{
			services = new ServiceCollection();
		}

		public PrometeAppBuilder Use<T>() where T : class
		{
			services.AddSingleton<T>();
			return this;
		}

		public PrometeApp Build<TWindow>() where TWindow : IWindow
		{
			services.AddSingleton(typeof(IWindow), typeof(TWindow));
			return new PrometeApp(services);
		}
	}
}
