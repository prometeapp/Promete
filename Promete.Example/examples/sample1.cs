using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("/sample1", @"""Hello world!""")]
public class Sample1ExampleScene(ConsoleLayer console, PrometeApp app, Keyboard keyboard) : Scene
{
	public override void OnStart()
	{
		console.Print("Hello, world!");
		console.Print("Press [ESC] to exit");
	}

	public override void OnUpdate()
	{
		if (keyboard.Escape.IsKeyDown)
		{
			app.LoadScene<MainScene>();
		}
	}
}
