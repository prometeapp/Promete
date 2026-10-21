using System;
using System.IO;

namespace Promete.Assets.Packaging;

/// <summary>
/// ファイルを、1 つのハンドルと <see cref="RandomAccess"/> で読む読み出し元です。読み出し位置を共有しないので、ロックが要りません。
/// </summary>
/// <remarks>
/// <para>
/// Windows では同期のハンドルへの I/O は直列化されるので (大きな読み込みの後ろで小さな読み込みが待たされる)、
/// <see cref="FileOptions.Asynchronous"/> で開きます。ブラウザ (<see cref="OperatingSystem.IsBrowser"/>) では <see cref="FileOptions.None"/> で開きます。
/// </para>
/// <para>ファイルは <see cref="FileShare.Read"/> で開きます。参照カウントが 0 になったらハンドルを閉じます。</para>
/// </remarks>
internal sealed class FilePackageStorage : PackageStorage
{
    /// <summary>
    /// ファイルを開きます。
    /// </summary>
    /// <param name="fullPath">ファイルのフルパス。</param>
    /// <exception cref="FileNotFoundException">ファイルが存在しない場合。</exception>
    public FilePackageStorage(string fullPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
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
