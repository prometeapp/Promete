using Promete.Graphics;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using IWindow = Promete.Windowing.IWindow;

namespace Promete.Android;

public sealed class OpenGLAndroidWindow : IWindow
{
	public VectorInt Location
	{
		get => (0, 0);
		set { /* do nothing */ }
	}
	public VectorInt Size
	{
		get => ActualSize;
		set { /* do nothing */ }
	}
	public VectorInt ActualSize => (view.FramebufferSize.X, view.FramebufferSize.Y);
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

	public int Scale
	{
		get => 1;
		set { /* do nothing */ }
	}

	public bool IsVisible
	{
		get => true;
		set { /* do nothing */ }
	}
	public bool IsFocused { get; private set; }
	public bool IsFullScreen
	{
		get => false;
		set { /* do nothing */ }
	}
	public float TotalTime { get; private set; }
	public float DeltaTime { get; private set; }
	public long FramePerSeconds { get; private set;}
	public long UpdatePerSeconds { get; private set;}
	public long TotalFrame { get; private set; }
	public int RefreshRate => 60;
	public float PixelRatio => view.FramebufferSize.X / view.Size.X;

	public string Title
	{
		get => "";
		set { /* do nothing */ }
	}

	public WindowMode Mode
	{
		get => WindowMode.Fixed;
		set { /* do nothing */ }
	}
	public IInputContext? _RawInputContext { get; private set; }

	public TextureFactory TextureFactory => textureFactory ?? throw new InvalidOperationException("window is not loaded");

	private int frameCount;
	private int updateCount;
	private int prevSecondUps;
	private int prevSecondFps;
	private byte[] screenshotBuffer = Array.Empty<byte>();
	private GL? gl;
	private TextureFactory? textureFactory;

	private readonly PrometeApp app;
	private readonly Silk.NET.Windowing.IView view;

	public OpenGLAndroidWindow(PrometeApp app)
	{
		this.app = app;
		view = Window.GetView(ViewOptions.Default with
		{
			API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0)),
		});

		view.Load += OnLoad;
		view.Update += OnUpdateFrame;
		view.Render += OnRenderFrame;
		view.Resize += OnResize;
		view.Closing += OnUnload;
		view.FocusChanged += v => IsFocused = v;
	}

	public void Run()
	{
		view.Run();
		view.Dispose();
	}

	public void Exit()
	{
		view.Close();
	}

	public ITexture TakeScreenshot()
	{
		throw new NotImplementedException();
	}

	private void OnLoad()
	{
		gl = view.CreateOpenGL();
		_RawInputContext = view.CreateInput();
		textureFactory = new OpenGLTextureFactory(gl);

		Start?.Invoke();
	}

	private void OnResize(Vector2D<int> obj)
	{
		gl?.Viewport(view.FramebufferSize);

		Resize?.Invoke();
	}

	private void OnRenderFrame(double obj)
	{
		if (gl == null) return;

		gl.ClearColor(app.BackgroundColor);
		gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		if (app.Root != null) app.RenderElement(app.Root);
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

	public event Action? Start;
	public event Action? Update;
	public event Action? Render;
	public event Action? Destroy;
	public event Action? PreUpdate;
	public event Action? PostUpdate;
	public event Action<FileDroppedEventArgs>? FileDropped;
	public event Action? Resize;
}
