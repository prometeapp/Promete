using System.Drawing;
using Promete.Nodes;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("/sample2.demo", "簡単なお絵かきツール")]
public class Sample2ExampleScene(ConsoleLayer console, Mouse mouse, Keyboard keyboard) : Scene
{
	private VectorInt previousPosition;

	public override void OnStart()
	{
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print("PAINT EXAMPLE");
		console.Print("Mouse Left: Paint");
		console.Print("Mouse Right: Clear");
		console.Print("Keyboard [ESC]: Quit");
		console.Print($"\nobjects: {Root.Count}\n{Window.FramePerSeconds}fps");

		var position = mouse.Position;
		if (mouse[MouseButtonType.Right].IsButtonDown)
			Root.Clear();

		if (mouse[MouseButtonType.Left] && previousPosition != position)
			Root.Add(Shape.CreateLine(previousPosition, position, Color.White));

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		previousPosition = position;
	}
}
