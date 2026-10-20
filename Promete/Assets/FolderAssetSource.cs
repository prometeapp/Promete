using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Promete.Assets;

/// <summary>
/// 実際のフォルダの中身を、論理パスで読み出す <see cref="IAssetSource"/> です。
/// </summary>
/// <remarks>
/// <para>
/// ファイルシステムが大文字・小文字を区別しない環境 (Windows / macOS の既定) でも、論理パスの大文字・小文字を区別します。
/// パスの各要素 (ディレクトリを含む) を、ディレクトリを列挙して得た実際の名前と序数比較で照合します。
/// 大文字・小文字だけが違うファイルを開こうとすると、<see cref="AssetPathCaseMismatchException"/> をスローします。
/// </para>
/// <para>
/// ディレクトリごとの名前の一覧はキャッシュします。見つからなかったときは、そのディレクトリを列挙し直してから判断するので、
/// 実行中に追加したファイルも読めます。
/// </para>
/// </remarks>
public sealed class FolderAssetSource : IFileBackedAssetSource
{
    /// <summary>
    /// <see cref="FolderAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="rootPath">読み出すフォルダのパス。相対パスは <see cref="AppContext.BaseDirectory"/> を基準に解決します。</param>
    /// <param name="mountPoint">このフォルダを置く論理パスの接頭辞。例えば <c>"assets"</c> を指定すると、フォルダ直下の <c>x.png</c> は <c>"assets/x.png"</c> になります。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="mountPoint"/> が <c>\</c> を含む場合。</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="rootPath"/> のフォルダが存在しない場合。</exception>
    public FolderAssetSource(string rootPath, string mountPoint = "")
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 読み出すフォルダのフルパスを取得します。
    /// </summary>
    public string RootPath => throw new NotImplementedException();

    /// <summary>
    /// 正規化した論理パスの接頭辞を取得します。接頭辞が無い場合は空文字列です。
    /// </summary>
    public string MountPoint => throw new NotImplementedException();

    /// <inheritdoc />
    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath)
    {
        throw new NotImplementedException();
    }
}
