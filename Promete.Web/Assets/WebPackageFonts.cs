using System.Linq;
using Promete.Assets;

namespace Promete.Web.Assets;

/// <summary>
/// アセットパッケージの中のフォントを、ブラウザの <c>FontFace</c> に登録できるよう仮想ファイルシステムに取り出します。
/// </summary>
/// <remarks>
/// <para>
/// ブラウザでは、フォントをストリームから作れません (<c>FontFace</c> の登録が非同期のため)。そこで、パッケージをマウントする前
/// (<c>promete.js</c> のプリロードの中、<c>Main</c> より前) に、中のフォントをファイルに取り出し、<c>promete.js</c> がそれを
/// <c>FontFace</c> に登録して読み込みの完了を待ちます。ファミリー名は取り出したファイルの絶対パスです (<c>CanvasFontProvider.FromFile</c> と同じ規則)。
/// </para>
/// <para>
/// 取り出し先は <see cref="GetFontDirectory"/> が決めます。<see cref="WebPackageAssetSource.TryGetFilePath"/> は同じ規則でパスを求めるので、
/// <c>AssetManager.LoadFont</c> は <c>FromFile</c> の経路で、登録済みのフォントを使えます。
/// </para>
/// </remarks>
internal static class WebPackageFonts
{
    /// <summary>
    /// フォントとみなす拡張子 (小文字)。
    /// </summary>
    private static readonly string[] FontExtensions = [".ttf", ".otf", ".woff", ".woff2"];

    /// <summary>
    /// パッケージのフォントを取り出すディレクトリを求めます。
    /// </summary>
    /// <param name="rootDirectory">仮想ファイルシステムのルート (ブラウザでは <c>/</c>)。</param>
    /// <param name="location">パッケージのファイル名、または埋め込みリソースの名前 (<c>PrometeAssetSourceAttribute.Location</c>)。</param>
    /// <returns><c>&lt;rootDirectory&gt;/.promete/fonts/&lt;location&gt;</c> (区切りは <see cref="Path.Combine(string[])"/> による)。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public static string GetFontDirectory(string rootDirectory, string location)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(location);

        return Path.Combine(rootDirectory, ".promete", "fonts", location);
    }

    /// <summary>
    /// 論理パスがフォントのファイルかどうかを、拡張子 (<c>.ttf</c> / <c>.otf</c> / <c>.woff</c> / <c>.woff2</c>。大文字・小文字を区別しない) で判定します。
    /// </summary>
    /// <param name="logicalPath">論理パス。</param>
    /// <returns>フォントなら <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="logicalPath"/> が <see langword="null"/> の場合。</exception>
    public static bool IsFont(string logicalPath)
    {
        ArgumentNullException.ThrowIfNull(logicalPath);

        foreach (var extension in FontExtensions)
        {
            if (logicalPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// パッケージの中のフォントを、すべてファイルに取り出します。
    /// </summary>
    /// <param name="package">パッケージのソース。</param>
    /// <param name="fontDirectory">取り出し先 (<see cref="GetFontDirectory"/>)。論理パスの <c>/</c> をディレクトリの区切りとして、その下に置きます。</param>
    /// <returns>取り出したファイルの絶対パス (<see cref="Path.GetFullPath(string)"/>)。論理パスの序数順です。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public static string[] Extract(IAssetSource package, string fontDirectory)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(fontDirectory);

        var fontPaths = package
            .Enumerate("", "*", true)
            .Where(IsFont)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var extracted = new string[fontPaths.Length];
        for (var i = 0; i < fontPaths.Length; i++)
        {
            extracted[i] = ExtractOne(package, fontPaths[i], fontDirectory);
        }

        return extracted;
    }

    /// <summary>
    /// 1 つのフォントを、取り出し先の下に書き出します。
    /// </summary>
    /// <param name="package">パッケージのソース。</param>
    /// <param name="logicalPath">フォントの論理パス。</param>
    /// <param name="fontDirectory">取り出し先のディレクトリ。</param>
    /// <returns>書き出したファイルの絶対パス。</returns>
    private static string ExtractOne(IAssetSource package, string logicalPath, string fontDirectory)
    {
        var rootFullPath = Path.GetFullPath(fontDirectory);
        var fullPath = Path.GetFullPath(
            Path.Combine(rootFullPath, Path.Combine(logicalPath.Split('/')))
        );
        var rootPrefix =
            rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"フォント '{logicalPath}' の取り出し先がディレクトリの外です。"
            );
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var input = package.OpenRead(logicalPath);
        using var output = File.Create(fullPath);
        input.CopyTo(output);

        return fullPath;
    }
}
