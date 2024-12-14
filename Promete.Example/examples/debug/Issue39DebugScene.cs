using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.debug;

[Demo("/debug/issue39", "Issue39: Debug Scene")]
public class Issue39DebugScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private readonly IAudioSource _bgm = new VorbisAudioSource("./assets/GB-Action-C02-2.ogg");
    private readonly IAudioSource _wav = new WaveAudioSource("./assets/lineclear.wav");
    private readonly AudioPlayer _player = new();
    private int _sfxPlayedCount;

    public override void OnUpdate()
    {
        console.Clear();
        console.Print("Issue39: Debug Scene");
        console.Print($"SFX Played: {_sfxPlayedCount}");
        console.Print($"Uptime: {GetUptime()}");
        console.Print($"Time: {_player.Time} / {_player.Length}");
        console.Print($"Samples: {_player.TimeInSamples} / {_player.LengthInSamples}");
        console.Print("[Enter]: Toggle BGM");
        console.Print("[Space]: Play SFX once");
        console.Print("[B]: Play SFX while pressing");

        if (keyboard.B)
        {
            PlaySfx();
        }

        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
            return;
        }
        if (keyboard.Enter.IsKeyDown)
        {
            if (_player.IsPlaying)
            {
                _player.Stop();
            }
            else
            {
                _player.Play(_bgm, 0);
            }
        }
        if (keyboard.Space.IsKeyDown)
        {
            PlaySfx();
        }
    }

    public override void OnDestroy()
    {
        _player.Stop();
        _player.Dispose();
    }

    private void PlaySfx()
    {
        _player.PlayOneShot(_wav, 0.1f, 2);
        _sfxPlayedCount++;
    }

    private string GetUptime()
    {
        var t = Window.TotalTime;
        var h = (int)(t / 60 / 60);
        var m = (int)(t / 60 % 60);
        var s = (int)(t % 60);

        return $"{h:D2}:{m:D2}:{s:D2}";
    }
}
