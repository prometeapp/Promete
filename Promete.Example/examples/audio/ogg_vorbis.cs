using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.audio;

[Demo("/audio/ogg vorbis.demo", "BGM を再生します")]
public class OggVorbisExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly AudioPlayer _audio = new();
    private VorbisAudioSource _bgm = new("./assets/GB-Action-C02-2.ogg");

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
                       Location: {_audio.Time / 1000f:0.000} / {_audio.Length / 1000f:0.000}
                       Location in Samples: {_audio.TimeInSamples} / {_audio.LengthInSamples}
                       Loaded: {_bgm.LoadedSize} / {_bgm.Samples}
                       Volume: {_audio.Gain}
                       Pitch: {_audio.Pitch}
                       Pan: {_audio.Pan}
                       Is Playing: {_audio.IsPlaying}
                       Is Pausing: {_audio.IsPausing}
                       [↑] Volume Up
                       [↓] Volume Down
                       [←] Pitch Down
                       [→] Pitch Up
                       [A] Pan Left
                       [D] Pan Right
                       PRESS ESC TO RETURN
                       """);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Up.IsKeyDown)
            _audio.Gain += 0.1f;

        if (keyboard.Down.IsKeyDown)
            _audio.Gain -= 0.1f;

        if (keyboard.Left.IsKeyDown)
            _audio.Pitch -= 0.1f;

        if (keyboard.Right.IsKeyDown)
            _audio.Pitch += 0.1f;

        if (keyboard.A.IsKeyDown)
            _audio.Pan = MathF.Max(-1, (int)((_audio.Pan - 0.1f) * 10) / 10f);

        if (keyboard.D.IsKeyDown)
            _audio.Pan = MathF.Min(1, (int)((_audio.Pan + 0.1f) * 10) / 10f);

        if (keyboard.Space.IsKeyDown)
        {
            if (_audio is { IsPlaying: true, IsPausing: false })
                _audio.Pause();
            else if (_audio.IsPausing)
                _audio.Resume();
            else
                _audio.Play(_bgm, 0);
        }
    }

    public override void OnDestroy()
    {
        _audio.Stop();
        _audio.Dispose();
        _bgm.Dispose();
        Window.FileDropped -= WindowOnFileDropped;
    }

    private void WindowOnFileDropped(FileDroppedEventArgs e)
    {
        var path = e.Path;
        if (!path.EndsWith(".ogg")) return;

        _audio.Stop();
        _bgm = new VorbisAudioSource(path);
        _audio.Play(_bgm, 0);
    }
}
