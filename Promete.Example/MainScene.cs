using Promete;
using Promete.Elements;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;
using Color = System.Drawing.Color;

namespace Promete.Example;

public class MainScene : Scene
{
	private ITexture texture;
	private Sprite[] sprites;

	private readonly PrometeApp app;
	private readonly IWindow window;
	private readonly Keyboard keyboard;
	private readonly ConsoleLayer console;
	private readonly Mouse mouse;

	public MainScene(PrometeApp app, IWindow window, Keyboard keyboard, Mouse mouse, ConsoleLayer console)
	{
		this.app = app;
		this.window = window;
		this.keyboard = keyboard;
		this.mouse = mouse;
		this.console = console;

		texture = window.TextureFactory.CreateSolid(Color.White, (8, 8));
		for (var i = 0; i < 10000; i++)
		{
			var sprite = new Sprite(texture)
			{
				TintColor = Random.Shared.NextColor(),
				Location = (-100, -100),
				Scale = Random.Shared.NextVector(40, 40) / 10
			};

			Root.Add(sprite);
		}
	}

	public override void OnStart()
	{
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print("Hello, Promete!");
		console.Print($"FPS: {window.FramePerSeconds}");
		console.Print($"UPS: {window.UpdatePerSeconds}");

		if (keyboard.Escape.IsKeyDown)
		{
			app.Exit();
		}
	}
}
