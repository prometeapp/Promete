using System;
using System.IO;

namespace Promete.Assets;

/// <summary>
/// 要求したアセットが見つからず、大文字・小文字だけが違うアセットがある場合にスローされる例外です。
/// </summary>
/// <remarks>
/// アセットのパスは、ファイルシステムが大文字・小文字を区別しない環境 (Windows / macOS の既定) でも区別します。
/// 開発中だけ読めてリリースで読めなくなる事故を防ぐためです。
/// </remarks>
public class AssetPathCaseMismatchException : FileNotFoundException
{
    /// <summary>
    /// <see cref="AssetPathCaseMismatchException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="requestedPath">要求した論理パス。</param>
    /// <param name="actualPath">大文字・小文字を無視すると一致する、実際の論理パス。</param>
    public AssetPathCaseMismatchException(string requestedPath, string actualPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 要求した論理パスを取得します。
    /// </summary>
    public string RequestedPath => throw new NotImplementedException();

    /// <summary>
    /// 大文字・小文字を無視すると一致する、実際の論理パスを取得します。
    /// </summary>
    public string ActualPath => throw new NotImplementedException();
}
