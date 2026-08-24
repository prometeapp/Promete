using System.Drawing;
using System.Text;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Markup;
using Promete.Nodes;

namespace Promete.Example.examples.ptml;

[Demo("/ptml/parse.demo", "PTMLの解析結果をダンプ")]
public class PtmlParseDemoScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private readonly StringBuilder _buf = new();
    private Text? _dumpView;
    private Text? _editorView;
    private Text? _ptmlView;

    public override void OnStart()
    {
        console.Print("Promete Text Editor");
        console.Print("Press [ESC] to exit");

        _editorView = new Text("", Font.GetDefault(), Color.White).Location(8, 64);
        _ptmlView = new Text("", Font.GetDefault(), Color.White).Location(8, 84);
        _dumpView = new Text("", Font.GetDefault(), Color.White).Location(8, 140);

        _ptmlView.UseRichText = true;

        Root.AddRange(_editorView, _ptmlView, _dumpView);

        // 実行前に残ったキー入力をクリア
        keyboard.GetString();
    }

    public override void OnUpdate()
    {
        _editorView!.Content = _buf.ToString();
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
        {
            _buf.Length--;
            DumpPtml();
        }

        if (
            keyboard.Enter.ElapsedFrameCount == 1
            || (keyboard.Enter.ElapsedTime > 0.5f && keyboard.Enter.ElapsedFrameCount % 3 == 0)
        )
        {
            _buf.Append('\n');
            DumpPtml();
        }

        if (keyboard.HasChar())
        {
            _buf.Append(keyboard.GetString());
            DumpPtml();
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    private void DumpPtml()
    {
        if (_ptmlView == null)
            return;
        if (_dumpView == null)
            return;

        _ptmlView.Content = _buf.ToString();

        try
        {
            var (plainText, decorations) = PtmlParser.Parse(_buf.ToString(), true);
            _dumpView.Content = $"""
                （デバッグビュー）
                プレーンテキスト：{plainText}

                ダンプ：
                {string.Join('\n', decorations)}
                """;
            _dumpView.Color = Color.Lime;
        }
        catch (PtmlParserException e)
        {
            _dumpView.Content = e.Message;
            _dumpView.Color = Color.Red;
        }
    }
}
