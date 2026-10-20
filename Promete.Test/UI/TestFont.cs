using Promete.Graphics.Fonts;

namespace Promete.Test.UI;

/// <summary>
/// 既定のフォントの寸法を、実行時に測って提供します。
/// 既定のフォントは実行環境 (OS) によって異なるため、テストで文字の幅や行の高さを決め打ちしないために使います。
/// フォントが比例フォントの場合もあるため、文字の幅は 'a' の幅を指します。
/// 位置を計算するテストでは、同じ文字 (a) だけを並べて使います。
/// </summary>
internal static class TestFont
{
    /// <summary>文字 'a' 1つ分の幅。</summary>
    public static int CharWidth { get; } = Measure("aa").Glyphs[1].Position.X;

    /// <summary>1行の高さ。</summary>
    public static int LineHeight { get; } = Measure("a").Lines[0].Height;

    private static TextLayout Measure(string text) =>
        TextLayoutEngine.Layout(text, Font.GetDefault(), new TextRenderingOptions());
}
