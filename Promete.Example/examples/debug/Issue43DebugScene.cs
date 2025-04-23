using System.Collections;
using Promete.Audio;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.debug;

/// <summary>
/// `AudioPlayer.Play` を再生中に呼び出すと、`IsPlaying` が `false` になる
/// https://github.com/prometeapp/Promete/issues/43
/// </summary>
[Demo("/debug/issue43", "Issue43: Debug Scene")]
public class Issue43DebugScene(CoroutineManager coroutine, ConsoleLayer console, Keyboard keyboard) : Scene
{
    private AudioPlayer _audioPlayer;
    private VorbisAudioSource _source;
    public override void OnStart()
    {
        coroutine.Start(Debug());
    }

    private IEnumerator Debug()
    {
        _audioPlayer = new AudioPlayer();
        _source = new VorbisAudioSource("assets/GB-Action-C02-2.ogg");
        console.Print("Loading...");
        yield return new WaitUntil(() => _source.IsLoadingFinished);

        _audioPlayer.Play(_source);
        console.Print("IsPlaying: " + _audioPlayer.IsPlaying);
        yield return new WaitForSeconds(1f);
        _audioPlayer.Play(_source);
        console.Print("IsPlaying: " + _audioPlayer.IsPlaying);

        Assert(_audioPlayer.IsPlaying, "問題は修正されました", "問題は修正されていません");
    }


    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }

    public override void OnDestroy()
    {
        _audioPlayer.Dispose();
        _source.Dispose();
    }

    private void Assert(bool condition, string successMessage, string failureMessage)
    {
        console.Print(condition ? successMessage : failureMessage);
    }
}
