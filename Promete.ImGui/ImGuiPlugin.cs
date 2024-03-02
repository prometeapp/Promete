using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;
using Promete.Elements;
using Promete.Elements.Renderer;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Promete.ImGui;

/// <summary>
/// ImGUI との連携を提供する Promete プラグインです。このクラスは継承できません。
/// 本プラグインは、Prometeが OpenGL デスクトップバックエンドである場合にのみ使用できます。
/// </summary>
public sealed class ImGuiPlugin
{
	/// <summary>
	/// ウィンドウのスケーリング値と同期するかどうかを取得または設定します。
	/// </summary>
	public bool IsSyncronizeWithWindowScaling { get; set; }

	private ImFontPtr font;
	private nint fontData;

	private readonly ImGuiController controller;
	private readonly IWindow window;
	private readonly PrometeApp app;

	/// <summary>
	/// ImGuiPlugin の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="app"></param>
	/// <param name="window"></param>
	/// <exception cref="NotSupportedException">OpenGL デスクトップバックエンドでない場合スローされます。</exception>
	public ImGuiPlugin(PrometeApp app, IWindow window)
	{
		this.window = window;
		this.app = app;
		// PrometeがOpenGLバックエンドでなければ例外をスローする
		if (window is not OpenGLDesktopWindow glWindow)
		{
			throw new NotSupportedException("Promete.ImGui only supports OpenGL backend.");
		}

		var nativeWindow = GetNativeWindow();
		controller = new ImGuiController(glWindow.GL, nativeWindow, window._RawInputContext, ConfigureImGui);

		this.window.Destroy += OnWindowDestroy;
		this.window.Render += OnWindowRender;
	}

	private Silk.NET.Windowing.IWindow GetNativeWindow()
	{
		var field = typeof(OpenGLDesktopWindow).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
		return field?.GetValue(window as OpenGLDesktopWindow) as Silk.NET.Windowing.IWindow ??
		       throw new InvalidOperationException("BUG: Failed to get native window.");
	}

	private unsafe void ConfigureImGui()
	{
		var io = ImGuiNET.ImGui.GetIO();
		io.NativePtr->IniFilename = null;

		var config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig())
		{
			GlyphRanges = io.Fonts.GetGlyphRangesJapanese(),
			OversampleH = 2,
			OversampleV = 1,
			RasterizerMultiply = 1.2f,
			PixelSnapH = true,
			FontDataOwnedByAtlas = false,
		};

		using var fontStream = typeof(PrometeApp).Assembly.GetManifestResourceStream("Promete.Resources.font.ttf") ?? throw new InvalidOperationException("Failed to load font.");
		using var memoryStream = new MemoryStream();
		fontStream.CopyTo(memoryStream);
		var bytes = memoryStream.ToArray();
		fontData = Marshal.AllocCoTaskMem(bytes.Length);
		Marshal.Copy(bytes, 0, fontData, bytes.Length);
		font = io.Fonts.AddFontFromMemoryTTF(fontData, bytes.Length, 18, config);
	}

	private void OnWindowRender()
	{
		controller.Update(window.DeltaTime);
		if (IsSyncronizeWithWindowScaling)
		{
			ImGuiNET.ImGui.GetIO().FontGlobalScale = window.Scale * window.PixelRatio;
		}
		Render?.Invoke();
		controller.Render();
	}

	private void OnWindowDestroy()
	{
		controller.Dispose();
		Marshal.FreeCoTaskMem(fontData);
		font.Destroy();
	}

	public event Action? Render;
}
