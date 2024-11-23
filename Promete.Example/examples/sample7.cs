using System.Drawing;
using Promete.Nodes;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("sample7.demo", "スプライトの回転テスト2")]
public class SpriteRotateTest2Scene : Scene
{
	private Texture2D tParent, tChild;
	private Sprite spriteParent, spriteChild;
	private Container wrapper;
	private float angle = 0;
	private int mode = 0;
	private bool isPlaying = true;

	private ConsoleLayer _console;
	private Keyboard _keyboard;

	private string ModeText => mode switch
	{
		0 => "Rotate Parent",
		1 => "Rotate Child",
		2 => "Rotate Both",
		_ => "Unknown",
	};

	public SpriteRotateTest2Scene(ConsoleLayer console, Keyboard keyboard)
	{
		tParent = Window.TextureFactory.CreateSolid(Color.DarkBlue, (160, 120));
		tChild = Window.TextureFactory.CreateSolid(Color.Chocolate, (32, 32));

		_console = console;
		_keyboard = keyboard;

		this.Root = [
			wrapper = new Container()
				.Scale((2, 2))
				.Location(320, 240)
				.Children(
					spriteParent = new Sprite(tParent).Location(0, 0),
					spriteChild = new Sprite(tChild).Location(32, 32)
				),
		];
	}

	public override void OnUpdate()
	{
		_console.Clear();
		_console.Print("Angle: " + angle);
		_console.Print("Mode: " + ModeText);
		_console.Print("[1]: Change Mode");
		_console.Print("[SPACE]: Toggle Rotation");
		_console.Print("[ESC]: return");

		if (isPlaying)
		{
			angle += Window.DeltaTime * 90;
			if (angle > 360) angle -= 360;
		}

		switch (mode)
		{
			case 0:
				wrapper.Angle = angle;
				break;
			case 1:
				spriteChild.Angle = angle;
				break;
			case 2:
				wrapper.Angle = angle;
				spriteChild.Angle = angle;
				break;
		}

		if (_keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		if (_keyboard.Number1.IsKeyDown)
		{
			wrapper.Angle = spriteChild.Angle = 0;
			mode = (mode + 1) % 3;
		}

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
}
