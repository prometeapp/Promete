using Promete.Graphics.Fonts;

namespace Promete.Experimental.Wasm.Web;

/// <summary>
/// Canvas2D でグリフを描画する <see cref="IFontProvider"/> です。
/// </summary>
/// <remarks>
/// ファイルから作るフォントは、事前に JavaScript が FontFace として登録した、絶対パスをファミリー名とするフォントを使います。
/// システムフォントは、ファミリー名を CSS の font-family としてそのまま使います。
/// </remarks>
public sealed class CanvasFontProvider : IFontProvider
{
    private const string DefaultFamily = "sans-serif";

    private readonly Dictionary<string, IGlyphSource> _sourceCache = [];

    public Font FromFile(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        int faceIndex
    ) =>
        Font.FromGlyphSource(GetOrCreateSource(Path.GetFullPath(path)), size, style, isAntialiased);

    public Font FromSystem(string familyName, float size, FontStyle style, bool isAntialiased) =>
        Font.FromGlyphSource(GetOrCreateSource(familyName), size, style, isAntialiased);

    public Font FromStream(Stream stream, float size, FontStyle style, bool isAntialiased) =>
        throw new NotSupportedException(
            "ブラウザでは、ストリームからのフォントの生成に対応していません (FontFace の登録が非同期のため)。"
        );

    public Font GetDefault(float size, FontStyle style, bool isAntialiased) =>
        Font.FromGlyphSource(GetOrCreateSource(DefaultFamily), size, style, isAntialiased);

    private IGlyphSource GetOrCreateSource(string family)
    {
        if (!_sourceCache.TryGetValue(family, out var source))
        {
            source = new CanvasGlyphSource(family);
            _sourceCache[family] = source;
        }

        return source;
    }
}
