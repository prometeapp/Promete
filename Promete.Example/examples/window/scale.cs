using Promete.Example.Kernel;
using Promete.Input;
using Silk.NET.Input;

namespace Promete.Example.examples.window;

[Demo("window/scale.demo", "Scaleのテスト")]
public class WindowScaleDemoScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
	public override void OnUpdate()
	{
		console.Clear();

		console.Print($"Current Scale: {Window.Scale}");
		console.Print("[1]: Scale 1x");
		console.Print("[2]: Scale 2x");
		console.Print("[3]: Scale 4x");
		console.Print("[4]: Scale 8x");

		if (keyboard.Number1.IsKeyDown) Window.Scale = 1;
		if (keyboard.Number2.IsKeyDown) Window.Scale = 2;
		if (keyboard.Number3.IsKeyDown) Window.Scale = 4;
		if (keyboard.Number4.IsKeyDown) Window.Scale = 8;

		if (keyboard.Escape.IsKeyDown) App.LoadScene<MainScene>();
	}
}
