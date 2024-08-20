using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;

namespace Promete.Example.examples;


[Demo("/sample5.demo", "10000スプライトを表示してFPSを計測します")]
public class BenchmarkScene(Keyboard keyboard) : Scene
{
	private Texture2D strawberry;
	private bool initialized;
	private readonly Random rnd = new();

	public override async void OnStart()
	{
		strawberry = Window.TextureFactory.Load("assets/ichigo.png");
		App.NextFrame(Init);
		Window.Title = "Initializing in background thread...";
	}

	public override void OnUpdate()
	{
		if (!initialized) return;
		Window.Title = $"{Window.FramePerSeconds} FPS";

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}

	public override void OnDestroy()
	{
		strawberry.Dispose();
	}

	private async void Init()
	{
		await Task.Factory.StartNew(() =>
		{
			for (var i = 0; i < 10000; i++)
			{
				var sprite = new Sprite(strawberry)
					.Location(rnd.NextVector(Window.Width, Window.Height));
				App.NextFrame(() => Root.Add(sprite));
			}

			initialized = true;
		});
	}
}
