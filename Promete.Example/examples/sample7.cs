using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("sample7.demo", "スプライトの回転テスト2")]
public class SpriteRotateTest2Scene(ConsoleLayer console, Keyboard keyboard, Mouse mouse) : Scene
{
	private ITexture tParent, tChild;
	private Sprite spriteParent, spriteChild;
	private Container wrapper;
	private float angle = 0;
	private int mode = 0;
	private bool isPlaying = true;

	private string ModeText => mode switch
	{
		0 => "Rotate Parent",
		1 => "Rotate Child",
		2 => "Rotate Both",
		_ => "Unknown",
	};

	protected override Container Setup()
	{
		tParent = Window.TextureFactory.CreateSolid(Color.DarkBlue, (160, 120));
		tChild = Window.TextureFactory.CreateSolid(Color.Chocolate, (32, 32));
		spriteParent = new Sprite(tParent, location: (0, 0));
		spriteChild = new Sprite(tChild, location: (32, 32));

		return
		[
			wrapper = new Container(scale: (1, 1), location: (320, 240))
			{
				spriteParent, spriteChild,
			},
		];
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print("Angle: " + angle);
		console.Print("Mode: " + ModeText);
		console.Print("[1]: Change Mode");
		console.Print("[SPACE]: Toggle Rotation");
		console.Print("[ESC]: return");

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

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		if (keyboard.Number1.IsKeyDown)
		{
			wrapper.Angle = spriteChild.Angle = 0;
			mode = (mode + 1) % 3;
		}

		if (keyboard.Space.IsKeyDown)
		{
			isPlaying ^= true;
		}

		if (keyboard.Left.IsKeyDown)
		{
			angle = (int)(angle - 1);
			if (angle < 0) angle = 360;
		}

		if (keyboard.Right.IsKeyDown)
		{
			angle = (int)(angle + 1);
			if (angle > 360) angle = 0;
		}
	}
}
