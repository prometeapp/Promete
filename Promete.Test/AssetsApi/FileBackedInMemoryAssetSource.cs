using System.Diagnostics.CodeAnalysis;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="IFileBackedAssetSource"/> を実装した <see cref="InMemoryAssetSource"/>。TryGetFilePath は "/fake/" + path を返す。
/// </summary>
internal sealed class FileBackedInMemoryAssetSource : InMemoryAssetSource, IFileBackedAssetSource
{
    public FileBackedInMemoryAssetSource(params (string Path, string Content)[] files)
        : base(files) { }

    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        fullPath = Exists(path) ? "/fake/" + path : null;
        return fullPath != null;
    }
}
