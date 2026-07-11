using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.window;

[Demo("window/time.demo", "時間情報を表示する")]
public class Time(ConsoleLayer console, Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        console.Clear();
        console.Print($"TimeScale: {Time.TimeScale}");
        console.Print($"Time: {Time.TotalTime}");
        console.Print($"Time without Scale: {Time.TotalTimeWithoutScale}");
        console.Print($"DeltaTime: {Time.DeltaTime}");
        console.Print($"FPS: {Time.FramePerSeconds}");
        console.Print($"UPS: {Time.UpdatePerSeconds}");
        console.Print("Press [ESC] to return");

        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }
}
