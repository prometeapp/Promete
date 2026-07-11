using Promete.Audio;
using Promete.Audio.Filters;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.audio;

[Demo("/audio/filters.demo", "AudioPlayerのDSPフィルターを切り替えます")]
public class FiltersExampleScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly AudioPlayer _audio = new();
    private readonly VorbisAudioSource _bgm = new("./assets/amaebi.ogg");

    private readonly LowPassFilter _lowPass = new();
    private readonly DelayFilter _delay = new();
    private readonly DistortionFilter _distortion = new();

    private bool _isLowPassEnabled;
    private bool _isDelayEnabled;
    private bool _isDistortionEnabled;

    public override void OnStart()
    {
        Window.Title = "Audio filters example";
        console.Print("Audio Filters Example");

        _audio.Play(_bgm, 0);
        _delay.Mix = 0.23f;
        _delay.Feedback = 0.12f;
        _delay.Time = 0.25f;
    }

    public override void OnUpdate()
    {
        console.Clear();
        console.Print(
            $"""
            Audio Filters Example

            [1] LowPass  : {(
                _isLowPassEnabled ? "ON" : "OFF"
            )} (Cutoff: {_lowPass.CutoffFrequency:0} Hz) (Mix: {_lowPass.Mix * 100:0}%) {(_lowPass.AutoMakeupGain ? "AUTO GAIN" : "")}
            [2] Delay    : {(_isDelayEnabled ? "ON" : "OFF")}
            [3] Distortion: {(_isDistortionEnabled ? "ON" : "OFF")} (Drive: {_distortion.Drive})

            [↑] LowPass Cutoff Up
            [↓] LowPass Cutoff Down

            Is Playing: {_audio.IsPlaying}
            PRESS ESC TO RETURN
            """
        );

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Number1.IsKeyUp)
            ToggleFilter(_lowPass, ref _isLowPassEnabled);

        if (keyboard.Number2.IsKeyUp)
            ToggleFilter(_delay, ref _isDelayEnabled);

        if (keyboard.Number3.IsKeyUp)
            ToggleFilter(_distortion, ref _isDistortionEnabled);

        if (keyboard.Up.IsPressed)
            _lowPass.CutoffFrequency += 10;

        if (keyboard.Down.IsPressed)
            _lowPass.CutoffFrequency -= 10;

        if (keyboard.Right.IsKeyDown)
            _distortion.Drive += 1;

        if (keyboard.Left.IsKeyDown)
            _distortion.Drive -= 1;

        if (keyboard.W)
            _lowPass.Mix = Math.Clamp(_lowPass.Mix + 0.01f, 0, 1);

        if (keyboard.S)
            _lowPass.Mix = Math.Clamp(_lowPass.Mix - 0.01f, 0, 1);

        if (keyboard.A.IsKeyDown)
            _lowPass.AutoMakeupGain ^= true;

        if (keyboard.Space.IsKeyDown)
            if (!_audio.IsPausing)
                _audio.Pause();
            else
                _audio.Resume();
    }

    public override void OnDestroy()
    {
        _audio.Stop();
        _audio.Dispose();
        _bgm.Dispose();
    }

    private void ToggleFilter(IAudioFilter filter, ref bool isEnabled)
    {
        if (isEnabled)
            _audio.Filters.Remove(filter);
        else
            _audio.Filters.Add(filter);

        isEnabled = !isEnabled;
    }
}
