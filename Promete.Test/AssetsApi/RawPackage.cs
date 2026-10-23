using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Promete.Test.AssetsApi;

/// <summary>
/// アセットパッケージの形式 (Promete/Assets/PACKAGE_FORMAT.md) を、テストの側で独立に実装したもの。
/// ライターの出力の検査 (<see cref="RawPackage.Parse"/>) と、リーダーに渡す細工したパッケージの作成 (<see cref="RawPackageBuilder"/>) に使う。
/// 本体の定数 (AssetPackageFormat) は使わず、仕様書の値を直接書く。
/// </summary>
internal static class RawPackageLayout
{
    public const int HeaderSize = 64;
    public const int NonceSize = 12;
    public const uint HeaderFlagTocEncrypted = 1;
    public const uint EntryFlagDeflate = 1;
    public const uint EntryFlagEncrypted = 2;

    public static byte[] Magic => [0x50, 0x52, 0x4D, 0x54, 0x50, 0x41, 0x4B, 0x1A];

    /// <summary>
    /// raw Deflate (zlib ヘッダなし) で圧縮する。
    /// </summary>
    public static byte[] Deflate(byte[] data)
    {
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.Optimal, true))
            deflate.Write(data);
        return buffer.ToArray();
    }

    /// <summary>
    /// raw Deflate を展開する。
    /// </summary>
    public static byte[] Inflate(byte[] data)
    {
        using var deflate = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// 圧縮が効く (Auto でも圧縮される) テキスト。
    /// </summary>
    public static byte[] CompressibleText(string seed = "promete", int repeat = 200) =>
        Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(seed + " asset package. ", repeat)));

    /// <summary>
    /// 圧縮が効かない乱数のバイト列。
    /// </summary>
    public static byte[] RandomBytes(int length, int seed = 1)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }
}

/// <summary>
/// パースしたパッケージ。
/// </summary>
internal sealed class RawPackage
{
    public required byte[] Bytes { get; init; }

    public required byte[] Magic { get; init; }

    public ushort Major { get; init; }

    public ushort Minor { get; init; }

    public uint HeaderSize { get; init; }

    public uint Flags { get; init; }

    /// <summary>
    /// ヘッダの 20〜23 バイト目 (予約)。
    /// </summary>
    public uint Reserved20 { get; init; }

    public ulong TocOffset { get; init; }

    public ulong TocSize { get; init; }

    public required byte[] TocNonce { get; init; }

    /// <summary>
    /// ヘッダの 52〜63 バイト目 (予約)。
    /// </summary>
    public required byte[] Reserved52 { get; init; }

    public required List<RawTocEntry> Entries { get; init; }

    /// <summary>
    /// 目次の最後のエントリの後ろに残ったバイト数。
    /// </summary>
    public int TocTrailingLength { get; init; }

