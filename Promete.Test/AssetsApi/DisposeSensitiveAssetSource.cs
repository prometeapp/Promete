using System.Diagnostics.CodeAnalysis;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// Dispose された後は、すべてのメンバーが <see cref="ObjectDisposedException"/> を投げるテスト用のソース。
/// 実際のソース (パッケージのリーダーなど) が Unmount の後に呼ばれた状況を再現する。
/// </summary>
internal sealed class DisposeSensitiveAssetSource : IFileBackedAssetSource, IDisposable
{
    private readonly InMemoryAssetSource _inner;
    private int _disposed;

    public DisposeSensitiveAssetSource(params (string Path, string Content)[] files)
    {
        _inner = new InMemoryAssetSource(files);
    }

    /// <summary>
    /// true にすると、Exists が true を返した直後に自分を Dispose する (Exists と OpenRead の間に Unmount された状況)。
    /// </summary>
    public bool DisposeAfterExists { get; init; }

    /// <summary>
    /// 各メンバーの先頭で空回りする回数。並行テストで競合の窓を広げるために使う。
    /// </summary>
    public int SpinIterations { get; init; }

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public bool Exists(string path)
    {
        Enter();
        var exists = _inner.Exists(path);
        if (exists && DisposeAfterExists)
            Dispose();
        return exists;
    }

    public Stream OpenRead(string path)
    {
        Enter();
        return _inner.OpenRead(path);
    }

    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        Enter();
        return _inner.Enumerate(directory, pattern, recursive).ToList();
    }

    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        Enter();
        fullPath = _inner.Exists(path) ? "/sensitive/" + path : null;
        return fullPath != null;
    }

    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);

    private void Enter()
    {
        if (SpinIterations > 0)
            Thread.SpinWait(SpinIterations);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }
}
