using Promete;
using Promete.Audio;
using Promete.Elements;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;
using Color = System.Drawing.Color;

namespace Promete.Example.Android;

public class MainScene : Scene
{
	private Tilemap map;

	private readonly PrometeApp app;
	private readonly IWindow window;
	private readonly Keyboard keyboard;
	private readonly ConsoleLayer console;
	private readonly Mouse mouse;
	private readonly ITile tile;
	private readonly AudioPlayer player;
	private readonly VorbisAudioSource sound;
	private readonly WaveAudioSource sfx1;

	public MainScene(PrometeApp app, IWindow window, Keyboard keyboard, Mouse mouse, ConsoleLayer console)
	{
		this.app = app;
		this.window = window;
		this.keyboard = keyboard;
		this.mouse = mouse;
		this.console = console;

		player = new AudioPlayer();
		sound = new VorbisAudioSource(MainActivity.CurrentAssets!.Open("assets/kagerou.ogg"));

		tile = new Tile(window.TextureFactory.CreateSolid(Color.SlateGray, (16, 16)));
	}

	public override Container Setup()
	{
		map = new Tilemap((16, 16));

		var tw = window.Width / 16;
		var th = window.Height / 16;

		for (var y = 0; y < th; y++)
		{
			for (var x = 0; x < tw; x++)
			{
				map[x, y] = Random.Shared.Next(100) < 25 ? tile : null;
			}
		}

		return new Container
		{
			map,
		};
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print("Hello, Promete!");
		console.Print($"FPS: {window.FramePerSeconds}");
		console.Print($"UPS: {window.UpdatePerSeconds}");

		if (mouse[MouseButtonType.Left].IsButtonDown)
		{
			if (player.IsPlaying) player.Stop();
			else player.Play(sound);
		}
	}
}
