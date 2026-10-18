using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/vsync.demo", "VSync Test")]
public class VsyncTestScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnUpdate()
    {
        console.Clear();
        console.Print("VSync: " + Time.IsVsyncMode);
        console.Print("Fps: " + Time.FramePerSeconds);
        console.Print("Target Refresh Rate: " + Time.TargetFps);
        console.Print("[ESC]: return");
        console.Print("[SPACE]: Toggle VSync Mode");

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Space.IsKeyUp)
            Time.IsVsyncMode ^= true;
        if (keyboard.Left.IsKeyDown || keyboard.Left.ElapsedTime > 0.3)
            Time.TargetFps = Math.Max(0, Time.TargetFps - 1);
        if (keyboard.Right.IsKeyDown || keyboard.Right.ElapsedTime > 0.3)
            Time.TargetFps = Math.Min(240, Time.TargetFps + 1);
    }
}
