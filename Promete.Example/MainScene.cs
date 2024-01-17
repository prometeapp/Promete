using System.Drawing;
using Promete;
using Promete.Elements;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example;

public class MainScene : Scene
{
	private ITexture texture;
	private Sprite sprite;

	private readonly PrometeApp app;
	private readonly Keyboard keyboard;
	private readonly ConsoleLayer console;
	private readonly Mouse mouse;

	public MainScene(PrometeApp app, Keyboard keyboard, Mouse mouse, ConsoleLayer console, IWindow window)
	{
		this.app = app;
		this.keyboard = keyboard;
		this.mouse = mouse;
		this.console = console;

		texture = window.TextureFactory.CreateSolid(Color.Red, (16, 16));
		sprite = new Sprite(texture);

		Root.Add(sprite);
	}

	public override void OnStart()
	{
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print("Hello, Promete!");
		console.Print($"Sprite: {sprite.Location}");

		if (keyboard.Escape.IsKeyDown)
		{
			app.Exit();
		}

		sprite.Location = mouse.Position;
		sprite.Scale += mouse.Scroll;
	}
}
