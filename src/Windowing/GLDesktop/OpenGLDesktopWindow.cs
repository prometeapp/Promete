using System;
using Promete.Graphics;
using SixLabors.ImageSharp;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Promete.Windowing.GLDesktop
{
	/// <summary>
	/// A implementation of <see cref="IWindow"/> for the desktop environment.
	/// </summary>
	public sealed class OpenGLDesktopWindow : IWindow
	{
		public VectorInt Location
		{
			get => (window.Position.X, window.Position.Y);
			set => window.Position = new Vector2D<int>(value.X, value.Y);
		}

		public VectorInt Size
		{
			get => (window.Size.X, window.Size.Y);
			set
			{
				window.Size = new(value.X, value.Y);
				screenshotBuffer = new byte[ActualWidth * ActualHeight * 4];
			}
		}

		public VectorInt ActualSize => (window.FramebufferSize.X, window.FramebufferSize.Y);

		public int X
		{
			get => Location.X;
			set => Location = (value, Y);
		}

		public int Y
		{
			get => Location.Y;
			set => Location = (X, value);
		}

		public int Width
		{
			get => Size.X;
			set => Size = (value, Height);
		}

		public int Height
		{
			get => Size.Y;
			set => Size = (Width, value);
		}

		public int ActualWidth => ActualSize.X;

		public int ActualHeight => ActualSize.Y;

		public bool IsVisible
		{
			get => window.IsVisible;
			set => window.IsVisible = value;
		}

		public bool IsFocused { get; private set; }

		public bool IsFullScreen
		{
			get => window.WindowState == WindowState.Fullscreen;
			set => window.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
		}

		public string Title
		{
			get => window.Title;
			set => window.Title = value;
		}

		public long TotalFrame { get; private set; }

		public float TotalTime { get; private set; }

		public float DeltaTime { get; private set; }

		public long FramePerSeconds { get; private set; }

		public long UpdatePerSeconds { get; private set; }

		// TODO: ゲーム起動前に変更可能にする
		public int RefreshRate => 60;

		public float PixelRatio => window.FramebufferSize.X / window.Size.X;

		public WindowMode Mode
		{
			get => window.WindowBorder switch
			{
				WindowBorder.Fixed => WindowMode.Fixed,
				WindowBorder.Hidden => WindowMode.NoFrame,
				WindowBorder.Resizable => WindowMode.Resizable,
				_ => throw new InvalidOperationException("unexpected window state"),
			};
			set => window.WindowBorder = value switch
			{
				WindowMode.Fixed => WindowBorder.Fixed,
				WindowMode.NoFrame => WindowBorder.Hidden,
				WindowMode.Resizable => WindowBorder.Resizable,
				_ => throw new ArgumentException(null, nameof(value)),
			};
		}
		public IInputContext? _RawInputContext { get; private set; }

		public TextureFactory TextureFactory => textureFactory ?? throw new InvalidOperationException("window is not loaded");

		public GL GL => gl ?? throw new InvalidOperationException("window is not loaded");

		private int frameCount;
		private int updateCount;
		private int prevSecond;
		private byte[] screenshotBuffer = Array.Empty<byte>();
		private GL? gl;
		private TextureFactory? textureFactory;

		private readonly PrometeApp app;
		private readonly Silk.NET.Windowing.IWindow window;

		public OpenGLDesktopWindow(PrometeApp app)
		{
			this.app = app;

			var options = WindowOptions.Default;
 			options.Size = new Vector2D<int>(640, 480);
 			options.Title = "Promete Window";
			options.WindowBorder = WindowBorder.Fixed;
			options.FramesPerSecond = RefreshRate;

			window = Window.Create(options);
			window.Load += OnLoad;
			window.Resize += OnResize;
			window.FileDrop += OnFileDrop;
			window.Render += OnRenderFrame;
			window.Update += OnUpdateFrame;
			window.Closing += OnUnload;
			window.FocusChanged += v => IsFocused = v;
		}

		public ITexture TakeScreenshot()
		{
			return TextureFactory.LoadFromImageSharpImage(TakeScreenshotAsImage());
		}

		public void Run()
		{
			window.Run();
		}

		public void Exit()
		{
			window.Close();
		}

		private unsafe Image TakeScreenshotAsImage()
		{
			fixed (byte* buffer = screenshotBuffer)
			{
				gl?.ReadPixels(0, 0, (uint)ActualWidth, (uint)ActualHeight, GLEnum.Rgba, GLEnum.UnsignedByte, buffer);
			}
			var img = Image.LoadPixelData<Rgba32>(screenshotBuffer, ActualWidth, ActualHeight);
			img.Mutate(i => i.Flip(FlipMode.Vertical));
			return img;
		}

		private void OnLoad()
		{
			gl = window.CreateOpenGL();
			_RawInputContext = window.CreateInput();
			textureFactory = new OpenGLTextureFactory(gl);
			screenshotBuffer = new byte[ActualWidth * ActualHeight * 4];

			Start?.Invoke();
		}

		private void OnResize(Vector2D<int> vec)
		{
			gl?.Viewport(window.FramebufferSize);

			Resize?.Invoke();
		}

		private void OnFileDrop(string[] files)
		{
			FileDropped?.Invoke(new FileDroppedEventArgs(files));
		}

		private void OnRenderFrame(double delta)
		{
			if (gl == null) return;
			// 画面の初期化
			gl.ClearColor(app.BackgroundColor);
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			app.Render(app.Root);

			Render?.Invoke();
		}

		private void OnUpdateFrame(double delta)
		{
			var deltaTime = (float)delta;
			TotalTime += deltaTime;
			DeltaTime = deltaTime;

			CalculateUps();
			CalculateFps();

			PreUpdate?.Invoke();
			app.Root.Update();
			Update?.Invoke();
			PostUpdate?.Invoke();

			TotalFrame++;
		}

		private void OnUnload()
		{
			Destroy?.Invoke();
		}

		private void CalculateUps()
		{
			updateCount++;
			if (Environment.TickCount - prevSecond <= 1000) return;

			UpdatePerSeconds = updateCount;
			updateCount = 0;
			prevSecond = Environment.TickCount;
		}

		private void CalculateFps()
		{
			frameCount++;
			if (Environment.TickCount - prevSecond <= 1000) return;

			UpdatePerSeconds = frameCount;
			frameCount = 0;
			prevSecond = Environment.TickCount;
		}

		public event Action? Start;
		public event Action? Update;
		public event Action? Render;
		public event Action? Destroy;
		public event Action<FileDroppedEventArgs>? FileDropped;
		public event Action? Resize;
		public event Action? PreUpdate;
		public event Action? PostUpdate;
	}
}
