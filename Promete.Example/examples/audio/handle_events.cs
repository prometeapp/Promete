using Promete.Assets;
using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.audio;

[Demo("/audio/handle_events.demo", "AudioPlayerのイベント")]
public class HandleEventsExampleScene(Keyboard keyboard, ConsoleLayer console, AssetManager assets)
    : Scene
{
    private readonly AudioPlayer _audio = new();
    private IAudioSource _bgm = assets.LoadAudio("assets/amaebi.ogg", AssetScope.Scene);

    // ドロップされたファイルは AssetManager の管理外なので、この画面で所有して破棄する
    private VorbisAudioSource? _droppedBgm;

    public override void OnStart()
    {
        View.FileDropped += WindowOnFileDropped;
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
        _droppedBgm?.Dispose();
        View.FileDropped -= WindowOnFileDropped;
    }

    private void WindowOnFileDropped(FileDroppedEventArgs e)
    {
        var path = e.Path;
        if (!path.EndsWith(".ogg"))
            return;

        _audio.Stop();
        _droppedBgm?.Dispose();
        _droppedBgm = new VorbisAudioSource(path);
        _bgm = _droppedBgm;
        _audio.Play(_bgm, 0);
    }
}
