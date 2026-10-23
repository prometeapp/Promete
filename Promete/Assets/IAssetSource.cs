using System.Collections.Generic;
using System.IO;

namespace Promete.Assets;

/// <summary>
/// 論理パスでアセットを読み出す、1 つの読み込み元を表します。
/// </summary>
/// <remarks>
/// <para>
/// 論理パスは <c>/</c> 区切りで、大文字・小文字を区別します。規則は <see cref="AssetPath"/> を参照してください。
/// <see cref="AssetFileSystem"/> は、<see cref="AssetPath.Normalize"/> で正規化したパスを渡します。
/// 実装は、利用者から直接呼ばれた場合に備えて、自分でも正規化してください (正規化は冪等です)。
/// </para>
/// <para>
/// 実装は、どのスレッドから呼ばれても安全である必要があります。
/// </para>
/// <para>
/// <see cref="AssetFileSystem"/> は、Unmount や Dispose の直後に、読み込み中の別スレッドからこのソースのメンバーを呼ぶことがあります。
/// 破棄された後に呼ばれたら <see cref="System.ObjectDisposedException"/> を投げてかまいません (<see cref="AssetFileSystem"/> はそのソースに無いものとして扱います)。
/// 既に開いたストリームは、閉じられるまで有効にしておいてください。
/// </para>
/// </remarks>
public interface IAssetSource
{
    /// <summary>
    /// 指定した論理パスのファイルが存在するかどうかを取得します。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <returns>
    /// 存在する場合は <see langword="true"/>。大文字・小文字だけが違う場合や、ディレクトリを指す場合は <see langword="false"/>。
    /// </returns>
    public bool Exists(string path);

    /// <summary>
    /// 指定した論理パスのファイルを読み取り用に開きます。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <returns>読み取り用のストリーム。常に seek できる (<see cref="Stream.CanSeek"/> が <see langword="true"/>) ストリームを返します。</returns>
    /// <exception cref="AssetPathCaseMismatchException">大文字・小文字だけが違うファイルがある場合。</exception>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合。</exception>
    public Stream OpenRead(string path);

    /// <summary>
    /// 指定したディレクトリにあるファイルの論理パスを列挙します。
    /// </summary>
    /// <param name="directory">ディレクトリの論理パス。空文字列または <c>/</c> はルートを表します。</param>
    /// <param name="pattern">ファイル名に対するパターン。<c>*</c> と <c>?</c> を使えます。大文字・小文字を区別します。</param>
    /// <param name="recursive">サブディレクトリも含めて列挙する場合は <see langword="true"/>。</param>
    /// <returns>ファイルの論理パス (先頭の <c>/</c> なし) の列挙。ディレクトリは含みません。ディレクトリが無い場合は空です。</returns>
    public IEnumerable<string> Enumerate(string directory, string pattern, bool recursive);
}
