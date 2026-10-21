using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Promete.Assets.Infrastructure;

/// <summary>
/// マニフェストに載った「論理パス → 実ファイル」の対応だけを読める <see cref="IAssetSource"/> です。
/// MSBuild 統合 (<c>PrometeAsset</c>) の Folder モードで使います。
/// </summary>
/// <remarks>
/// <para>
/// <b>バックエンド実装者向けの型です。</b> <see cref="IAssetManifestLoader"/> の実装が、マニフェストを読んで作ります。
/// </para>
/// <para>
/// マニフェストに載っていないファイルは、実ファイルがあっても読めません。開発中 (Folder モード) にだけ読めてリリース (Package モード) で
/// 読めなくなる事故を防ぐためです。論理パスは大文字・小文字を区別し、マニフェストのキーと序数比較で照合します。
/// 大文字・小文字だけが違うパスで開こうとすると <see cref="AssetPathCaseMismatchException"/> をスローします。
/// </para>
/// <para>
/// <see cref="Enumerate"/> はマニフェストに載ったパスを返します (実ファイルの有無は確かめません)。
/// <see cref="Exists"/> は、マニフェストに載っていて、かつ実ファイルがある場合に <see langword="true"/> を返します。
/// </para>
/// <para>このクラスは不変で、どのスレッドから呼んでも安全です。</para>
/// </remarks>
public sealed class ManifestAssetSource : IFileBackedAssetSource
{
    /// <summary>
    /// <see cref="ManifestAssetSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="files">論理パスと、実ファイルの絶対パスの組。論理パスは <see cref="AssetPath.Normalize"/> で正規化します。</param>
    /// <param name="description">例外のメッセージに使う、マニフェストの説明 (ファイル名など)。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// 論理パスが正しくない場合、正規化すると同じになる論理パスが複数ある場合、実ファイルのパスが <see langword="null"/> または絶対パスでない場合。
    /// </exception>
    public ManifestAssetSource(IEnumerable<KeyValuePair<string, string>> files, string description)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <exception cref="AssetPathCaseMismatchException">マニフェストに無く、大文字・小文字だけが違う論理パスがある場合。</exception>
    /// <exception cref="FileNotFoundException">
    /// マニフェストに載っていない場合 (メッセージで <c>PrometeAsset</c> での宣言を案内します)。載っているが実ファイルが無い場合 (メッセージに実ファイルのパスを含みます)。
    /// </exception>
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
