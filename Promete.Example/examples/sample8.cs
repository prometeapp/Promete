using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("sample8.demo", "スプライトの回転テスト3")]
public class SpriteRotateTest3Scene : Scene
{
	private ITexture tIchigo;
	private List<Container> allIchigos = [];
	private float angle = 0;
	private bool isPlaying = true;

	private ConsoleLayer _console;
	private Keyboard _keyboard;

	public SpriteRotateTest3Scene(ConsoleLayer console, Keyboard keyboard)
	{
		_console = console;
		_keyboard = keyboard;

		tIchigo = Window.TextureFactory.Load("assets/ichigo.png");

		var parent = CreateIchigo(Window.Size / 2);
		var root = parent;
		allIchigos.Add(parent);

		for (var i = 1; i < 16; i++)
		{
			var child = CreateIchigo((32 + i * 4, 0));

			parent.Add(child);
			allIchigos.Add(child);
			parent = child;
		}

		Root = [root];
	}

	public override void OnUpdate()
	{
		_console.Clear();
		_console.Print("Angle: " + angle);
		_console.Print("[SPACE]: Toggle Rotation");
		_console.Print("[ESC]: return");

		if (isPlaying)
		{
			angle += Window.DeltaTime * 30;
			if (angle > 360) angle -= 360;
		}

		allIchigos.ForEach(i => i.Angle = angle);

		if (_keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		if (_keyboard.Space.IsKeyDown)
		{
			isPlaying ^= true;
		}

		if (_keyboard.Left.IsKeyDown)
		{
			angle = (int)(angle - 1);
			if (angle < 0) angle = 360;
		}

		if (_keyboard.Right.IsKeyDown)
		{
			angle = (int)(angle + 1);
			if (angle > 360) angle = 0;
		}
	}

	private Container CreateIchigo(Vector location)
	{
		return new Container()
			.Location(location)
			.Children(
				new Sprite(tIchigo)
			);
	}
}
