using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.audio;

[Demo("/audio/handle_events.demo", "AudioPlayerのイベント")]
public class HandleEventsExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly AudioPlayer _audio = new();
    private VorbisAudioSource _bgm = new("./assets/GB-Action-C02-2.ogg");

    public override void OnStart()
    {
        Window.FileDropped += WindowOnFileDropped;
        console.Clear();
        console.Print($"PRESS SPACE TO PLAY/STOP");
        console.Print($"PRESS ESC TO RETURN");
        console.Print($"DROP .ogg FILES TO CHANGE BGM");
        _audio.StartPlaying += (_, _) => console.Print($"BGM STARTED");
        _audio.StopPlaying += (_, _) => console.Print($"BGM STOPPED");
        _audio.Loop += (_, _) => console.Print($"BGM LOOP");
        _audio.FinishPlaying += (_, _) => console.Print($"BGM FINISHED");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Space.IsKeyDown)
        {
            if (_audio.IsPlaying)
                _audio.Stop(0.5f);
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
