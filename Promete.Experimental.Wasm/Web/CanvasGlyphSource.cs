using System.Runtime.InteropServices.JavaScript;
using Promete.Graphics.Fonts;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// ブラウザの Canvas2D でグリフをラスタライズする <see cref="IGlyphSource"/> です。
/// FreeType のネイティブを使わずに <c>Text</c> を動かすための PoC です。
/// </summary>
/// <param name="family">CSS のフォントファミリー名 (FontFace などで登録済みのもの)。</param>
public sealed partial class CanvasGlyphSource(string family) : IGlyphSource
{
    private const string Module = "canvasGlyph";

    public int SourceId { get; } = GlyphSourceId.Next();

    /// <summary>グリフ描画用の JavaScript モジュールを読み込みます。使用前に 1 回呼びます。</summary>
    /// <param name="moduleUrl">canvasGlyph.js の絶対 URL。</param>
    /// <returns>読み込みの完了を表すタスク。</returns>
    public static Task ImportModule(string moduleUrl) => JSHost.ImportAsync(Module, moduleUrl);

    public FontMetrics GetMetrics(in GlyphRenderOptions options)
    {
        var m = Metrics(family, options.Size, options.IsBold, options.IsItalic);
        return new FontMetrics((float)m[0], -(float)m[1], (float)(m[0] + m[1]));
    }

    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph)
    {
        glyph = new GlyphInfo
        {
            Source = this,
            GlyphIndex = (uint)codepoint,
            Codepoint = codepoint,
            Advance = (float)Advance(
                family,
                options.Size,
                options.IsBold,
                options.IsItalic,
                codepoint
            ),
        };
        return true;
    }

    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options)
    {
        if (
            !RasterizeNative(
                family,
                options.Size,
                options.IsBold,
                options.IsItalic,
                (int)glyph.GlyphIndex
            )
        )
            return GlyphBitmap.Empty;

        return new GlyphBitmap(
            LastPixels(),
            new VectorInt(LastWidth(), LastHeight()),
            new VectorInt(LastBearingX(), LastBearingY())
        );
    }

    public float GetKerning(int left, int right, in GlyphRenderOptions options) => 0;

    public void Dispose() { }

    [JSImport("metrics", Module)]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    private static partial double[] Metrics(string family, double size, bool bold, bool italic);

    [JSImport("advance", Module)]
    private static partial double Advance(
        string family,
        double size,
        bool bold,
        bool italic,
        int codepoint
    );

    [JSImport("rasterize", Module)]
    private static partial bool RasterizeNative(
        string family,
        double size,
        bool bold,
        bool italic,
        int codepoint
    );

    [JSImport("lastWidth", Module)]
    private static partial int LastWidth();

    [JSImport("lastHeight", Module)]
    private static partial int LastHeight();

    [JSImport("lastBearingX", Module)]
    private static partial int LastBearingX();

    [JSImport("lastBearingY", Module)]
    private static partial int LastBearingY();

    [JSImport("lastPixels", Module)]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    private static partial byte[] LastPixels();
}
