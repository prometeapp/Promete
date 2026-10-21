using System;
using System.IO;

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
    /// <summary>
    /// エントリを読むストリームを作ります。
    /// </summary>
    /// <param name="storage">読み出し元。参照を 1 つ増やした状態で渡すこと (このストリームが閉じるときに返す)。</param>
    /// <param name="start">エントリの先頭の、読み出し元での位置。</param>
    /// <param name="length">エントリの長さ。</param>
    public PackageEntryStream(PackageStorage storage, long start, long length)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool CanRead => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool CanSeek => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotImplementedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
