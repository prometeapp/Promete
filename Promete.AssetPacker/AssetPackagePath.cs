using System;
using System.Collections.Generic;
using System.Text;

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
/// <item><description><c>InvariantGlobalization</c> が有効な環境では Unicode の正規化が効かないため、ASCII 以外の文字を含むパスは拒否します。</description></item>
/// </list>
/// </remarks>
public static class AssetPackagePath
{
    /// <summary>
    /// この実行環境で Unicode の正規化 (<see cref="string.Normalize(NormalizationForm)"/>) が効くかどうかを取得します。
    /// </summary>
    /// <remarks>
    /// <c>InvariantGlobalization</c> が有効な環境では、<see cref="string.Normalize(NormalizationForm)"/> は何もせずに元の文字列を返します。
    /// 判定は型の初期化で 1 回だけ行い、結果を使い回します。
    /// </remarks>
    internal static bool IsUnicodeNormalizationSupported { get; } =
        string.Concat((char)0x304B, (char)0x3099).Normalize(NormalizationForm.FormC)
        == ((char)0x304C).ToString();

    /// <summary>
    /// ファイルの論理パスを正規化します。
    /// </summary>
    /// <param name="path">正規化するパス。</param>
    /// <returns>正規化したパス (例: <c>"/a/./b/../c.png"</c> → <c>"a/c.png"</c>)。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> が <c>\</c> を含む場合や、正規化した結果が空になる場合。
    /// Unicode の正規化が効かない環境 (InvariantGlobalization) で、ASCII 以外の文字を含む場合も含みます。</exception>
    public static string Normalize(string path) => Normalize(path, IsUnicodeNormalizationSupported);

    /// <summary>
    /// 正規化が効くかどうかを指定して、ファイルの論理パスを正規化します。<see cref="Normalize(string)"/> の本体で、テストから効かない環境を再現するために使います。
    /// </summary>
    /// <param name="path">正規化するパス。</param>
    /// <param name="unicodeNormalizationSupported">Unicode の正規化が効く環境かどうか。</param>
    /// <returns>正規化したパス。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> が <c>\</c> を含む場合や、正規化した結果が空になる場合。
    /// <paramref name="unicodeNormalizationSupported"/> が <see langword="false"/> で、<paramref name="path"/> が ASCII 以外の文字を含む場合 (NFC になっているか確かめられないため)。
    /// </exception>
    internal static string Normalize(string path, bool unicodeNormalizationSupported)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.IndexOf('\\') >= 0)
        {
            throw new ArgumentException(
                "論理パスの区切り文字は '/' のみ使えます。'\\' は使えません。",
                nameof(path)
            );
        }

        // 正規化が効かない環境では、NFC かどうか確かめられないので、ASCII 以外は NFC でも拒否する
        if (!unicodeNormalizationSupported && ContainsNonAscii(path))
        {
            throw new ArgumentException(
                $"パス '{path}' は ASCII 以外の文字を含みますが、この環境では Unicode の正規化 (NFC) が効きません (InvariantGlobalization が有効です)。"
                    + "ビルドを InvariantGlobalization が無効な環境で行うか、パスを ASCII にしてください。",
                nameof(path)
            );
        }

        var segments = new List<string>();
        foreach (var segment in path.Normalize(NormalizationForm.FormC).Split('/'))
        {
            if (segment.Length == 0 || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            throw new ArgumentException("ファイルのパスは空にできません。", nameof(path));
        }

        return string.Join("/", segments);
    }

    /// <summary>
    /// 文字列に ASCII 以外の文字が含まれるかどうかを取得します。
    /// </summary>
    /// <param name="text">調べる文字列。</param>
    /// <returns>ASCII 以外の文字が 1 つでもあれば <see langword="true"/>。</returns>
    private static bool ContainsNonAscii(string text)
    {
        foreach (var c in text)
        {
            if (c > 0x7F)
            {
                return true;
            }
        }

        return false;
    }
}
