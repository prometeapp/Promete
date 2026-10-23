namespace Promete.Assets.Packaging;

/// <summary>
/// アセットパッケージの形式の定数です。リーダー (Promete) とライター (Promete.AssetPacker) の両方からリンクしてコンパイルします。
/// </summary>
/// <remarks>
/// <para>
/// netstandard2.0 のライターと共有するため、定数と配列だけで書きます (<c>Span</c> や <c>BinaryPrimitives</c> は使いません)。
/// 形式の詳細は <c>Promete/Assets/PACKAGE_FORMAT.md</c> を参照してください。
/// </para>
/// <para>数値はすべてリトルエンディアンです。オフセットはパッケージの先頭からのバイト数です。</para>
/// </remarks>
internal static class AssetPackageFormat
{
    /// <summary>
    /// 形式のメジャーバージョン。リーダーは、これと違うメジャーバージョンを拒否します。
    /// </summary>
    public const ushort MajorVersion = 1;

    /// <summary>
    /// 形式のマイナーバージョン。リーダーは、どのマイナーバージョンも受け入れます。
    /// </summary>
    public const ushort MinorVersion = 0;

    /// <summary>
    /// ヘッダの大きさ (バイト)。マイナーバージョンでヘッダを延ばす場合も、リーダーはこれより小さいヘッダを拒否します。
    /// </summary>
    public const int HeaderSize = 64;

    /// <summary>
    /// nonce の大きさ (バイト)。ChaCha20 (RFC 8439) の nonce と同じ 12 バイトです。
    /// </summary>
    public const int NonceSize = 12;

    // ヘッダの各フィールドのオフセット
    public const int MagicOffset = 0;
    public const int MajorVersionOffset = 8;
    public const int MinorVersionOffset = 10;
    public const int HeaderSizeOffset = 12;
    public const int HeaderFlagsOffset = 16;
    public const int TocOffsetOffset = 24;
    public const int TocSizeOffset = 32;
    public const int TocNonceOffset = 40;

    /// <summary>
    /// ヘッダのフラグ: 目次が暗号化されている (Phase 5 の予約)。
    /// </summary>
    public const uint HeaderFlagTocEncrypted = 1u << 0;

    /// <summary>
    /// エントリのフラグ: データが Deflate (RFC 1951、zlib ヘッダなし) で圧縮されている。
    /// </summary>
    public const uint EntryFlagDeflate = 1u << 0;

    /// <summary>
    /// エントリのフラグ: データが暗号化されている (Phase 5 の予約)。
    /// </summary>
    public const uint EntryFlagEncrypted = 1u << 1;

    /// <summary>
    /// 目次のエントリ 1 件の、パスと拡張領域を除いた大きさ (バイト)。
    /// パスの長さ (2) + フラグ (4) + オフセット (8) + 格納サイズ (8) + 元のサイズ (8) + nonce (12) + 拡張領域の長さ (2)。
    /// </summary>
    public const int TocEntryFixedSize = 2 + 4 + 8 + 8 + 8 + NonceSize + 2;

    /// <summary>
    /// パスの UTF-8 での最大の長さ (バイト)。
    /// </summary>
    public const int MaxPathByteLength = ushort.MaxValue;

    /// <summary>
    /// ファイルの先頭のマジック (8 バイト): <c>"PRMTPAK"</c> と 0x1A。
    /// </summary>
    /// <returns>マジックのバイト列の新しい配列。</returns>
    public static byte[] GetMagic()
    {
        return new byte[] { 0x50, 0x52, 0x4D, 0x54, 0x50, 0x41, 0x4B, 0x1A };
    }
}
