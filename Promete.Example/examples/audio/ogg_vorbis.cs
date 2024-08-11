using System.Diagnostics;
using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.audio;

[Demo("/audio/ogg vorbis.demo", "BGM を再生します")]
public class OggVorbisExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
	private readonly AudioPlayer audio = new();
	private VorbisAudioSource bgm = new VorbisAudioSource("./assets/kagerou.ogg");

	public override void OnStart()
	{
		Window.Title = "Ogg Vorbis playback example";
		console.Print("Ogg Vorbis playback Example");

		Window.FileDropped += WindowOnFileDropped;
	}

	public override void OnUpdate()
	{
		console.Clear();
		console.Print($"""
		               Location: {audio.Time / 1000f:0.000} / {audio.Length / 1000f:0.000}
		               Location in Samples: {audio.TimeInSamples} / {audio.LengthInSamples}
		               Loaded: {bgm.LoadedSize} / {bgm.Samples}
		               [↑] Volume Up
		               [↓] Volume Down
		               [←] Pitch Down
		               [→] Pitch Up
		               PRESS ESC TO RETURN
		               """);

		if (keyboard.Escape.IsKeyUp)
			App.LoadScene<MainScene>();

		if (keyboard.Up.IsKeyDown)
			audio.Gain += 0.1f;

		if (keyboard.Down.IsKeyDown)
			audio.Gain -= 0.1f;

		if (keyboard.Left.IsKeyDown)
			audio.Pitch -= 0.1f;

		if (keyboard.Right.IsKeyDown)
			audio.Pitch += 0.1f;

		if (keyboard.Space.IsKeyDown)
		{
			if (audio.IsPlaying)
			{
				audio.Stop();
			}
			else
			{
				audio.Play(bgm, 0);
			}
		}
	}

	public override void OnDestroy()
	{
		audio.Stop();
		audio.Dispose();
		Window.FileDropped -= WindowOnFileDropped;
	}

	private void WindowOnFileDropped(FileDroppedEventArgs e)
	{
		var path = e.Path;
		if (!path.EndsWith(".ogg")) return;

		audio.Stop();
		bgm = new VorbisAudioSource(path);
		audio.Play(bgm, 0);
	}
}
