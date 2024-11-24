using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.window;

[Demo("window/property.demo", "ウィンドウの情報を変更する")]
public class property(ConsoleLayer console, Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        console.Clear();
        console.Print($"Position: {Window.Location}");
        console.Print($"Size: {Window.Size}");
        console.Print($"ActualSize: {Window.ActualSize}");
        console.Print($"IsFocused: {Window.IsFocused}");
        console.Print($"IsVisible: {Window.IsVisible}");
        console.Print($"UPS: {Window.UpdatePerSeconds}");
        console.Print($"FPS: {Window.FramePerSeconds}");
        console.Print($"Mode: {Window.Mode}");
        // console.Print($"TotalFrame: {Window.TotalFrame}");

        console.Print($"[1]: Set WindowMode to {nameof(WindowMode.Resizable)}");
        console.Print($"[2]: Set WindowMode to {nameof(WindowMode.Fixed)}");
        console.Print($"[3]: Set WindowMode to {nameof(WindowMode.NoFrame)}");
        console.Print("[4]: Toggle Fullscreen: " + Window.IsFullScreen);
        console.Print("[5]: Toggle VSync: " + Window.IsVsyncMode);
        console.Print("[ESC]: Exit");

        if (keyboard.Number1.IsKeyDown) Window.Mode = WindowMode.Resizable;
        if (keyboard.Number2.IsKeyDown) Window.Mode = WindowMode.Fixed;
        if (keyboard.Number3.IsKeyDown) Window.Mode = WindowMode.NoFrame;
        if (keyboard.Number4.IsKeyDown) Window.IsFullScreen = !Window.IsFullScreen;
        if (keyboard.Number5.IsKeyDown) Window.IsVsyncMode = !Window.IsVsyncMode;
        if (keyboard.Escape.IsKeyDown) App.LoadScene<MainScene>();
    }
}
