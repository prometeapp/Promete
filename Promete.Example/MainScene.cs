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
		sound = new VorbisAudioSource("assets/kagerou.ogg");
		sfx1 = new WaveAudioSource("assets/lineclear.wav");

		tile = new Tile(window.TextureFactory.CreateSolid(Color.Red, (16, 16)));
	}

	public override Container Setup()
	{
		map = new Tilemap((16, 16));
		string a = "";
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
		var pan = (mouse.Position.X - window.Size.X / 2) / (window.Size.X / 2f);
		console.Print($"pan: {pan}");

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

		if (mouse[MouseButtonType.Right].IsButtonDown)
		{
			player.PlayOneShotAsync(sfx1, mouse.Position.Y / (float)window.Height, 2f, pan);
		}

		if (keyboard.Number1.IsKeyDown)
		{
			TestAwaitingTaskAsync();
		}
	}

	private async void TestAwaitingTaskAsync()
	{
		player.PlayOneShot(sfx1);
		await Task.Delay(1000);
		player.PlayOneShot(sfx1);
		await Task.Delay(1000);
		player.PlayOneShot(sfx1);
		await Task.Delay(1000);
	}
}
