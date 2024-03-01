using ImGuiNET;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.ImGui;
using Promete.Input;
using Promete.Windowing;
using UI = ImGuiNET.ImGui;

namespace Promete.Example.examples;

[Demo("/plugins/imgui.demo", "ImGui example")]
public class ImGuiExampleScene(Keyboard keyboard, ImGuiPlugin imgui) : Scene
{
	private Sprite? ichigo;

	public override void OnStart()
	{
		Window.Mode = WindowMode.Resizable;
		imgui.OnRender = OnRender;
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
		UI.End();
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
