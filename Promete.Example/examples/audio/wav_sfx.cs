using Promete.Audio;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.audio;

[Demo("/audio/wav sfx.demo", "短い効果音をいくつか再生します")]
public class WavExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly AudioPlayer player = new();
    private readonly WaveAudioSource sfx = new("assets/lineclear.wav");

    public override void OnStart()
    {
        console.Print("SFX Example\n");
        console.Print("[SPACE]: Replay");
        console.Print("[F1]: Replay with x2 pitch");
        console.Print("[F2]: Replay with x0.5 pitch");
        console.Print("[F3]: Replay with x0.5 gain");
        console.Print("[F4]: Replay with x0 gain");
        console.Print("[ESC]: Quit");
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyUp)
            player.PlayOneShotAsync(sfx);

        if (keyboard.F1.IsKeyUp)
            player.PlayOneShotAsync(sfx, pitch: 2);

        if (keyboard.F2.IsKeyUp)
            player.PlayOneShotAsync(sfx, pitch: 0.5f);

        if (keyboard.F3.IsKeyUp)
            player.PlayOneShotAsync(sfx, gain: 0.5f);

        if (keyboard.F4.IsKeyUp)
            player.PlayOneShotAsync(sfx, gain: 0);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }
}
