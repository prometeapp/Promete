using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/container", "Elementをコンテナーにいくつか追加する例")]
public class ContainerExampleScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse) : Scene
{
	private ITexture ichigo;
	private readonly Container container = new();

	public override void OnStart()
	{
		ichigo = Window.TextureFactory.Load("assets/ichigo.png");
		Root.Add(container);

		var canvas = new Container(location: (400, 200));

		var random = new Random(300);

		VectorInt Rnd() => random.NextVectorInt(256, 256);

		Parallel.For(0L, 120, (_) =>
		{
			var (v1, v2, v3) = (Rnd(), Rnd(), Rnd());
			switch (random.Next(4))
			{
				case 0:
					canvas.Add(Shape.CreateLine(v1, v2, random.NextColor()));
					break;
				case 1:
					canvas.Add(Shape.CreateRect(v1, v2, random.NextColor(), random.Next(4), random.NextColor()));
					break;
				case 2:
					canvas.Add(Shape.CreatePixel(v1, random.NextColor()));
					break;
				case 3:
					canvas.Add(Shape.CreateTriangle(v1, v2, v3, random.NextColor(), random.Next(4),
						random.NextColor()));
					break;
			}
		});

		container.Add(new Text("O", font: Font.GetDefault(32), color: Color.White));

		container.Add(canvas);

		Parallel.For(0L, 8, (_) =>
		{
			container.Add(new Sprite(ichigo)
			{
				Location = random.NextVector(Window.Width, Window.Height),
				Scale = Vector.One + random.NextVectorFloat() * 7,
				TintColor = random.NextColor(),
			});
		});

		console.Print("Scroll to move");
		console.Print("Press ↑ to scale up");
		console.Print("Press ↓ to scale down");
		console.Print("Press ESC to return");
	}

	public override void OnUpdate()
	{
		if (keyboard.Up) container.Scale += Vector.One * 0.25f * Window.DeltaTime;
		if (keyboard.Down) container.Scale -= Vector.One * 0.25f * Window.DeltaTime;
		container.Location += mouse.Scroll * (-1, 1);
		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}

	public override void OnDestroy()
	{
		ichigo.Dispose();
	}
}
