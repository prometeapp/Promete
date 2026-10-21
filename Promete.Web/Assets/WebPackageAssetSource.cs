using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
    private readonly IAssetSource _package;
    private readonly string _fontDirectory;
    private int _disposed;

    /// <summary>
    /// <see cref="WebPackageAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="package">パッケージのソース。所有権はこのインスタンスに移ります (<see cref="Dispose"/> で破棄します)。</param>
    /// <param name="fontDirectory">フォントの取り出し先 (<see cref="WebPackageFonts.GetFontDirectory"/>)。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    public WebPackageAssetSource(IAssetSource package, string fontDirectory)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(fontDirectory);

        _package = package;
        _fontDirectory = fontDirectory;
    }

    /// <inheritdoc />
    public bool Exists(string path) => _package.Exists(path);

    /// <inheritdoc />
    public Stream OpenRead(string path) => _package.OpenRead(path);

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive) =>
        _package.Enumerate(directory, pattern, recursive);

    /// <inheritdoc />
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        fullPath = null;

        var logicalPath = AssetPath.Normalize(path);
        if (!WebPackageFonts.IsFont(logicalPath) || !_package.Exists(logicalPath))
        {
            return false;
        }

        var candidate = Path.GetFullPath(
            Path.Combine(_fontDirectory, Path.Combine(logicalPath.Split('/')))
        );
        if (!File.Exists(candidate))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    /// <summary>
    /// パッケージのソースを破棄します。2 回目以降の呼び出しは何もしません。
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && _package is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
