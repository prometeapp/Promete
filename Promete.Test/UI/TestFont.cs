using Promete.Graphics.Fonts;

namespace Promete.Test.UI;

/// <summary>
/// 既定のフォントの寸法を、実行時に測って提供します。
/// 既定のフォントは実行環境 (OS) によって異なるため、テストで文字の幅や行の高さを決め打ちしないために使います。
/// 等幅のフォントで、文字の幅が整数であることを前提にしています。
/// </summary>
internal static class TestFont
{
    /// <summary>1文字の幅。</summary>
    public static int CharWidth { get; } = Measure("aa").Glyphs[1].Position.X;

    /// <summary>1行の高さ。</summary>
    public static int LineHeight { get; } = Measure("a").Lines[0].Height;

    private static TextLayout Measure(string text) =>
        TextLayoutEngine.Layout(text, Font.GetDefault(), new TextRenderingOptions());
}
