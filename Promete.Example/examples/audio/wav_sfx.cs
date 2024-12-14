using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.audio;

[Demo("/audio/wav sfx.demo", "短い効果音をいくつか再生します")]
public class WavExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly AudioPlayer _player = new();
    private readonly WaveAudioSource _sfx = new("assets/lineclear.wav");

    public override void OnStart()
    {
        console.Print("SFX Example\n");
        console.Print("[SPACE]: Replay");
        console.Print("[F1]: Replay with x2 pitch");
        console.Print("[F2]: Replay with x0.5 pitch");
        console.Print("[F3]: Replay with x0.5 gain");
        console.Print("[F4]: Replay with x0 gain");
        console.Print("[F5]: Replay with -1 pan");
        console.Print("[F6]: Replay with -0.5 pan");
        console.Print("[F7]: Replay with 0.5 pan");
        console.Print("[F8]: Replay with 1 pan");

        console.Print("[ESC]: Quit");
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyUp)
            _player.PlayOneShot(_sfx);

        if (keyboard.F1.IsKeyUp)
            _player.PlayOneShot(_sfx, pitch: 2);

        if (keyboard.F2.IsKeyUp)
            _player.PlayOneShot(_sfx, pitch: 0.5f);

        if (keyboard.F3.IsKeyUp)
            _player.PlayOneShot(_sfx, gain: 0.5f);

        if (keyboard.F4.IsKeyUp)
            _player.PlayOneShot(_sfx, gain: 0);

        if (keyboard.F5.IsKeyUp)
            _player.PlayOneShot(_sfx, pan: -1);

        if (keyboard.F6.IsKeyUp)
            _player.PlayOneShot(_sfx, pan: -0.5f);

        if (keyboard.F7.IsKeyUp)
            _player.PlayOneShot(_sfx, pan: 0.5f);

        if (keyboard.F8.IsKeyUp)
            _player.PlayOneShot(_sfx, pan: 1);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _player.Dispose();
    }
}
