namespace Promete.Test.AssetsApi;

/// <summary>
/// 読んだバイト数と Dispose の回数を記録する、メモリ上のストリーム。<see cref="CanSeekOverride"/> で seek できないふりもできる。
/// </summary>
internal sealed class ProbeStream(byte[] data) : Stream
{
    private readonly MemoryStream _inner = new(data, false);
    private long _bytesRead;
    private int _disposeCount;

    /// <summary>
    /// <see langword="false"/> にすると、seek できないストリームとしてふるまう。
    /// </summary>
    public bool CanSeekOverride { get; init; } = true;

    /// <summary>
    /// Read で返したバイト数の合計。
    /// </summary>
    public long BytesRead => Interlocked.Read(ref _bytesRead);

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool IsDisposed => DisposeCount > 0;

    public override bool CanRead => !IsDisposed;

    public override bool CanSeek => CanSeekOverride && !IsDisposed;

    public override bool CanWrite => false;

    public override long Length =>
        CanSeekOverride ? _inner.Length : throw new NotSupportedException();

    public override long Position
    {
        get => CanSeekOverride ? _inner.Position : throw new NotSupportedException();
        set
        {
            if (!CanSeekOverride)
                throw new NotSupportedException();
            _inner.Position = value;
        }
    }

    public void ResetCounter() => Interlocked.Exchange(ref _bytesRead, 0);

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var read = _inner.Read(buffer);
        Interlocked.Add(ref _bytesRead, read);
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        CanSeekOverride ? _inner.Seek(offset, origin) : throw new NotSupportedException();

    public override void Flush() { }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Interlocked.Increment(ref _disposeCount);
        base.Dispose(disposing);
    }
}
