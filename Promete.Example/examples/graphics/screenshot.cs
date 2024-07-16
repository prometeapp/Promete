using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples.graphics;

[Demo("graphics/screenshot.demo", "スクリーンショットの撮影")]
public class ScreenshotTest(Mouse mouse, Keyboard keyboard) : Scene
{
	private Texture2D? _texture;

	private readonly Sprite _sprite = new Sprite();

	public override void OnStart()
	{
		Window.Size = (320, 240);
		Window.Scale = 2;

		for (var i = 0; i < 100; i++)
		{
			var loc = Random.Shared.NextVectorInt(Window.X, Window.Y);
			var size = Random.Shared.NextVectorInt(64, 64) + (8, 8);
			Root.Add(Shape.CreateRect(loc, loc + size, Random.Shared.NextColor()));
		}

		Root.Add(_sprite);
	}

	public override void OnUpdate()
	{
		if (keyboard.Space.IsKeyUp)
		{
			_texture?.Dispose();
			_texture = Window.TakeScreenshot();
			_sprite.Texture = _texture;
			_sprite.Location = (0, 0);
		}

		_sprite.Location = mouse.Position;
		_sprite.Scale = (0.5f, 0.5f);

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}

	public override void OnDestroy()
	{
		Window.Size = (640, 480);
		Window.Scale = 1;
	}
}
