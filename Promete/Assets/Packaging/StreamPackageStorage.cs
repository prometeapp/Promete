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
    /// <summary>
    /// ストリームを読み出し元にします。
    /// </summary>
    /// <param name="stream">seek と読み取りのできるストリーム。</param>
    /// <param name="leaveOpen"><see langword="true"/> なら、参照カウントが 0 になってもストリームを閉じません。</param>
    public StreamPackageStorage(Stream stream, bool leaveOpen)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <remarks>構築した時点の位置から末尾までの長さです。</remarks>
    public override long Length => throw new NotImplementedException();

    /// <inheritdoc />
    public override int ReadAt(long position, Span<byte> buffer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        throw new NotImplementedException();
    }
}
