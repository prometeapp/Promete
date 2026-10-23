namespace Promete.AssetPacker;

/// <summary>
/// アセットパッケージに格納するときの、エントリの圧縮の方法を指定します。MSBuild の <c>Compress</c> メタデータ (<c>auto</c> / <c>true</c> / <c>false</c>) に対応します。
/// </summary>
public enum AssetCompression
{
    /// <summary>
    /// 拡張子で判定します (<see cref="AssetPackageWriter.IsAutoCompressed"/>)。
    /// 圧縮の対象でも、圧縮しても小さくならない場合は無圧縮で格納します。
    /// </summary>
    Auto,

    /// <summary>
    /// 常に Deflate で圧縮します。小さくならない場合も圧縮したまま格納します。
    /// </summary>
    Always,

    /// <summary>
    /// 圧縮しません。
    /// </summary>
    Never,
}
