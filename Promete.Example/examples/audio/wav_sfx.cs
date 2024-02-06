using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.audio;

[Demo("/audio/wav sfx", "短い効果音をいくつか再生します")]
public class WavExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
	private readonly AudioPlayer player = new();
	private readonly WaveAudioSource sfx = new("assets/lineclear.wav");

	public override void OnStart()
	{
		console.Print("SFX Example\n");
		console.Print("[SPACE]: Replay");
		console.Print("[ESC]: Quit");
	}

	public override void OnUpdate()
	{
		if (keyboard.Space.IsKeyUp)
			player.PlayOneShotAsync(sfx);

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();
	}
}
