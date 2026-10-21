using System;
using System.IO;

namespace Promete.AssetPacker;

/// <summary>
/// アセットパッケージを書き出します。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Add"/> や <see cref="AddFile"/> でエントリを足し、<see cref="WriteTo(Stream)"/> か <see cref="WriteTo(string)"/> で書き出します。
/// 書き出したパッケージは、Promete の <c>PackageAssetSource</c> で読めます。形式は <c>Promete/Assets/PACKAGE_FORMAT.md</c> を参照してください。
/// </para>
/// <para>
/// 同じエントリの集合からは、足した順に関係なく同じバイト列を書き出します (エントリは論理パスの序数順に並べます)。
/// ただし Deflate の出力は実行環境の圧縮ライブラリに依存するので、.NET Framework と .NET のように実行環境が違うと、圧縮したエントリのバイト列は変わりえます。
/// </para>
/// <para>このクラスはスレッド安全ではありません。</para>
/// </remarks>
public sealed class AssetPackageWriter
{
    /// <summary>
    /// 書き出す形式のメジャーバージョン。
    /// </summary>
    public const int FormatMajorVersion = Promete.Assets.Packaging.AssetPackageFormat.MajorVersion;

    /// <summary>
    /// 書き出す形式のマイナーバージョン。
    /// </summary>
    public const int FormatMinorVersion = Promete.Assets.Packaging.AssetPackageFormat.MinorVersion;

    /// <summary>
    /// 足したエントリの数を取得します。
    /// </summary>
    public int Count => throw new NotImplementedException();

    /// <summary>
    /// <see cref="AssetCompression.Auto"/> のときに、圧縮を試みる対象かどうかを、拡張子で判定します。
    /// </summary>
    /// <remarks>
    /// 既に圧縮されている形式 (画像: png / jpg / jpeg / gif / webp / avif、音声: ogg / oga / opus / mp3 / m4a / aac / flac、
    /// 動画: mp4 / webm、フォント: woff / woff2、アーカイブ: zip / gz / 7z) と、
    /// 展開せずに seek して読みたい wav は、圧縮しません。拡張子の大文字・小文字は区別しません。それ以外は圧縮を試みます。
    /// </remarks>
    /// <param name="path">論理パスまたはファイル名。</param>
    /// <returns>圧縮を試みる場合は <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が <see langword="null"/> の場合。</exception>
    public static bool IsAutoCompressed(string path)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// メモリ上のバイト列をエントリとして足します。
    /// </summary>
    /// <param name="path">論理パス。<see cref="AssetPackagePath.Normalize"/> で正規化して格納します。</param>
    /// <param name="content">中身。配列はコピーせずに保持するので、書き出すまで変更しないでください。</param>
    /// <param name="compression">圧縮の方法。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> または <paramref name="content"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> が論理パスとして正しくない場合 (<see cref="AssetPackagePath.Normalize"/>)。
    /// 正規化したパスの UTF-8 が 65535 バイトを超える場合。正規化すると既に足したエントリと同じパスになる場合。
    /// <paramref name="compression"/> が定義されていない値の場合。
    /// </exception>
    public void Add(
        string path,
        byte[] content,
        AssetCompression compression = AssetCompression.Auto
    )
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// ファイルをエントリとして足します。ファイルの中身は書き出すときに読みます。
    /// </summary>
    /// <param name="path">論理パス。<see cref="AssetPackagePath.Normalize"/> で正規化して格納します。</param>
    /// <param name="sourceFilePath">中身のファイルのパス。相対パスはカレントディレクトリを基準に解決します。</param>
    /// <param name="compression">圧縮の方法。</param>
    /// <exception cref="ArgumentNullException">引数が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> が論理パスとして正しくない場合。正規化すると既に足したエントリと同じパスになる場合。
    /// <paramref name="compression"/> が定義されていない値の場合。
    /// </exception>
    /// <exception cref="FileNotFoundException"><paramref name="sourceFilePath"/> のファイルが存在しない場合。</exception>
    public void AddFile(
        string path,
        string sourceFilePath,
        AssetCompression compression = AssetCompression.Auto
    )
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// パッケージをストリームの現在位置から書き出します。書き出した後、ストリームの位置はパッケージの末尾になります。ストリームは閉じません。
    /// </summary>
    /// <remarks>
    /// ヘッダを最後に書き直すため、ストリームは seek できる必要があります。パッケージ内のオフセットは、書き出し始めた位置からの相対値です。
    /// </remarks>
    /// <param name="output">書き出し先。書き込みと seek ができること。</param>
    /// <exception cref="ArgumentNullException"><paramref name="output"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException"><paramref name="output"/> が書き込みか seek をできない場合。</exception>
    /// <exception cref="FileNotFoundException"><see cref="AddFile"/> で足したファイルが、書き出す時点で存在しない場合。</exception>
    public void WriteTo(Stream output)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// パッケージをファイルに書き出します。既にあるファイルは上書きします。
    /// </summary>
    /// <remarks>
    /// 同じフォルダの一時ファイルに書いてから置き換えます。途中で失敗した場合、書きかけのファイルは残さず、既にあったファイルもそのまま残します。
    /// </remarks>
    /// <param name="outputPath">書き出すファイルのパス。フォルダは存在している必要があります。</param>
    /// <exception cref="ArgumentNullException"><paramref name="outputPath"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="FileNotFoundException"><see cref="AddFile"/> で足したファイルが、書き出す時点で存在しない場合。</exception>
    public void WriteTo(string outputPath)
    {
        throw new NotImplementedException();
    }
}
