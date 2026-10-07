using System.Drawing;
using Promete;
using Promete.Audio;
using Promete.Experimental.Wasm.Web;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Experimental.Wasm;

/// <summary>
/// オーディオの検証用シーン。WAV (SE) と Ogg Vorbis (BGM) を、Web Audio 経由で再生する。
/// 1: BGM 再生/停止、2: WAV を Play、3: WAV を PlayOneShot (OpenAL 直叩きのため失敗する想定)。
/// </summary>
public class AudioScene(Keyboard keyboard) : Scene
{
    private AudioPlayer _player = null!;
    private VorbisAudioSource? _bgm;
    private WaveAudioSource? _wav;
    private Text _status = null!;
    private string _result = "(未実行)";

    /// <summary>検証結果を JavaScript から読むための文字列。</summary>
    public static string Report { get; private set; } = string.Empty;

    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        var font = Font.FromGlyphSource(new CanvasGlyphSource("Misaki"), 16);
        _status = new Text("audio", font, Color.White).Location(10, 10);
        Root.Add(_status);

        _player = new AudioPlayer(new WebAudioOutput());
        _bgm = new VorbisAudioSource("/assets/GB-Action-C02-2.ogg");
        _wav = new WaveAudioSource("/assets/lineclear.wav");
    }

    public override void OnUpdate()
    {
        if (keyboard.Number1.IsKeyDown)
        {
            if (_player.IsPlaying)
                _player.Stop();
            else
                _player.Play(_bgm!, 0);
        }

        if (keyboard.Number2.IsKeyDown)
            Try("Play(wav)", () => _player.Play(_wav!));
        if (keyboard.Number3.IsKeyDown)
            _ = TryAsync("PlayOneShotAsync(wav)", () => _player.PlayOneShotAsync(_wav!).AsTask());

        Report =
            $"state={WebAudioOutput.GetState()} t={WebAudioOutput.GetCurrentTime():0.00} rms={WebAudioOutput.GetRms():0.0000} playing={_player.IsPlaying} last={_result}";
        _status.Content = Report;
    }

    private async Task TryAsync(string name, Func<Task> action)
    {
        try
        {
            await action();
            _result = name + ": ok";
        }
        catch (Exception e)
        {
            _result = name + ": " + e.GetType().Name + " " + e.Message;
        }
    }

    private void Try(string name, Action action)
    {
        try
        {
            action();
            _result = name + ": ok";
        }
        catch (Exception e)
        {
            _result = name + ": " + e.GetType().Name;
        }
    }
}
