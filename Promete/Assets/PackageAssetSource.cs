using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Promete.Assets;

/// <summary>
/// アセットパッケージ (Promete.AssetPacker の <c>AssetPackageWriter</c> で書き出したもの) を読む <see cref="IAssetSource"/> です。
/// </summary>
/// <remarks>
/// <para>
/// ファイル、埋め込みリソース、任意の <see cref="Stream"/> から開けます。開くときに読むのはヘッダと目次だけで、
/// 各エントリの中身は <see cref="OpenRead"/> したときに、必要な部分だけを読みます (パッケージ全体をメモリに載せません)。
/// </para>
/// <para>
/// <see cref="OpenRead"/> は常に seek できるストリームを返します。無圧縮のエントリは、パッケージの一部を直接読むストリームを、
/// 圧縮したエントリは、展開した <see cref="MemoryStream"/> を返します。
/// 複数のストリームを同時に開いて、別々のスレッドから読めます。
/// </para>
/// <para>
/// <see cref="Dispose"/> の後も、既に開いたストリームは閉じられるまで有効です。パッケージのファイルや元のストリームは、
/// このソースと、開いたストリームのすべてが閉じられた時点で閉じます。
/// </para>
/// <para>
/// パッケージが壊れている場合は <see cref="InvalidDataException"/> を、対応していない形式のバージョンや機能 (暗号化など) の場合は
/// <see cref="NotSupportedException"/> をスローします。
/// </para>
/// </remarks>
public sealed class PackageAssetSource : IAssetSource, IDisposable
{
    /// <summary>
    /// ファイルからパッケージを開きます。
    /// </summary>
    /// <param name="filePath">パッケージのファイルのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが存在しない場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。メッセージにファイルのパスを含みます。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public PackageAssetSource(string filePath, string mountPoint = "")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// ストリームからパッケージを開きます。ストリームの現在位置を、パッケージの先頭として扱います。
    /// </summary>
    /// <remarks>
    /// seek できないストリームは、開くときに全体をメモリに読み込みます (<paramref name="leaveOpen"/> が <see langword="false"/> なら、読み込んだ直後に閉じます)。
    /// seek できるストリームは、このソースが読み出し位置を管理するので、開いた後は他から読み書きしないでください。
    /// </remarks>
    /// <param name="stream">パッケージを読むストリーム。読み取りができること。</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> なら、<paramref name="stream"/> を閉じません。
    /// <see langword="false"/> なら、このソースと開いたストリームのすべてが閉じられた時点で閉じます。構築に失敗した場合はその場で閉じます。
    /// </param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> が読み取りをできない場合。<paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public PackageAssetSource(Stream stream, bool leaveOpen = false, string mountPoint = "")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// アセンブリの埋め込みリソースからパッケージを開きます。
    /// </summary>
    /// <param name="assembly">リソースを持つアセンブリ。</param>
    /// <param name="resourceName">リソースの名前 (<c>LogicalName</c>)。</param>
    /// <param name="mountPoint">論理パスの前に付ける接頭辞。</param>
    /// <returns>開いたソース。</returns>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="FileNotFoundException">リソースが見つからない場合。</exception>
    /// <exception cref="InvalidDataException">パッケージが壊れている場合。メッセージにリソースの名前を含みます。</exception>
    /// <exception cref="NotSupportedException">対応していない形式のバージョンや機能の場合。</exception>
    public static PackageAssetSource FromEmbeddedResource(
        Assembly assembly,
        string resourceName,
        string mountPoint = ""
    )
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <exception cref="InvalidDataException">圧縮したエントリのデータが壊れている場合。</exception>
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public Stream OpenRead(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">破棄された後に呼び出した場合。</exception>
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// このソースを破棄します。既に開いたストリームは、閉じられるまで有効です。2 回目以降の呼び出しは何もしません。
    /// </summary>
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
