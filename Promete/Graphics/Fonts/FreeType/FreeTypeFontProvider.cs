using System;
using System.Collections.Generic;
using System.IO;

namespace Promete.Graphics.Fonts.FreeType;

/// <summary>
/// FreeType とシステムフォントを用いる、既定の <see cref="IFontProvider"/> の実装です。
/// </summary>
/// <remarks>
/// グリフの供給元のキャッシュをプロセス全体で共有するため、<see cref="Shared"/> の 1 つのインスタンスだけを使います。
/// </remarks>
internal sealed class FreeTypeFontProvider : IFontProvider
{
    private readonly Dictionary<(string Path, int FaceIndex), IGlyphSource> _sourceCache = new();
    private readonly Lazy<SystemFontInfo> _defaultFont = new(ResolveDefaultFont);

    private FreeTypeFontProvider() { }

    /// <summary>
    /// 共有のインスタンスを取得します。
    /// </summary>
    public static FreeTypeFontProvider Shared { get; } = new();

    public Font FromFile(
        string path,
        float size,
        FontStyle style,
        bool isAntialiased,
        int faceIndex
    )
    {
        return Font.FromGlyphSource(GetOrLoadSource(path, faceIndex), size, style, isAntialiased);
    }

    public Font FromSystem(string familyName, float size, FontStyle style, bool isAntialiased)
    {
        if (!SystemFonts.TryGet(familyName, style, out var info))
            throw new FontException($"フォント \"{familyName}\" が見つかりませんでした。");

        return FromSystemFontInfo(info, size, style, isAntialiased);
    }

    public Font FromStream(Stream stream, float size, FontStyle style, bool isAntialiased)
    {
        return Font.FromGlyphSource(
            FreeTypeGlyphSource.FromStream(stream),
            size,
            style,
            isAntialiased
        );
    }

    public Font GetDefault(float size, FontStyle style, bool isAntialiased)
    {
        return FromSystemFontInfo(_defaultFont.Value, size, style, isAntialiased);
    }

    /// <summary>
    /// 実行環境における既定のフォントを解決します。
    /// </summary>
    private static SystemFontInfo ResolveDefaultFont()
    {
        if (SystemFonts.TryGetFirst(EnumerateDefaultFamilies(), FontStyle.Normal, out var info))
            return info;

        // 候補がいずれも見つからない環境では、最初に見つかったフォントで代用する
        return SystemFonts.Fonts.Count > 0
            ? SystemFonts.Fonts[0]
            : throw new FontException("利用できるフォントが見つかりませんでした。");
    }

    private static IEnumerable<string> EnumerateDefaultFamilies()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["BIZ UDGothic", "Yu Gothic", "Meiryo", "MS Gothic", "Segoe UI", "Arial"];
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return ["BIZ UDGothic", "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Helvetica"];
        }

        return
        [
            "Noto Sans CJK JP",
            "Noto Sans JP",
            "IPAGothic",
            "Droid Sans Fallback",
            "DejaVu Sans",
            "Liberation Sans",
        ];
    }

    private Font FromSystemFontInfo(
        SystemFontInfo info,
        float size,
        FontStyle style,
        bool isAntialiased
    )
    {
        // 要求されたスタイルの字形が存在する場合、重ねて合成する必要はない
        var resolvedStyle = info.Style == style ? FontStyle.Normal : style;
        return Font.FromGlyphSource(
            GetOrLoadSource(info.Path, info.FaceIndex),
            size,
            resolvedStyle,
            isAntialiased
        );
    }

    /// <summary>
    /// グリフソースを取得します。同一のファイルとフェイスに対しては同じインスタンスを返します。
    /// </summary>
    private IGlyphSource GetOrLoadSource(string path, int faceIndex)
    {
        var key = (Path.GetFullPath(path), faceIndex);
        if (_sourceCache.TryGetValue(key, out var source))
            return source;

        source = FreeTypeGlyphSource.FromFile(key.Item1, faceIndex);
        _sourceCache[key] = source;
        return source;
    }
}
