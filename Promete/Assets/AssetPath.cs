using System;

namespace Promete.Assets;

/// <summary>
/// アセットの論理パスを扱うヘルパーです。
/// </summary>
/// <remarks>
/// <para>論理パスの規則は次のとおりです。</para>
/// <list type="bullet">
/// <item><description>区切り文字は <c>/</c>。<c>\</c> を含むパスは使えません。</description></item>
/// <item><description>先頭の <c>/</c> の有無は区別しません。正規化したパスには先頭の <c>/</c> が付きません。</description></item>
/// <item><description><c>.</c> と <c>..</c> は解決します。<c>..</c> でルートより上には出られません (ルートで止まります)。</description></item>
/// <item><description>Unicode は NFC に正規化します。</description></item>
/// <item><description>大文字・小文字を区別します。</description></item>
/// </list>
/// <para>
/// <c>InvariantGlobalization</c> が有効な環境では、Unicode の正規化は行われません (何もせずに元の文字列を返します)。
/// その場合、ASCII 以外の文字を含むパスは NFC で渡してください。
/// </para>
/// </remarks>
public static class AssetPath
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

    /// <summary>
    /// ディレクトリの論理パスを正規化します。ルートは空文字列になります。
    /// </summary>
    /// <param name="path">正規化するパス。</param>
    /// <returns>正規化したパス。ルートの場合は空文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が <c>\</c> を含む場合。</exception>
    public static string NormalizeDirectory(string path)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// ファイル名がパターンに一致するかどうかを取得します。
    /// </summary>
    /// <param name="fileName">ファイル名 (ディレクトリを含まない)。</param>
    /// <param name="pattern">パターン。<c>*</c> は 0 文字以上、<c>?</c> は 1 文字に一致します。それ以外の文字はそのまま比較します。大文字・小文字を区別します。</param>
    /// <returns>一致する場合は <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> が <c>/</c> または <c>\</c> を含む場合。</exception>
    public static bool IsMatch(string fileName, string pattern)
    {
        throw new NotImplementedException();
    }
}
