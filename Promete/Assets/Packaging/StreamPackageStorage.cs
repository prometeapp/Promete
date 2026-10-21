using System;
using System.IO;

namespace Promete.Assets.Packaging;

/// <summary>
/// seek できるストリームを、ロックをかけて読む読み出し元です。埋め込みリソースや、利用者が渡した任意のストリームに使います。
/// </summary>
/// <remarks>
/// 構築した時点のストリームの位置を、読み出し元の先頭 (位置 0) として扱います。
/// 読むたびに、ロックの中で位置を合わせてから読みます。参照カウントが 0 になったら、<c>leaveOpen</c> が <see langword="false"/> ならストリームを閉じます。
/// </remarks>
internal sealed class StreamPackageStorage : PackageStorage
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly long _origin;
    private readonly long _length;
    private readonly object _gate = new();

    /// <summary>
    /// ストリームを読み出し元にします。
    /// </summary>
    /// <param name="stream">seek と読み取りのできるストリーム。</param>
    /// <param name="leaveOpen"><see langword="true"/> なら、参照カウントが 0 になってもストリームを閉じません。</param>
    public StreamPackageStorage(Stream stream, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;
        _leaveOpen = leaveOpen;
        _origin = stream.Position;
        _length = stream.Length - _origin;
    }

    /// <inheritdoc />
    /// <remarks>構築した時点の位置から末尾までの長さです。</remarks>
    public override long Length => _length;

    /// <inheritdoc />
    public override int ReadAt(long position, Span<byte> buffer)
    {
        lock (_gate)
        {
            _stream.Position = _origin + position;
            return _stream.Read(buffer);
        }
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        if (!_leaveOpen)
            _stream.Dispose();
    }
}
