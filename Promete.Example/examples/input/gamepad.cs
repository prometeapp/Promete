using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.input;

[Demo("input/gamepad.demo", "ゲームパッドの入力確認")]
public class GamepadExampleScene(ConsoleLayer console, Keyboard keyboard, Gamepads pads) : Scene
{
    private Gamepad? CurrentPad => pads[0];

    public override void OnUpdate()
    {
        console.Clear();
        if (CurrentPad is null || !CurrentPad.IsConnected)
        {
            console.Print("Connect a gamepad!");
            return;
        }

        console.Print($"Name: {CurrentPad.Name}");
        console.Print($"Supports Motor: {(CurrentPad.IsVibrationSupported ? "Yes" : "No")}");
        console.Print($"Left Stick: {CurrentPad.LeftStick}");
        console.Print($"Right Stick: {CurrentPad.RightStick}");


        console.Print($"\n{"Button Type",-12} Pressed Down  Up");
        foreach (var btn in CurrentPad.AllButtons)
            console.Print($"{btn.Type.ToString(),-12} {btn.IsPressed,-7} {btn.IsButtonDown,-5} {btn.IsButtonUp,-5}");

        if (keyboard.Escape.IsKeyDown) App.LoadScene<MainScene>();
    }
}
