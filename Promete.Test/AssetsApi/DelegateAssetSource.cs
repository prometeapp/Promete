using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 各メンバーの振る舞いをデリゲートで差し替えられるテスト用のソース。
/// </summary>
internal sealed class DelegateAssetSource : IAssetSource, IDisposable
{
    private int _disposeCount;

    public Func<string, bool> OnExists { get; init; } = _ => false;

    public Func<string, Stream> OnOpenRead { get; init; } =
        path => throw new FileNotFoundException("not found", path);

    public Func<string, string, bool, IEnumerable<string>> OnEnumerate { get; init; } =
        (_, _, _) => [];

    public Action? OnDispose { get; init; }

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool Exists(string path) => OnExists(path);

    public Stream OpenRead(string path) => OnOpenRead(path);

    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive) =>
        OnEnumerate(directory, pattern, recursive);

    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCount);
        OnDispose?.Invoke();
    }
}
