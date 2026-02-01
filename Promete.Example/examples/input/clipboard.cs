using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.input;

[Demo("input/clipboard.demo", "クリップボード機能のデモ")]
public class clipboard(Keyboard keyboard, ConsoleLayer console) : Scene
{
    public override void OnStart()
    {
        console.Print("クリップボード機能のテスト");
        console.Print("[Ctrl+C]: クリップボード上に「Hello from Promete!」をコピーします");
        console.Print("[Ctrl+V]: クリップボード上の文字列をコンソールに貼り付けます");
        console.Print("[ESC] 終了");
        console.Print("");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
            return;
        }

        var isCtrlPressed = keyboard.ControlLeft.IsPressed || keyboard.ControlRight.IsPressed;

        if (isCtrlPressed && keyboard.C.IsKeyDown)
        {
            keyboard.ClipboardText = "Hello from Promete!";
            console.Print("クリップボードにコピーしました: Hello from Promete!");
        }

        if (isCtrlPressed && keyboard.V.IsKeyDown)
        {
            var text = keyboard.ClipboardText ?? "(クリップボードが空または、テキスト以外のデータです)";
            console.Print($"貼り付け: {text}");
        }
    }
}
