using System.Collections.Concurrent;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// バイト列を持つテスト用のソース。開いた回数と開いたストリームを記録し、次の OpenRead を止めることもできる。
/// 渡されたパスは正規化済みとみなし、序数比較で照合する。
/// </summary>
internal sealed class TrackingAssetSource : IAssetSource
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _openCounts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, OpenGate> _gates = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<TrackedStream> _streams = new();

    /// <summary>
    /// これまでに開いたストリーム (開いた順)。
    /// </summary>
    public IReadOnlyList<TrackedStream> Streams => _streams.ToArray();

    public TrackingAssetSource Add(string path, byte[] content)
    {
        _files[path] = content;
        return this;
    }

    /// <summary>
    /// 指定したパスを OpenRead した回数。
    /// </summary>
    public int OpenCount(string path) => _openCounts.TryGetValue(path, out var count) ? count : 0;

    /// <summary>
    /// 指定したパスで開いたストリーム。
    /// </summary>
    public IReadOnlyList<TrackedStream> StreamsOf(string path) =>
        _streams.Where(s => s.Path == path).ToArray();

    /// <summary>
    /// 指定したパスの次の OpenRead を、返したゲートが開かれるまで止める (1 回だけ)。
    /// </summary>
    public OpenGate BlockNextOpen(string path)
    {
        var gate = new OpenGate();
        _gates[path] = gate;
        return gate;
    }

    public bool Exists(string path) => _files.ContainsKey(path);

    public Stream OpenRead(string path)
    {
        _openCounts.AddOrUpdate(path, 1, (_, c) => c + 1);
        if (_gates.TryRemove(path, out var gate))
            gate.Wait();

        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException($"'{path}' is not found.", path);

        var stream = new TrackedStream(path, bytes);
        _streams.Enqueue(stream);
        return stream;
    }

    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive) =>
        _files.Keys.Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// OpenRead を止めるゲート。
    /// </summary>
    internal sealed class OpenGate
    {
        private readonly ManualResetEventSlim _entered = new();
        private readonly ManualResetEventSlim _released = new();

        /// <summary>
        /// 止められた OpenRead に入るまで待つ。
        /// </summary>
        public void WaitEntered()
        {
            if (!_entered.Wait(GateTimeout))
                throw new TimeoutException("OpenRead が呼ばれませんでした。");
        }

        /// <summary>
        /// 止めた OpenRead を進める。
        /// </summary>
        public void Release() => _released.Set();

        internal void Wait()
        {
            _entered.Set();
            if (!_released.Wait(GateTimeout))
                throw new TimeoutException("ゲートが開かれませんでした。");
        }
    }
}

/// <summary>
/// Dispose されたかどうかを記録するストリーム。
/// </summary>
internal sealed class TrackedStream(string path, byte[] bytes) : MemoryStream(bytes, false)
{
    private int _disposeCount;

    public string Path { get; } = path;

    public bool IsDisposed => Volatile.Read(ref _disposeCount) > 0;

    protected override void Dispose(bool disposing)
    {
        Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}
