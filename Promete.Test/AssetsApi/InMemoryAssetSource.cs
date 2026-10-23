using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// メモリ上の辞書を読むテスト用のソース。渡されたパスをそのまま (正規化済みとみなして) 序数比較で照合する。
/// </summary>
internal class InMemoryAssetSource : IAssetSource, IDisposable
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private int _disposeCount;

    public InMemoryAssetSource(params (string Path, string Content)[] files)
    {
        foreach (var (path, content) in files)
            Add(path, content);
    }

    /// <summary>
    /// Dispose が呼ばれた回数。
    /// </summary>
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    /// <summary>
    /// true にすると、OpenRead が seek できないストリームを返す。
    /// </summary>
    public bool ReturnNonSeekableStream { get; init; }

    public void Add(string path, string content) => _files[path] = Encoding.UTF8.GetBytes(content);

    public bool Exists(string path) => _files.ContainsKey(path);

    public Stream OpenRead(string path)
    {
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException($"'{path}' is not found.", path);
        var stream = new MemoryStream(bytes, false);
        return ReturnNonSeekableStream ? new NonSeekableStream(stream) : stream;
    }

    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        var prefix = directory.Length == 0 ? "" : directory + "/";
        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$"
        );
        foreach (var path in _files.Keys)
        {
            if (!path.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            var rest = path[prefix.Length..];
            if (!recursive && rest.Contains('/'))
                continue;
            var name = rest[(rest.LastIndexOf('/') + 1)..];
            if (regex.IsMatch(name))
                yield return path;
        }
    }

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}
