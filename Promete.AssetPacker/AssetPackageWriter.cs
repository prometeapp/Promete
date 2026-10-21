using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Promete.Assets.Packaging;

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
    /// 拡張子で圧縮を試みない形式 (既に圧縮されている形式と、seek して読みたい wav)。大文字・小文字は区別しません。
    /// </summary>
    private static readonly HashSet<string> NoAutoCompressionExtensions = new HashSet<string>(
        new[]
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".webp",
            ".avif",
            ".ogg",
            ".oga",
            ".opus",
            ".mp3",
            ".m4a",
            ".aac",
            ".flac",
            ".wav",
            ".mp4",
            ".webm",
            ".woff",
            ".woff2",
            ".zip",
            ".gz",
            ".7z",
        },
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>
    /// 正規化した論理パスをキーにした、足したエントリ。序数比較なので、大文字・小文字だけ違うパスは別のエントリです。
    /// </summary>
    private readonly Dictionary<string, AssetPackageEntry> _entries = new Dictionary<
        string,
        AssetPackageEntry
    >(StringComparer.Ordinal);

    /// <summary>
    /// 足したエントリの数を取得します。
    /// </summary>
    public int Count => _entries.Count;

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
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var fileName = path.Substring(path.LastIndexOf('/') + 1);
        var dotIndex = fileName.LastIndexOf('.');
        if (dotIndex < 0)
        {
            return true;
        }

        return !NoAutoCompressionExtensions.Contains(fileName.Substring(dotIndex));
    }

    /// <summary>
    /// メモリ上のバイト列をエントリとして足します。
    /// </summary>
    /// <param name="path">論理パス。<see cref="AssetPackagePath.Normalize(string)"/> で正規化して格納します。</param>
    /// <param name="content">中身。配列はコピーせずに保持するので、書き出すまで変更しないでください。</param>
    /// <param name="compression">圧縮の方法。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> または <paramref name="content"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> が論理パスとして正しくない場合 (<see cref="AssetPackagePath.Normalize(string)"/>)。
    /// 正規化したパスの UTF-8 が 65535 バイトを超える場合。正規化すると既に足したエントリと同じパスになる場合。
    /// <paramref name="compression"/> が定義されていない値の場合。
    /// </exception>
    public void Add(
        string path,
        byte[] content,
        AssetCompression compression = AssetCompression.Auto
    )
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        AddEntry(path, compression, content, null);
    }

    /// <summary>
    /// ファイルをエントリとして足します。ファイルの中身は書き出すときに読みます。
    /// </summary>
    /// <param name="path">論理パス。<see cref="AssetPackagePath.Normalize(string)"/> で正規化して格納します。</param>
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
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (sourceFilePath is null)
        {
            throw new ArgumentNullException(nameof(sourceFilePath));
        }

        var fullSourcePath = Path.GetFullPath(sourceFilePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException(
                $"パッケージに足すファイルが見つかりません: {fullSourcePath}",
                fullSourcePath
            );
        }

        AddEntry(path, compression, null, fullSourcePath);
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
        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (!output.CanWrite || !output.CanSeek)
        {
            throw new ArgumentException(
                "書き込みと seek ができるストリームが必要です。",
                nameof(output)
            );
        }

        var start = output.Position;
        output.Write(new byte[AssetPackageFormat.HeaderSize], 0, AssetPackageFormat.HeaderSize);

        var keys = new List<string>(_entries.Keys);
        keys.Sort(StringComparer.Ordinal);

        var records = new List<TocRecord>(keys.Count);
        foreach (var key in keys)
        {
            var entry = _entries[key];
            var content = entry.Content ?? File.ReadAllBytes(entry.SourceFilePath!);
            var (stored, flags) = Encode(entry, content);

            records.Add(
                new TocRecord(
                    Encoding.UTF8.GetBytes(entry.LogicalPath),
                    flags,
                    (ulong)(output.Position - start),
                    (ulong)stored.Length,
                    (ulong)content.Length
                )
            );
            output.Write(stored, 0, stored.Length);
        }

        var toc = BuildToc(records);
        var tocOffset = (ulong)(output.Position - start);
        output.Write(toc, 0, toc.Length);
        var end = output.Position;

        var header = BuildHeader(tocOffset, (ulong)toc.Length);
        output.Position = start;
        output.Write(header, 0, header.Length);
        output.Position = end;
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
        if (outputPath is null)
        {
            throw new ArgumentNullException(nameof(outputPath));
        }

        var fullPath = Path.GetFullPath(outputPath);
        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                WriteTo(file);
            }

            if (File.Exists(fullPath))
            {
                File.Replace(tempPath, fullPath, null);
            }
            else
            {
                File.Move(tempPath, fullPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// 中身を圧縮の方法に従って符号化します。圧縮した場合は、格納するバイト列とフラグを返します。
    /// </summary>
    /// <param name="entry">エントリ。</param>
    /// <param name="content">元の中身。</param>
    /// <returns>格納するバイト列と、エントリのフラグ。</returns>
    private static (byte[] Stored, uint Flags) Encode(AssetPackageEntry entry, byte[] content)
    {
        switch (entry.Compression)
        {
            case AssetCompression.Always:
                return (Deflate(content), AssetPackageFormat.EntryFlagDeflate);

            case AssetCompression.Auto when IsAutoCompressed(entry.LogicalPath):
                var deflated = Deflate(content);
                if (deflated.Length < content.Length)
                {
                    return (deflated, AssetPackageFormat.EntryFlagDeflate);
                }

                return (content, 0);

            default:
                return (content, 0);
        }
    }

    /// <summary>
    /// バイト列を raw Deflate で圧縮します。
    /// </summary>
    /// <param name="content">圧縮するバイト列。</param>
    /// <returns>圧縮したバイト列。</returns>
    private static byte[] Deflate(byte[] content)
    {
        using var compressed = new MemoryStream();
        using (
            var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true)
        )
        {
            deflate.Write(content, 0, content.Length);
        }

        return compressed.ToArray();
    }

    /// <summary>
    /// 目次のバイト列を組み立てます。エントリは渡された順に並べます。
    /// </summary>
    /// <param name="records">目次の各エントリ。</param>
    /// <returns>目次のバイト列 (エントリ数の 4 バイトを含む)。拡張領域と目次の余りはありません。</returns>
    private static byte[] BuildToc(List<TocRecord> records)
    {
        var size = 4;
        foreach (var record in records)
        {
            size += AssetPackageFormat.TocEntryFixedSize + record.PathBytes.Length;
        }

        var toc = new byte[size];
        WriteUInt32(toc, 0, (uint)records.Count);

        var position = 4;
        foreach (var record in records)
        {
            WriteUInt16(toc, position, (ushort)record.PathBytes.Length);
            position += 2;
            Array.Copy(record.PathBytes, 0, toc, position, record.PathBytes.Length);
            position += record.PathBytes.Length;
            WriteUInt32(toc, position, record.Flags);
            position += 4;
            WriteUInt64(toc, position, record.Offset);
            position += 8;
            WriteUInt64(toc, position, record.StoredSize);
            position += 8;
            WriteUInt64(toc, position, record.OriginalSize);
            position += 8;

            // nonce (12 バイト) は 0 のまま。extraLength も 0 のまま。
            position += AssetPackageFormat.NonceSize + 2;
        }

        return toc;
    }

    /// <summary>
    /// ヘッダのバイト列を組み立てます。暗号化はせず、予約の領域は 0 のままです。
    /// </summary>
    /// <param name="tocOffset">目次の位置 (パッケージの先頭からの相対値)。</param>
    /// <param name="tocSize">目次の大きさ。</param>
    /// <returns>ヘッダのバイト列 (<see cref="AssetPackageFormat.HeaderSize"/> バイト)。</returns>
    private static byte[] BuildHeader(ulong tocOffset, ulong tocSize)
    {
        var header = new byte[AssetPackageFormat.HeaderSize];
        Array.Copy(AssetPackageFormat.GetMagic(), 0, header, AssetPackageFormat.MagicOffset, 8);
        WriteUInt16(header, AssetPackageFormat.MajorVersionOffset, AssetPackageFormat.MajorVersion);
        WriteUInt16(header, AssetPackageFormat.MinorVersionOffset, AssetPackageFormat.MinorVersion);
        WriteUInt32(
            header,
            AssetPackageFormat.HeaderSizeOffset,
            (uint)AssetPackageFormat.HeaderSize
        );
        WriteUInt32(header, AssetPackageFormat.HeaderFlagsOffset, 0);
        WriteUInt64(header, AssetPackageFormat.TocOffsetOffset, tocOffset);
        WriteUInt64(header, AssetPackageFormat.TocSizeOffset, tocSize);
        return header;
    }

    /// <summary>
    /// 2 バイトの値をリトルエンディアンで書き込みます。
    /// </summary>
    /// <param name="buffer">書き込み先。</param>
    /// <param name="offset">書き込み位置。</param>
    /// <param name="value">値。</param>
    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 4 バイトの値をリトルエンディアンで書き込みます。
    /// </summary>
    /// <param name="buffer">書き込み先。</param>
    /// <param name="offset">書き込み位置。</param>
    /// <param name="value">値。</param>
    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        for (var i = 0; i < 4; i++)
        {
            buffer[offset + i] = (byte)(value >> (8 * i));
        }
    }

    /// <summary>
    /// 8 バイトの値をリトルエンディアンで書き込みます。
    /// </summary>
    /// <param name="buffer">書き込み先。</param>
    /// <param name="offset">書き込み位置。</param>
    /// <param name="value">値。</param>
    private static void WriteUInt64(byte[] buffer, int offset, ulong value)
    {
        for (var i = 0; i < 8; i++)
        {
            buffer[offset + i] = (byte)(value >> (8 * i));
        }
    }

    /// <summary>
    /// 検証してエントリを登録します。どこかで例外になった場合は何も登録しません。
    /// </summary>
    /// <param name="path">正規化する前の論理パス。</param>
    /// <param name="compression">圧縮の方法。</param>
    /// <param name="content">メモリ上の中身。ファイルから足す場合は <see langword="null"/>。</param>
    /// <param name="sourceFilePath">中身のファイルのフルパス。メモリ上の中身の場合は <see langword="null"/>。</param>
    /// <exception cref="ArgumentException">圧縮の方法、パス、重複のいずれかが不正な場合。</exception>
    private void AddEntry(
        string path,
        AssetCompression compression,
        byte[]? content,
        string? sourceFilePath
    )
    {
        if (!Enum.IsDefined(typeof(AssetCompression), compression))
        {
            throw new ArgumentException("未定義の圧縮の方法です。", nameof(compression));
        }

        var logicalPath = AssetPackagePath.Normalize(path);
        if (Encoding.UTF8.GetByteCount(logicalPath) > AssetPackageFormat.MaxPathByteLength)
        {
            throw new ArgumentException(
                $"論理パスの UTF-8 のバイト数が {AssetPackageFormat.MaxPathByteLength} を超えています: {path}",
                nameof(path)
            );
        }

        if (_entries.ContainsKey(logicalPath))
        {
            throw new ArgumentException(
                $"同じ論理パスのエントリが既にあります。正規化したパス: {logicalPath} (指定されたパス: {path})",
                nameof(path)
            );
        }

        _entries.Add(
            logicalPath,
            new AssetPackageEntry(logicalPath, compression, content, sourceFilePath)
        );
    }

    /// <summary>
    /// 目次に書く、エントリ 1 件分の情報。
    /// </summary>
    private sealed class TocRecord
    {
        /// <summary>
        /// 初期化します。
        /// </summary>
        /// <param name="pathBytes">論理パスの UTF-8 のバイト列。</param>
        /// <param name="flags">エントリのフラグ。</param>
        /// <param name="offset">データの位置 (パッケージの先頭からの相対値)。</param>
        /// <param name="storedSize">格納しているバイト数。</param>
        /// <param name="originalSize">元のバイト数。</param>
        public TocRecord(
            byte[] pathBytes,
            uint flags,
            ulong offset,
            ulong storedSize,
            ulong originalSize
        )
        {
            PathBytes = pathBytes;
            Flags = flags;
            Offset = offset;
            StoredSize = storedSize;
            OriginalSize = originalSize;
        }

        /// <summary>
        /// 論理パスの UTF-8 のバイト列を取得します。
        /// </summary>
        public byte[] PathBytes { get; }

        /// <summary>
        /// エントリのフラグを取得します。
        /// </summary>
        public uint Flags { get; }

        /// <summary>
        /// データの位置を取得します。
        /// </summary>
        public ulong Offset { get; }

        /// <summary>
        /// 格納しているバイト数を取得します。
        /// </summary>
        public ulong StoredSize { get; }

        /// <summary>
        /// 元のバイト数を取得します。
        /// </summary>
        public ulong OriginalSize { get; }
    }
}