    /// <summary>
    /// パッケージのバイト列 (<paramref name="start"/> から末尾まで) をパースする。形式に合わなければ例外を投げる。
    /// </summary>
    public static RawPackage Parse(byte[] bytes, int start = 0)
    {
        var package = bytes.AsSpan(start);
        var span = package;
        var tocOffset = BinaryPrimitives.ReadUInt64LittleEndian(span[24..]);
        var tocSize = BinaryPrimitives.ReadUInt64LittleEndian(span[32..]);
        var toc = package.Slice(checked((int)tocOffset), checked((int)tocSize));
        var count = BinaryPrimitives.ReadUInt32LittleEndian(toc);
        var p = 4;
        var entries = new List<RawTocEntry>();
        for (var i = 0; i < count; i++)
        {
            var pathLength = BinaryPrimitives.ReadUInt16LittleEndian(toc[p..]);
            p += 2;
            var pathBytes = toc.Slice(p, pathLength).ToArray();
            p += pathLength;
            var flags = BinaryPrimitives.ReadUInt32LittleEndian(toc[p..]);
            p += 4;
            var offset = BinaryPrimitives.ReadUInt64LittleEndian(toc[p..]);
            p += 8;
            var stored = BinaryPrimitives.ReadUInt64LittleEndian(toc[p..]);
            p += 8;
            var original = BinaryPrimitives.ReadUInt64LittleEndian(toc[p..]);
            p += 8;
            var nonce = toc.Slice(p, 12).ToArray();
            p += 12;
            var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(toc[p..]);
            p += 2;
            var extra = toc.Slice(p, extraLength).ToArray();
            p += extraLength;
            entries.Add(
                new RawTocEntry
                {
                    Path = Encoding.UTF8.GetString(pathBytes),
                    PathBytes = pathBytes,
                    Flags = flags,
                    Offset = offset,
                    StoredSize = stored,
                    OriginalSize = original,
                    Nonce = nonce,
                    Extra = extra,
                    StoredData = package
                        .Slice(checked((int)offset), checked((int)stored))
                        .ToArray(),
                }
            );
        }

        return new RawPackage
        {
            Bytes = package.ToArray(),
            Magic = span[..8].ToArray(),
            Major = BinaryPrimitives.ReadUInt16LittleEndian(span[8..]),
            Minor = BinaryPrimitives.ReadUInt16LittleEndian(span[10..]),
            HeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(span[12..]),
            Flags = BinaryPrimitives.ReadUInt32LittleEndian(span[16..]),
            Reserved20 = BinaryPrimitives.ReadUInt32LittleEndian(span[20..]),
            TocOffset = tocOffset,
            TocSize = tocSize,
            TocNonce = span.Slice(40, 12).ToArray(),
            Reserved52 = span.Slice(52, 12).ToArray(),
            Entries = entries,
            TocTrailingLength = toc.Length - p,
        };
    }

    public RawTocEntry Entry(string path) => Entries.Single(e => e.Path == path);
}

/// <summary>
/// 目次の 1 件。
/// </summary>
internal sealed class RawTocEntry
{
    public required string Path { get; init; }

    public required byte[] PathBytes { get; init; }

    public uint Flags { get; init; }

    public ulong Offset { get; init; }

    public ulong StoredSize { get; init; }

    public ulong OriginalSize { get; init; }

    public required byte[] Nonce { get; init; }

    public required byte[] Extra { get; init; }

    public required byte[] StoredData { get; init; }

    public bool IsDeflated => (Flags & RawPackageLayout.EntryFlagDeflate) != 0;

    /// <summary>
    /// 展開した中身。
    /// </summary>
    public byte[] Content => IsDeflated ? RawPackageLayout.Inflate(StoredData) : StoredData;
}

/// <summary>
/// 仕様書どおりのパッケージを、各フィールドを細工できる形で組み立てる。リーダーのテスト用。
/// </summary>
internal sealed class RawPackageBuilder
{
    private readonly List<Item> _items = [];

    public byte[] Magic { get; set; } = RawPackageLayout.Magic;

    public ushort Major { get; set; } = 1;

    public ushort Minor { get; set; }

    /// <summary>
    /// ヘッダの大きさ。64 より大きくすると、その分のゼロ (と <see cref="HeaderExtra"/>) を 64 バイト目以降に置く。
    /// </summary>
    public uint HeaderSize { get; set; } = 64;

    /// <summary>
    /// 64 バイト目以降に置く、マイナーバージョンで足されたとみなすヘッダの拡張。
    /// </summary>
    public byte[] HeaderExtra { get; set; } = [];

    public uint Flags { get; set; }

    public ulong? TocOffsetOverride { get; set; }

    public ulong? TocSizeOverride { get; set; }

    public uint? EntryCountOverride { get; set; }

    /// <summary>
    /// 目次の最後のエントリの後ろに置くバイト列 (マイナーバージョンでの拡張を模す)。
    /// </summary>
    public byte[] TocTrailing { get; set; } = [];

    /// <summary>
    /// 書き出したバイト列を、この長さに切り詰める。
    /// </summary>
    public int? TruncateTo { get; set; }

