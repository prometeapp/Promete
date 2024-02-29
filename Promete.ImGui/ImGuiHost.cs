using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;
using Promete.Elements;
using Promete.Elements.Renderer;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Promete.ImGui;

public class ImGuiHost : ElementBase
{
	public Action RenderAction { get; set; }

	private ImFontPtr font;
	private nint fontData;

	private readonly ImGuiController controller;

	public ImGuiHost(Action renderAction)
	{
		RenderAction = renderAction;
		var window = PrometeApp.Current?.Window ?? throw new InvalidOperationException("Promete is not initialized.");
		var glWindow = window as OpenGLDesktopWindow ??
		               throw new InvalidOperationException("Promete.ImGui only supports OpenGL backend.");
		var silkWindowField = typeof(OpenGLDesktopWindow).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
		var silkWindow = silkWindowField?.GetValue(glWindow) as Silk.NET.Windowing.IWindow ??
		                 throw new InvalidOperationException("BUG: Failed to get native window.");
		controller = new ImGuiController(glWindow.GL, silkWindow, window._RawInputContext, ConfigureImGui);
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
		var fontBytes = memoryStream.ToArray();
		fontData = Marshal.AllocCoTaskMem(fontBytes.Length);
		Marshal.Copy(fontBytes, 0, fontData, fontBytes.Length);
		font = io.Fonts.AddFontFromMemoryTTF(fontData, fontBytes.Length, 18, config);
	}

	protected override void OnDestroy()
	{
		controller.Dispose();
	}

	public class ImGuiHostRenderer : ElementRendererBase
	{
		public override void Render(ElementBase element)
		{
			var host = element as ImGuiHost ?? throw new InvalidOperationException("The element is not ImguiHost.");
			var window = PrometeApp.Current!.Window!;
			host.controller.Update(window.DeltaTime);
			ImGuiNET.ImGui.GetIO().FontGlobalScale = window.Scale * window.PixelRatio;
			host.RenderAction();
			host.controller.Render();
		}
	}
}
