using System.Diagnostics.CodeAnalysis;
using Promete.Assets;

namespace Promete.Web.Assets;

/// <summary>
/// ブラウザでアセットパッケージを読むソースです。読み込みはパッケージに任せ、フォントだけは取り出し済みのファイルのパスを返します。
/// </summary>
/// <remarks>
/// <see cref="TryGetFilePath"/> は、パッケージにあるフォント (<see cref="WebPackageFonts.IsFont"/>) で、
/// <see cref="WebPackageFonts.Extract"/> が取り出したファイルがある場合だけ <see langword="true"/> を返します。
/// それ以外 (フォントでない、取り出されていない、パッケージに無い) は <see langword="false"/> です。
/// </remarks>
internal sealed class WebPackageAssetSource : IFileBackedAssetSource, IDisposable
{
    /// <summary>
    /// <see cref="WebPackageAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="package">パッケージのソース。所有権はこのインスタンスに移ります (<see cref="Dispose"/> で破棄します)。</param>
    /// <param name="fontDirectory">フォントの取り出し先 (<see cref="WebPackageFonts.GetFontDirectory"/>)。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public WebPackageAssetSource(IAssetSource package, string fontDirectory)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// パッケージのソースを破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
