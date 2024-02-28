using ImGuiNET;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.ImGui;
using Promete.Input;
using Promete.Windowing;
using UI = ImGuiNET.ImGui;

namespace Promete.Example.examples;

[Demo("/imgui.demo", "ImGui example")]
public class ImGuiExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
	private ImGuiHost _imguiHost;

	private Sprite? ichigo;

	protected override Container Setup()
	{
		return [
			_imguiHost = new ImGuiHost(OnRender),
		];
	}

	public override void OnStart()
	{
		Window.Mode = WindowMode.Resizable;
	}

	public override void OnUpdate()
	{
		if (keyboard.Escape.IsKeyUp)
		{
			App.LoadScene<MainScene>();
		}
	}

	private void OnRender()
	{
		UI.Begin("ImGui Window");
		UI.Text("Hello, ImGui from Promete!");
		if (UI.Button($"{(ichigo == null ? "Show" : "Hide")} Ichigo"))
		{
			ToggleIchigo();
		}
		if (UI.Button("Back"))
		{
			App.LoadScene<MainScene>();
		}
	}

	private void ToggleIchigo()
	{
		if (ichigo == null)
		{
			var texture = App.Window.TextureFactory.Load("./assets/ichigo.png");
			ichigo = new Sprite(texture)
				.Location(16, 16)
				.Scale(4, 4);
			Root.Add(ichigo);
		}
		else
		{
			Root.Remove(ichigo);
			ichigo.Destroy();
			ichigo.Texture?.Dispose();
			ichigo = null;
		}
	}
}
