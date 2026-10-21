using System;

namespace Promete.AssetPacker;

/// <summary>
/// アセットパッケージやマニフェストに書く論理パスを正規化します。
/// </summary>
/// <remarks>
/// <para>
/// 実行時の <c>Promete.Assets.AssetPath.Normalize</c> と同じ結果になります。netstandard2.0 のため、同じ規則をこちらで実装しています。
/// </para>
/// <list type="bullet">
/// <item><description>区切り文字は <c>/</c>。<c>\</c> を含むパスは使えません。</description></item>
/// <item><description>先頭の <c>/</c> と、空の要素 (<c>//</c>) は取り除きます。</description></item>
/// <item><description><c>.</c> と <c>..</c> は解決します。<c>..</c> でルートより上には出られません (ルートで止まります)。</description></item>
/// <item><description>Unicode は NFC に正規化します。</description></item>
/// <item><description>大文字・小文字はそのまま残します。</description></item>
/// </list>
/// </remarks>
public static class AssetPackagePath
{
    /// <summary>
    /// ファイルの論理パスを正規化します。
    /// </summary>
    /// <param name="path">正規化するパス。</param>
    /// <returns>正規化したパス (例: <c>"/a/./b/../c.png"</c> → <c>"a/c.png"</c>)。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が <c>\</c> を含む場合や、正規化した結果が空になる場合。</exception>
    public static string Normalize(string path)
    {
        throw new NotImplementedException();
    }
}
