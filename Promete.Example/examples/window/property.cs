using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples.window;

[Demo("window/property.demo", "ウィンドウの情報を変更する")]
public class Property(ConsoleLayer console, Keyboard keyboard) : Scene
{
    public override void OnUpdate()
    {
        console.Clear();
        console.Print($"Position: {View.Location}");
        console.Print($"Size: {View.Size}");
        console.Print($"ActualSize: {View.ActualSize}");
        console.Print($"IsFocused: {View.IsFocused}");
        console.Print($"IsVisible: {View.IsVisible}");
        console.Print($"UPS: {Time.UpdatePerSeconds}");
        console.Print($"FPS: {Time.FramePerSeconds}");
        console.Print($"Mode: {View.Mode}");

        console.Print($"[1]: Set WindowMode to {nameof(WindowMode.Resizable)}");
        console.Print($"[2]: Set WindowMode to {nameof(WindowMode.Fixed)}");
        console.Print($"[3]: Set WindowMode to {nameof(WindowMode.NoFrame)}");
        console.Print("[4]: Toggle Fullscreen: " + View.IsFullScreen);

        // TODO: vsyncmodeを復活させたら修正する
        // console.Print("[5]: Toggle VSync: " + View.IsVsyncMode);
        console.Print("[6]: Toggle TopMost: " + View.TopMost);
        console.Print("[ESC]: Exit");

        if (keyboard.Number1.IsKeyDown)
            View.Mode = WindowMode.Resizable;
        if (keyboard.Number2.IsKeyDown)
            View.Mode = WindowMode.Fixed;
        if (keyboard.Number3.IsKeyDown)
            View.Mode = WindowMode.NoFrame;
        if (keyboard.Number4.IsKeyDown)
            View.IsFullScreen = !View.IsFullScreen;

        // if (keyboard.Number5.IsKeyDown)
        //     Window.IsVsyncMode = !Window.IsVsyncMode;
        if (keyboard.Number6.IsKeyDown)
            View.TopMost = !View.TopMost;
        if (keyboard.Escape.IsKeyDown)
            App.LoadScene<MainScene>();
    }
}
