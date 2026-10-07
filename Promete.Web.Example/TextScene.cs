using System.Drawing;
using Promete;
using Promete.Graphics.Fonts;
using Promete.Nodes;

namespace Promete.Web.Example;

/// <summary>
/// テキスト描画の検証用シーン。Web では、フォントの生成がブラウザの Canvas2D によるグリフ描画に置き換わる。
/// </summary>
public class TextScene : Scene
{
    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        var small = Font.FromFile("assets/MisakiGothic.ttf", 8);
        var large = Font.FromFile("assets/MisakiGothic.ttf", 24);

        Root.Add(new Text("Hello, Promete on WebAssembly!", large, Color.White).Location(10, 10));
        Root.Add(
            new Text(
                "日本語のテキストも描画できる？ ひらがな カタカナ 漢字",
                small,
                Color.Lime
            ).Location(10, 60)
        );
        Root.Add(new Text("複数行の\nテキスト\nを表示", large, Color.Gold).Location(10, 100));
    }
}
