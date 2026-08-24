using System;
using System.Collections.Generic;
using System.Linq;
using Promete.Graphics.Fonts.FreeType;

namespace Promete.Graphics.Fonts;

/// <summary>
/// 実行環境にインストールされているフォントを検索します。
/// </summary>
/// <remarks>
/// フォントの一覧は初回アクセス時に構築されます。
/// このクラスに触れないアプリケーションでは、走査のコストは発生しません。
/// </remarks>
public static class SystemFonts
{
    private static readonly Lazy<IReadOnlyList<SystemFontInfo>> LazyFonts = new(
        () => SystemFontScanner.Scan()
    );

    private static readonly Lazy<
        IReadOnlyDictionary<string, IReadOnlyList<SystemFontInfo>>
    > LazyIndex = new(BuildIndex);

    /// <summary>
    /// 見つかったすべてのフォントを取得します。
    /// </summary>
    public static IReadOnlyList<SystemFontInfo> Fonts => LazyFonts.Value;

    /// <summary>
    /// 見つかったフォントファミリー名を取得します。
    /// </summary>
    public static IEnumerable<string> Families => LazyIndex.Value.Keys;

    /// <summary>
    /// 指定したファミリー名のフォントをすべて取得します。
    /// </summary>
    public static IReadOnlyList<SystemFontInfo> GetAll(string familyName)
    {
        return LazyIndex.Value.TryGetValue(familyName, out var fonts) ? fonts : [];
    }

    /// <summary>
    /// 指定したファミリー名とスタイルに最も適合するフォントの取得を試みます。
    /// </summary>
    /// <param name="familyName">フォントファミリー名。</param>
    /// <param name="style">求めるフォントスタイル。</param>
    /// <param name="font">見つかったフォント。</param>
    /// <returns>ファミリーが見つかれば <c>true</c>。</returns>
    /// <remarks>
    /// 要求されたスタイルの字形が存在しない場合は、
    /// 通常のスタイルを返します。この場合、スタイルは描画時に合成されます。
    /// </remarks>
    public static bool TryGet(string familyName, FontStyle style, out SystemFontInfo font)
    {
        var candidates = GetAll(familyName);
        if (candidates.Count == 0)
        {
            font = default;
            return false;
        }

        font = candidates.FirstOrDefault(
            f => f.Style == style,
            candidates.FirstOrDefault(f => f.Style == FontStyle.Normal, candidates[0])
        );
        return true;
    }

    /// <summary>
    /// 指定したファミリー名のフォントの取得を試みます。
    /// </summary>
    public static bool TryGet(string familyName, out SystemFontInfo font)
    {
        return TryGet(familyName, FontStyle.Normal, out font);
    }

    /// <summary>
    /// 指定したファミリー名のうち、最初に見つかったものを返します。
    /// </summary>
    /// <param name="familyNames">優先度の高い順に並べたファミリー名。</param>
    /// <param name="style">求めるフォントスタイル。</param>
    /// <param name="font">見つかったフォント。</param>
    public static bool TryGetFirst(
        IEnumerable<string> familyNames,
        FontStyle style,
        out SystemFontInfo font
    )
    {
        foreach (var name in familyNames)
        {
            if (TryGet(name, style, out font))
                return true;
        }

        font = default;
        return false;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SystemFontInfo>> BuildIndex()
    {
        return LazyFonts
            .Value.GroupBy(f => f.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SystemFontInfo>)g.ToArray(),
                StringComparer.OrdinalIgnoreCase
            );
    }
}