    /// <summary>
    /// 無圧縮のエントリを足す。
    /// </summary>
    public RawPackageBuilder Stored(string path, byte[] content, Action<Item>? tweak = null)
    {
        var item = new Item(path, 0, content, (ulong)content.Length);
        tweak?.Invoke(item);
        _items.Add(item);
        return this;
    }

    /// <summary>
    /// raw Deflate で圧縮したエントリを足す。
    /// </summary>
    public RawPackageBuilder Deflated(string path, byte[] content, Action<Item>? tweak = null)
    {
        var item = new Item(
            path,
            RawPackageLayout.EntryFlagDeflate,
            RawPackageLayout.Deflate(content),
            (ulong)content.Length
        );
        tweak?.Invoke(item);
        _items.Add(item);
        return this;
    }

    public RawPackageBuilder Stored(string path, string text, Action<Item>? tweak = null) =>
        Stored(path, Encoding.UTF8.GetBytes(text), tweak);

    public RawPackageBuilder Deflated(string path, string text, Action<Item>? tweak = null) =>
        Deflated(path, Encoding.UTF8.GetBytes(text), tweak);

    /// <summary>
    /// 足した順にデータ部へ並べ、末尾に目次を置いたパッケージを作る。
    /// </summary>
    public byte[] Build()
    {
        using var output = new MemoryStream();
        output.Write(new byte[Math.Max(HeaderSize, 64)]);
        var offsets = new List<ulong>();
        foreach (var item in _items)
        {
            offsets.Add((ulong)output.Position);
            output.Write(item.Stored);
        }

        var tocOffset = (ulong)output.Position;
        using (var toc = new MemoryStream())
        {
            WriteUInt32(toc, EntryCountOverride ?? (uint)_items.Count);
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var pathBytes = item.PathBytes ?? Encoding.UTF8.GetBytes(item.Path);
                WriteUInt16(toc, (ushort)pathBytes.Length);
                toc.Write(pathBytes);
                WriteUInt32(toc, item.Flags);
                WriteUInt64(toc, item.OffsetOverride ?? offsets[i]);
                WriteUInt64(toc, item.StoredSizeOverride ?? (ulong)item.Stored.Length);
                WriteUInt64(toc, item.OriginalSize);
                toc.Write(item.Nonce);
                WriteUInt16(toc, (ushort)item.Extra.Length);
                toc.Write(item.Extra);
            }

            toc.Write(TocTrailing);
            output.Write(toc.ToArray());
        }

        var tocSize = (ulong)output.Position - tocOffset;
        var bytes = output.ToArray();
        var header = bytes.AsSpan();
        Magic.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], Major);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], Minor);
        BinaryPrimitives.WriteUInt32LittleEndian(header[12..], HeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], Flags);
        BinaryPrimitives.WriteUInt64LittleEndian(header[24..], TocOffsetOverride ?? tocOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(header[32..], TocSizeOverride ?? tocSize);
        if (HeaderExtra.Length > 0)
            HeaderExtra.CopyTo(header[64..]);
        return TruncateTo is { } length ? bytes[..length] : bytes;
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, value);
        stream.Write(b);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(b, value);
        stream.Write(b);
    }

    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(b, value);
        stream.Write(b);
    }

    /// <summary>
    /// 組み立てるエントリ。フィールドを書き換えて細工する。
    /// </summary>
    internal sealed class Item(string path, uint flags, byte[] stored, ulong originalSize)
    {
        public string Path { get; set; } = path;

        /// <summary>
        /// パスのバイト列を直接指定する (不正な UTF-8 を試すため)。
        /// </summary>
        public byte[]? PathBytes { get; set; }

        public uint Flags { get; set; } = flags;

        public byte[] Stored { get; set; } = stored;

        public ulong OriginalSize { get; set; } = originalSize;

        public ulong? OffsetOverride { get; set; }

        public ulong? StoredSizeOverride { get; set; }

        public byte[] Nonce { get; set; } = new byte[12];

        public byte[] Extra { get; set; } = [];
    }
}
