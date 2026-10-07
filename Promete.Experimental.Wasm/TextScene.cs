using System.Drawing;
using Promete;
using Promete.Experimental.Wasm.Web;
using Promete.Graphics.Fonts;
using Promete.Nodes;

namespace Promete.Experimental.Wasm;

/// <summary>
/// テキスト描画の検証用シーン。FreeType (ネイティブ) が使えるかを試した上で、
/// 使えない場合はブラウザの Canvas2D によるグリフ描画 (<see cref="CanvasGlyphSource"/>) で表示する。
/// </summary>
public class TextScene : Scene
{
    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        try
        {
            _ = Font.FromFile("/assets/MisakiGothic.ttf", 16).Metrics;
            FeatureScene.Failures["freetype"] = "(動作した)";
        }
        catch (Exception e)
        {
            FeatureScene.Failures["freetype"] = e.GetType().Name + ": " + e.Message;
        }

        var source = new CanvasGlyphSource("Misaki");
        var small = Font.FromGlyphSource(source, 8);
        var large = Font.FromGlyphSource(source, 24);

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
