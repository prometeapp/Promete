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
    private readonly StringBuilder buf = new();
    private Text? dumpView;
    private Text? editorView;
    private Text? ptmlView;

    public override void OnStart()
    {
        console.Print("Promete Text Editor");
        console.Print("Press [ESC] to exit");

        editorView = new Text("", Font.GetDefault(), Color.White)
            .Location(8, 64);
        ptmlView = new Text("", Font.GetDefault(), Color.White)
            .Location(8, 84);
        dumpView = new Text("", Font.GetDefault(), Color.White)
            .Location(8, 140);

        ptmlView.UseRichText = true;

        Root.AddRange(editorView, ptmlView, dumpView);

        // 実行前に残ったキー入力をクリア
        keyboard.GetString();
    }

    public override void OnUpdate()
    {
        editorView!.Content = buf.ToString();
        if ((keyboard.BackSpace.ElapsedFrameCount == 1 ||
             (keyboard.BackSpace.ElapsedTime > 0.5f && keyboard.BackSpace.ElapsedFrameCount % 3 == 0)) &&
            buf.Length > 0)
        {
            buf.Length--;
            DumpPtml();
        }

        if (keyboard.Enter.ElapsedFrameCount == 1 ||
            (keyboard.Enter.ElapsedTime > 0.5f && keyboard.Enter.ElapsedFrameCount % 3 == 0))
        {
            buf.Append('\n');
            DumpPtml();
        }

        if (keyboard.HasChar())
        {
            buf.Append(keyboard.GetString());
            DumpPtml();
        }

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    private void DumpPtml()
    {
        if (ptmlView == null) return;
        if (dumpView == null) return;

        ptmlView.Content = buf.ToString();

        try
        {
            var (plainText, decorations) = PtmlParser.Parse(buf.ToString(), true);
            dumpView.Content = $"""
                                （デバッグビュー）
                                プレーンテキスト：{plainText}

                                ダンプ：
                                {string.Join('\n', decorations)}
                                """;
            dumpView.Color = Color.Lime;
        }
        catch (PtmlParserException e)
        {
            dumpView.Content = e.Message;
            dumpView.Color = Color.Red;
        }
    }
}
