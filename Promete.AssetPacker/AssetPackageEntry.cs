namespace Promete.AssetPacker;

/// <summary>
/// <see cref="AssetPackageWriter"/> に足されたエントリ 1 件。中身は配列か、読み出すファイルのフルパスのどちらか一方を持ちます。
/// </summary>
internal sealed class AssetPackageEntry
{
    /// <summary>
    /// 初期化します。
    /// </summary>
    /// <param name="logicalPath">正規化した論理パス。</param>
    /// <param name="compression">圧縮の方法。</param>
    /// <param name="content">メモリ上の中身。ファイルから足した場合は <see langword="null"/>。</param>
    /// <param name="sourceFilePath">中身のファイルのフルパス。メモリ上の中身の場合は <see langword="null"/>。</param>
    public AssetPackageEntry(
        string logicalPath,
        AssetCompression compression,
        byte[]? content,
        string? sourceFilePath
    )
    {
        LogicalPath = logicalPath;
        Compression = compression;
        Content = content;
        SourceFilePath = sourceFilePath;
    }

    /// <summary>
    /// 正規化した論理パスを取得します。
    /// </summary>
    public string LogicalPath { get; }

    /// <summary>
    /// 圧縮の方法を取得します。
    /// </summary>
    public AssetCompression Compression { get; }

    /// <summary>
    /// メモリ上の中身を取得します。ファイルから足した場合は <see langword="null"/> です。
    /// </summary>
    public byte[]? Content { get; }

    /// <summary>
    /// 中身のファイルのフルパスを取得します。メモリ上の中身の場合は <see langword="null"/> です。
    /// </summary>
    public string? SourceFilePath { get; }
}
