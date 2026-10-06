using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace Promete.Graphics.Imaging;

/// <summary>
/// RGBA8888 の画像を PNG 形式で書き出すエンコーダです。
/// </summary>
internal static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = CreateCrcTable();

    /// <summary>
    /// 画像を PNG (8bit RGBA) としてストリームへ書き出します。
    /// </summary>
    public static void Encode(RgbaImage image, Stream stream)
    {
        stream.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, image.Width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], image.Height);
        header[8] = 8; // ビット深度
        header[9] = 6; // カラータイプ: RGBA
        header[10] = 0; // 圧縮方式
        header[11] = 0; // フィルター方式
        header[12] = 0; // インターレースなし
        WriteChunk(stream, "IHDR"u8, header);

        // 各行の先頭にフィルタータイプ 0 (None) を付けて zlib 圧縮する
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, true))
        {
            var stride = image.Width * 4;
            for (var y = 0; y < image.Height; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(image.Pixels, y * stride, stride);
            }
        }

        WriteChunk(stream, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        WriteChunk(stream, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
        stream.Write(buffer);
        stream.Write(type);
        stream.Write(data);

        var crc = UpdateCrc(0xFFFFFFFF, type);
        crc = UpdateCrc(crc, data);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, ~crc);
        stream.Write(buffer);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }
}
