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
    /// パッケージのフォントを取り出すディレクトリを求めます。
    /// </summary>
    /// <param name="rootDirectory">仮想ファイルシステムのルート (ブラウザでは <c>/</c>)。</param>
    /// <param name="location">パッケージのファイル名、または埋め込みリソースの名前 (<c>PrometeAssetSourceAttribute.Location</c>)。</param>
    /// <returns><c>&lt;rootDirectory&gt;/.promete/fonts/&lt;location&gt;</c> (区切りは <see cref="Path.Combine(string[])"/> による)。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public static string GetFontDirectory(string rootDirectory, string location)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 論理パスがフォントのファイルかどうかを、拡張子 (<c>.ttf</c> / <c>.otf</c> / <c>.woff</c> / <c>.woff2</c>。大文字・小文字を区別しない) で判定します。
    /// </summary>
    /// <param name="logicalPath">論理パス。</param>
    /// <returns>フォントなら <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="logicalPath"/> が <see langword="null"/> の場合。</exception>
    public static bool IsFont(string logicalPath)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
