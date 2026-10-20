using System.Diagnostics.CodeAnalysis;

namespace Promete.Assets;

/// <summary>
/// 実ファイルを持つ <see cref="IAssetSource"/> が、任意で実装するインターフェースです。
/// </summary>
/// <remarks>
/// ストリームではなくファイルのパスを要求する API (Web のフォントの読み込みなど) に、アセットを渡すために使います。
/// </remarks>
public interface IFileBackedAssetSource : IAssetSource
{
    /// <summary>
    /// 指定した論理パスに対応する実ファイルのフルパスを取得します。
    /// </summary>
    /// <param name="path">ファイルの論理パス。</param>
    /// <param name="fullPath">実ファイルのフルパス。見つからない場合は <see langword="null"/>。</param>
    /// <returns>見つかった場合は <see langword="true"/>。大文字・小文字だけが違う場合は <see langword="false"/>。</returns>
    public bool TryGetFilePath(string path, [NotNullWhen(true)] out string? fullPath);
}
