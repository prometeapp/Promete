using Promete;
using Promete.Audio;
using Promete.Elements;
using Promete.Graphics;
using Promete.Input;
using Promete.Windowing;
using Color = System.Drawing.Color;

namespace Promete.Example;

public class MainScene : Scene
{
	private readonly PrometeApp app;
	private readonly IWindow window;
	private readonly Keyboard keyboard;
	private readonly ConsoleLayer console;
	private readonly Tilemap map;
	private readonly Mouse mouse;
	private readonly ITile tile;
	private AudioPlayer player;
	private VorbisAudioSource sound;

	public MainScene(PrometeApp app, IWindow window, Keyboard keyboard, Mouse mouse, ConsoleLayer console)
	{
		this.app = app;
		this.window = window;
		this.keyboard = keyboard;
		this.mouse = mouse;
		this.console = console;

		Root.Add(map = new Tilemap((16, 16)));
		tile = new Tile(window.TextureFactory.CreateSolid(Color.Red, (16, 16)));
	}

	public override void OnStart()
	{
		player = new AudioPlayer();
		sound = new VorbisAudioSource("assets/kagerou.ogg");
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

		if (mouse[MouseButtonType.Left])
		{
			var (x, y) = mouse.Position / 16;
			map[x, y] = tile;
		}

		if (keyboard.Space.IsKeyDown)
		{
			if (player.IsPlaying) player.Stop();
			else player.Play(sound);
		}
	}
}
