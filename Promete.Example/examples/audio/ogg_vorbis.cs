using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.audio;

[Demo("/audio/ogg vorbis", "BGM を再生します")]
public class OggVorbisExampleScene(PrometeApp app, IWindow window, Keyboard keyboard, ConsoleLayer console) : Scene
{
	private readonly AudioPlayer audio = new();
	private IAudioSource bgm = new VorbisAudioSource("./assets/kagerou.ogg");

	public override void OnStart()
	{
		window.Title = "Ogg Vorbis playback example";
		console.Print("Ogg Vorbis playback Example");

		window.FileDropped += WindowOnFileDropped;
	}

	public override void OnUpdate()
	{
		console.Cursor += VectorInt.Up * 2;
		console.Print($"""
		               Location: {audio.Time / 1000f:0.000} / {audio.Length / 1000f:0.000}
		               [↑] Volume Up
		               [↓] Volume Down
		               [←] Pitch Down
		               [→] Pitch Up
		               PRESS ESC TO RETURN
		               """);

		if (keyboard.Escape.IsKeyUp)
			app.LoadScene<MainScene>();

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
				audio.Stop();
			else
				audio.Play(bgm);
		}
	}

	public override void OnDestroy()
	{
		audio.Stop();
		audio.Dispose();
		window.FileDropped -= WindowOnFileDropped;
	}

	private void WindowOnFileDropped(FileDroppedEventArgs e)
	{
		var path = e.Path;
		if (!path.EndsWith(".ogg")) return;

		audio.Stop();
		bgm = new VorbisAudioSource(path);
		audio.Play(bgm);
	}
}
