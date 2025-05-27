using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/vsync.demo", "VSync Test")]
public class VsyncTestScene(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnUpdate()
    {
        console.Clear();
        console.Print("VSync: " + Window.IsVsyncMode);
        console.Print("Fps: " + Window.FramePerSeconds);
        console.Print("Target Refresh Rate: " + Window.TargetFps);
        console.Print("[ESC]: return");
        console.Print("[SPACE]: Toggle VSync Mode");

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (keyboard.Space.IsKeyUp)
            Window.IsVsyncMode ^= true;
        if (keyboard.Left.IsKeyDown || keyboard.Left.ElapsedTime > 0.3)
            Window.TargetFps = Math.Max(0, Window.TargetFps - 1);
        if (keyboard.Right.IsKeyDown || keyboard.Right.ElapsedTime > 0.3)
            Window.TargetFps = Math.Min(240, Window.TargetFps + 1);
    }
}
