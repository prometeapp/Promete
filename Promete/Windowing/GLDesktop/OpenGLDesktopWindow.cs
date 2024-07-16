using System;
using System.Threading;
using System.Threading.Tasks;
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
			get => size;
			set
			{
				size = value;
				UpdateWindowSize();
			}
		}

		public VectorInt ActualSize => new VectorInt(window.FramebufferSize.X, window.FramebufferSize.Y) / Scale;

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

		public int Scale
		{
			get => scale;
			set
			{
				if (value is not 1 and not 2 and not 4 and not 8)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Scale must be 1, 2, 4, or 8.");
				}
				scale = value;
				UpdateWindowSize();
			}
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
			set
			{
				if (window.Title == value) return;
				window.Title = value;
			}
		}

		public long TotalFrame { get; private set; }

		public float TotalTime { get; private set; }

		public float DeltaTime { get; private set; }

		public long FramePerSeconds { get; private set; }

		public long UpdatePerSeconds { get; private set; }

		public int RefreshRate
		{
			get => (int)window.FramesPerSecond;
			set => window.FramesPerSecond = value;
		}

		public bool IsVsyncMode
		{
			get => window.VSync;
			set => window.VSync = value;
		}

		public float PixelRatio => window.Size.X == 0 ? 1 : window.FramebufferSize.X / window.Size.X;

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

		private int scale = 1;
		private int frameCount;
		private int updateCount;
		private int prevSecondUps;
		private int prevSecondFps;
		private byte[] screenshotBuffer = [];
		private VectorInt size = (640, 480);
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
			options.FramesPerSecond = 60;
			options.VSync = false;

			window = Window.Create(options);
			window.Load += OnLoad;
			window.Resize += OnResize;
			window.FileDrop += OnFileDrop;
			window.Render += OnRenderFrame;
			window.Update += OnUpdateFrame;
			window.Closing += OnUnload;
			window.FocusChanged += v => IsFocused = v;
		}

		public Texture2D TakeScreenshot()
		{
			return TextureFactory.LoadFromImageSharpImage(TakeScreenshotAsImage());
		}

		public async Task SaveScreenshotAsync(string path, CancellationToken ct = default)
		{
			var img = TakeScreenshotAsImage();
			await img.SaveAsPngAsync(path, ct);
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
				gl?.ReadPixels(0, 0, (uint)(ActualWidth * scale), (uint)(ActualHeight * scale), PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
			}
			var img = Image.LoadPixelData<Rgba32>(screenshotBuffer, ActualWidth * scale, ActualHeight * scale);
			img.Mutate(i => i.Flip(FlipMode.Vertical));
			return img;
		}

		private void OnLoad()
		{
			gl = window.CreateOpenGL();
			_RawInputContext = window.CreateInput();
			textureFactory = new OpenGLTextureFactory(gl, app);
			UpdateWindowSize();

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

			if (app.Root != null) app.RenderElement(app.Root);
			CalculateFps();

			Render?.Invoke();
		}

		private void OnUpdateFrame(double delta)
		{
			var deltaTime = (float)delta;
			TotalTime += deltaTime;
			DeltaTime = deltaTime;

			CalculateUps();

			PreUpdate?.Invoke();
			if (app.Root != null) app.UpdateElement(app.Root);
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
			if (Environment.TickCount - prevSecondUps <= 1000) return;

			UpdatePerSeconds = updateCount;
			updateCount = 0;
			prevSecondUps = Environment.TickCount;
		}

		private void CalculateFps()
		{
			frameCount++;
			if (Environment.TickCount - prevSecondFps <= 1000) return;

			FramePerSeconds = frameCount;
			frameCount = 0;
			prevSecondFps = Environment.TickCount;
		}

		private void UpdateWindowSize()
		{
			window.Size = new Vector2D<int>(Size.X, Size.Y) * scale;
			screenshotBuffer = new byte[window.FramebufferSize.X * window.FramebufferSize.Y * scale * 4];
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
