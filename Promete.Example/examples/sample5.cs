using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples;


[Demo("/sample5", "10000スプライトを表示してFPSを計測します")]
public class BenchmarkScene(PrometeApp app, IWindow window, Keyboard keyboard) : Scene
{
	private ITexture strawberry;
	private bool initialized;
	private readonly Random rnd = new();

	public override async void OnStart()
	{
		strawberry = window.TextureFactory.Load("assets/ichigo.png");
		for (var i = 0; i < 10000; i++)
		{
			window.Title = $"Creating sprites {(int)((i + 1) / 10000f * 100)}%";
			Root.Add(new Sprite(strawberry, location: rnd.NextVector(window.Width - 16, window.Height - 16)));
			if (i % 1000 == 0)
				await Task.Delay(1);
		}

		initialized = true;
	}

	public override void OnUpdate()
	{
		if (!initialized) return;
		window.Title = $"{window.FramePerSeconds} FPS";

		if (keyboard.Escape.IsKeyUp)
			app.LoadScene<MainScene>();
	}

	public override void OnDestroy()
	{
		strawberry.Dispose();
	}
}
