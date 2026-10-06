using System.Drawing;
using System.Text;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("/sample4.demo", "簡易テキストエディタ")]
public class TextEditorScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private readonly StringBuilder _buf = new();
    private Text? _editorView;

    public override void OnStart()
    {
        console.Print("Promete Text Editor");
        console.Print("Press [ESC] to exit");

        _editorView = new Text("", Font.GetDefault(), Color.White).Location(8, 64);
        Root.Add(_editorView);

        // 実行前に残ったキー入力をクリア
        keyboard.GetString();
    }

    public override void OnUpdate()
    {
        _editorView!.Content = _buf.ToString() + '_';
        if (
            (
                keyboard.BackSpace.ElapsedFrameCount == 1
                || (
                    keyboard.BackSpace.ElapsedTime > 0.5f
                    && keyboard.BackSpace.ElapsedFrameCount % 3 == 0
                )
            )
            && _buf.Length > 0
        )
            _buf.Length--;
        if (
            keyboard.Enter.ElapsedFrameCount == 1
            || (keyboard.Enter.ElapsedTime > 0.5f && keyboard.Enter.ElapsedFrameCount % 3 == 0)
        )
            _buf.Append('\n');

        if (keyboard.HasChar())
            _buf.Append(keyboard.GetString());

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }
}
