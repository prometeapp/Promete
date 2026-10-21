using System;
using System.IO;
using System.Threading;

namespace Promete.Assets.Packaging;

/// <summary>
/// パッケージの中の無圧縮のエントリを、読み出し元から直接読む、読み取り専用で seek できるストリームです。
/// </summary>
/// <remarks>
/// <para>
/// 読み出し元の参照を 1 つ持ち、閉じたときに 1 回だけ返します (2 回目の Dispose は何もしない)。
/// 読み出し位置はストリームごとに持つので、同じ読み出し元から作った複数のストリームを、別々のスレッドから読めます (1 つのストリームはスレッド安全ではありません)。
/// </para>
/// <para>
/// 末尾より後ろへの seek はでき、そこでの読み出しは 0 を返します。先頭より前への seek は <see cref="IOException"/>、
/// 負の <see cref="Position"/> は <see cref="ArgumentOutOfRangeException"/>。閉じた後は <see cref="CanRead"/> / <see cref="CanSeek"/> が
/// <see langword="false"/> になり、読み出し・seek・<see cref="Length"/>・<see cref="Position"/> は <see cref="ObjectDisposedException"/> をスローします。
/// </para>
/// </remarks>
internal sealed class PackageEntryStream : Stream
{
    private readonly PackageStorage _storage;
    private readonly long _start;
    private readonly long _length;
    private long _position;
    private int _disposed;

    /// <summary>
    /// エントリを読むストリームを作ります。
    /// </summary>
    /// <param name="storage">読み出し元。参照を 1 つ増やした状態で渡すこと (このストリームが閉じるときに返す)。</param>
    /// <param name="start">エントリの先頭の、読み出し元での位置。</param>
    /// <param name="length">エントリの長さ。</param>
    public PackageEntryStream(PackageStorage storage, long start, long length)
    {
        ArgumentNullException.ThrowIfNull(storage);

        _storage = storage;
        _start = start;
        _length = length;
    }

    /// <inheritdoc />
    public override bool CanRead => Volatile.Read(ref _disposed) == 0;

    /// <inheritdoc />
    public override bool CanSeek => CanRead;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _length;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ThrowIfDisposed();
            _position = value;
        }
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (buffer.Length - offset < count)
            throw new ArgumentException("オフセットと数が配列の範囲を超えています。");

        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();

        var remaining = _length - _position;
        if (remaining <= 0 || buffer.IsEmpty)
            return 0;

        // エントリの末尾を越えて読まないように切り詰める
        var target = buffer.Length > remaining ? buffer[..(int)remaining] : buffer;
        var read = _storage.ReadAt(_start + _position, target);
        _position += read;
        return read;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();

        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        if (target < 0)
            throw new IOException("先頭より前には移動できません。");

        _position = target;
        return target;
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        throw new NotSupportedException("このストリームは読み取り専用です。");
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("このストリームは読み取り専用です。");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // 参照を返すのは 1 回だけ
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _storage.Release();

        base.Dispose(disposing);
    }

    /// <summary>
    /// 閉じられていたら <see cref="ObjectDisposedException"/> をスローします。
    /// </summary>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
}
