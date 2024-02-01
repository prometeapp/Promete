using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("/sample2", "簡単なお絵かきツール")]
public class Sample2ExampleScene(PrometeApp app, ConsoleLayer console, Mouse mouse, Keyboard keyboard) : Scene
{
	private VectorInt previousPosition;

	public override void OnStart()
	{
		console.Print("PAINT EXAMPLE");
		console.Print("Mouse Left: Paint");
		console.Print("Mouse Right: Clear");
		console.Print("Keyboard [ESC]: Quit");
	}

	public override void OnUpdate()
	{
		var position = mouse.Position;
		if (mouse[MouseButtonType.Right].IsButtonDown)
			Root.Clear();

		if (mouse[MouseButtonType.Left])
			Root.Add(Shape.CreateLine(previousPosition, position, Color.White));

		if (keyboard.Escape.IsKeyUp)
			app.LoadScene<MainScene>();

		previousPosition = position;
	}
}
