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
	public sealed class OpenGLDesktopWindow(PrometeApp app) : IWindow
	{
		public VectorInt Location
		{
			get => (_window.Position.X, _window.Position.Y);
			set => _window.Position = new Vector2D<int>(value.X, value.Y);
		}

		public VectorInt Size
		{
			get => _size;
			set
			{
				_size = value;
				UpdateWindowSize();
			}
		}

		public VectorInt ActualSize => new VectorInt(_window.FramebufferSize.X, _window.FramebufferSize.Y) / Scale;

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
			get => _scale;
			set
			{
				if (value is not 1 and not 2 and not 4 and not 8)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Scale must be 1, 2, 4, or 8.");
				}
				_scale = value;
				UpdateWindowSize();
			}
		}

		public int ActualWidth => ActualSize.X;

		public int ActualHeight => ActualSize.Y;

		public bool IsVisible
		{
			get => _window.IsVisible;
			set => _window.IsVisible = value;
		}

		public bool IsFocused { get; private set; }

		public bool IsFullScreen
		{
			get => _window.WindowState == WindowState.Fullscreen;
			set => _window.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
		}

		public string Title
		{
			get => _window.Title;
			set
			{
				if (_window.Title == value) return;
				_window.Title = value;
			}
		}

		public long TotalFrame { get; private set; }

		public float TotalTime { get; private set; }

		public float DeltaTime { get; private set; }

		public long FramePerSeconds { get; private set; }

		public long UpdatePerSeconds { get; private set; }

		public int TargetFps
		{
			get => (int)_window.FramesPerSecond;
			set => _window.FramesPerSecond = value;
		}

		public int TargetUps
		{
			get => (int)_window.UpdatesPerSecond;
			set => _window.UpdatesPerSecond = value;
		}

		[Obsolete("Use TargetFps instead.")]
		public int RefreshRate
		{
			get => (int)_window.FramesPerSecond;
			set => _window.FramesPerSecond = value;
		}

		public bool IsVsyncMode
		{
			get => _window.VSync;
			set => _window.VSync = value;
		}

		public float PixelRatio => _window.Size.X == 0 ? 1 : _window.FramebufferSize.X / _window.Size.X;

		public WindowMode Mode
		{
			get => _window.WindowBorder switch
			{
				WindowBorder.Fixed => WindowMode.Fixed,
				WindowBorder.Hidden => WindowMode.NoFrame,
				WindowBorder.Resizable => WindowMode.Resizable,
				_ => throw new InvalidOperationException("unexpected window state"),
			};
			set => _window.WindowBorder = value switch
			{
				WindowMode.Fixed => WindowBorder.Fixed,
				WindowMode.NoFrame => WindowBorder.Hidden,
				WindowMode.Resizable => WindowBorder.Resizable,
				_ => throw new ArgumentException(null, nameof(value)),
			};
		}
		public IInputContext? _RawInputContext { get; private set; }

		public TextureFactory TextureFactory => _textureFactory ?? throw new InvalidOperationException("window is not loaded");

		public GL GL => _gl ?? throw new InvalidOperationException("window is not loaded");

		private int _scale = 1;
		private int _frameCount;
		private int _updateCount;
		private int _prevSecondUps;
		private int _prevSecondFps;
		private byte[] _screenshotBuffer = [];
		private VectorInt _size = (640, 480);
		private GL? _gl;
		private TextureFactory? _textureFactory;

		private Silk.NET.Windowing.IWindow _window;

		public Texture2D TakeScreenshot()
		{
			return TextureFactory.LoadFromImageSharpImage(TakeScreenshotAsImage());
		}

		public async Task SaveScreenshotAsync(string path, CancellationToken ct = default)
		{
			var img = TakeScreenshotAsImage();
			await img.SaveAsPngAsync(path, ct);
		}

		public void Run(WindowOptions opts)
		{
			var options = Silk.NET.Windowing.WindowOptions.Default;
			options.Position = new Vector2D<int>(opts.Location.X, opts.Location.Y);
			options.Size = new Vector2D<int>(opts.Size.X, opts.Size.Y) * opts.Scale;
			options.Title = opts.Title;
			options.WindowBorder = opts.Mode switch
			{
				WindowMode.Fixed => WindowBorder.Fixed,
				WindowMode.NoFrame => WindowBorder.Hidden,
				WindowMode.Resizable => WindowBorder.Resizable,
				_ => throw new ArgumentException(null, nameof(opts.Mode)),
			};
			options.WindowState = opts.IsFullScreen ? WindowState.Fullscreen : WindowState.Normal;
			options.FramesPerSecond = opts.TargetFps;
			options.UpdatesPerSecond = opts.TargetUps;
			options.VSync = opts.IsVsyncMode;

			_screenshotBuffer = new byte[options.Size.X * options.Size.Y * 4];

			_window = Window.Create(options);
			_window.Load += OnLoad;
			_window.Resize += OnResize;
			_window.FileDrop += OnFileDrop;
			_window.Render += OnRenderFrame;
			_window.Update += OnUpdateFrame;
			_window.Closing += OnUnload;
			_window.FocusChanged += v => IsFocused = v;
			_window.Run();
		}

		public void Exit()
		{
			_window.Close();
		}

		private unsafe Image TakeScreenshotAsImage()
		{
			fixed (byte* buffer = _screenshotBuffer)
			{
				_gl?.ReadPixels(0, 0, (uint)(ActualWidth * _scale), (uint)(ActualHeight * _scale), PixelFormat.Rgba, PixelType.UnsignedByte, buffer);
			}
			var img = Image.LoadPixelData<Rgba32>(_screenshotBuffer, ActualWidth * _scale, ActualHeight * _scale);
			img.Mutate(i => i.Flip(FlipMode.Vertical));
			return img;
		}

		private void OnLoad()
		{
			_gl = _window.CreateOpenGL();
			_RawInputContext = _window.CreateInput();
			_textureFactory = new OpenGLTextureFactory(_gl, app);
			UpdateWindowSize();

			Start?.Invoke();
		}

		private void OnResize(Vector2D<int> vec)
		{
			_gl?.Viewport(_window.FramebufferSize);
			Size = ActualSize;
			Resize?.Invoke();
		}

		private void OnFileDrop(string[] files)
		{
			FileDropped?.Invoke(new FileDroppedEventArgs(files));
		}

		private void OnRenderFrame(double delta)
		{
			if (_gl == null) return;
			// 画面の初期化
			_gl.ClearColor(app.BackgroundColor);
			_gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			if (app.Root != null) app.RenderElement(app.Root);
			CalculateFps();

			Render?.Invoke();

			TotalTime += (float)delta;
			TotalFrame++;
		}

		private void OnUpdateFrame(double delta)
		{
			var deltaTime = (float)delta;
			DeltaTime = deltaTime;

			CalculateUps();

			PreUpdate?.Invoke();
			if (app.Root != null) app.UpdateElement(app.Root);
			Update?.Invoke();
			PostUpdate?.Invoke();
		}

		private void OnUnload()
		{
			Destroy?.Invoke();
		}

		private void CalculateUps()
		{
			_updateCount++;
			if (Environment.TickCount - _prevSecondUps <= 1000) return;

			UpdatePerSeconds = _updateCount;
			_updateCount = 0;
			_prevSecondUps = Environment.TickCount;
		}

		private void CalculateFps()
		{
			_frameCount++;
			if (Environment.TickCount - _prevSecondFps <= 1000) return;

			FramePerSeconds = _frameCount;
			_frameCount = 0;
			_prevSecondFps = Environment.TickCount;
		}

		private void UpdateWindowSize()
		{
			_window.Size = new Vector2D<int>(Size.X, Size.Y) * _scale;
			_screenshotBuffer = new byte[_window.FramebufferSize.X * _window.FramebufferSize.Y * _scale * 4];
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
