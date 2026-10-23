using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Promete.Assets.Packaging;

/// <summary>
/// ファイルを、1 つのハンドルと <see cref="RandomAccess"/> で読む読み出し元です。読み出し位置を共有しないので、ロックが要りません。
/// </summary>
/// <remarks>
/// <para>
/// Windows では同期のハンドルへの I/O は直列化されるので (大きな読み込みの後ろで小さな読み込みが待たされる)、
/// <see cref="FileOptions.Asynchronous"/> で開きます。ブラウザ (<see cref="OperatingSystem.IsBrowser"/>) では <see cref="FileOptions.None"/> で開きます。
/// </para>
/// <para>ファイルは <see cref="FileShare.Read"/> と <see cref="FileShare.Delete"/> で開きます。ビルドがパッケージを一時ファイルから置き換えられるようにするためです (開いたハンドルは古い内容を読み続けます)。参照カウントが 0 になったらハンドルを閉じます。</para>
/// </remarks>
internal sealed class FilePackageStorage : PackageStorage
{
    private static readonly FileOptions FileOpenOptions = OperatingSystem.IsBrowser()
        ? FileOptions.None
        : FileOptions.Asynchronous;

    private readonly SafeFileHandle _handle;
    private readonly long _length;

    /// <summary>
    /// ファイルを開きます。
    /// </summary>
    /// <param name="fullPath">ファイルのフルパス。</param>
    /// <exception cref="FileNotFoundException">ファイルが存在しない場合。</exception>
    public FilePackageStorage(string fullPath)
    {
        var handle = File.OpenHandle(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            FileOpenOptions
        );

        try
        {
            _length = RandomAccess.GetLength(handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }

        _handle = handle;
    }

    /// <inheritdoc />
    public override long Length => _length;

    /// <inheritdoc />
    public override int ReadAt(long position, Span<byte> buffer) =>
        RandomAccess.Read(_handle, buffer, position);

    /// <inheritdoc />
    protected override void DisposeCore() => _handle.Dispose();
}
