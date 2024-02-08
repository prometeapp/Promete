using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("sample6.demo", "スプライトの回転テスト")]
public class SpriteRotateTestScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse) : Scene
{
	private ITexture tIchigo;
	private Sprite sprite;
	private Container wrapper;
	private float angle = 0;

	protected override Container Setup()
	{
		tIchigo = Window.TextureFactory.CreateSolid(Color.Chartreuse, (32, 32));
		sprite = new Sprite(tIchigo, location: (0, 0));

		return
		[
			wrapper = new Container(scale: (2, 2))
			{
				Shape.CreateLine(-32, 0, 32, 0, Color.Red),
				Shape.CreateLine(0, -32, 0, 32, Color.Blue),
				sprite,
			},
		];
	}

	public override void OnUpdate()
	{
		angle += Window.DeltaTime * 90;
		if (angle > 360) angle -= 360;
		sprite.Angle = angle;

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		if (keyboard.Space.IsKeyDown)
		{
			wrapper.Scale = wrapper.Scale.X == 1 ? (2, 2) : (1, 1);
		}

		wrapper.Location = mouse.Position;
	}
}
