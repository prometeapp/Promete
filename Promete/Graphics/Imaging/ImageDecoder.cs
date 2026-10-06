using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace Promete.Graphics.Imaging;

/// <summary>
/// PNG と BMP をデコードして <see cref="RgbaImage" /> へ変換するデコーダです。
/// </summary>
internal static class ImageDecoder
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private const long MaxZlibRatio = 1100;

    private static readonly int[] AdamStartX = [0, 4, 0, 2, 0, 1, 0];
    private static readonly int[] AdamStartY = [0, 0, 4, 0, 2, 0, 1];
    private static readonly int[] AdamStepX = [8, 8, 4, 4, 2, 2, 1];
    private static readonly int[] AdamStepY = [8, 8, 8, 4, 4, 2, 2];

    /// <summary>
    /// 画像ファイルを読み込みます。
    /// </summary>
    /// <exception cref="NotSupportedException">PNG / BMP 以外、または未対応の形式の場合。</exception>
    /// <exception cref="InvalidDataException">データが壊れている場合。</exception>
    public static RgbaImage Decode(string path)
    {
        return Decode(File.ReadAllBytes(path));
    }

    /// <summary>
    /// ストリームから画像を読み込みます。
    /// </summary>
    public static RgbaImage Decode(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Decode(memory.ToArray());
    }

    /// <summary>
    /// バイト列から画像を読み込みます。形式はシグネチャから判別します。
    /// </summary>
    public static RgbaImage Decode(byte[] data)
    {
        if (data.AsSpan().StartsWith(PngSignature))
            return DecodePng(data);
        if (data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M')
            return DecodeBmp(data);

        throw new NotSupportedException("対応している画像形式は PNG と BMP のみです。");
    }

    private static RgbaImage DecodePng(byte[] data)
    {
        int width = 0,
            height = 0,
            bitDepth = 0,
            colorType = 0,
            interlace = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        var hasHeader = false;
        using var compressed = new MemoryStream();

        var pos = PngSignature.Length;
        while (pos + 8 <= data.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(pos));
            var type = Encoding.ASCII.GetString(data, pos + 4, 4);
            var body = pos + 8;
            if (length < 0 || length > data.Length - body)
                throw new InvalidDataException("PNG のチャンクが壊れています。");

            switch (type)
            {
                case "IHDR":
                    if (length < 13)
                        throw new InvalidDataException("PNG のヘッダーが壊れています。");
                    width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(body));
                    height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(body + 4));
                    bitDepth = data[body + 8];
                    colorType = data[body + 9];
                    interlace = data[body + 12];
                    hasHeader = true;
                    break;
                case "PLTE":
                    palette = data.AsSpan(body, length).ToArray();
                    break;
                case "tRNS":
                    transparency = data.AsSpan(body, length).ToArray();
                    break;
                case "IDAT":
                    compressed.Write(data, body, length);
                    break;
            }

            if (type == "IEND")
                break;
            pos = body + length + 4;
        }

        if (!hasHeader)
            throw new InvalidDataException("PNG のヘッダーがありません。");
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue / 4)
            throw new InvalidDataException("PNG の画像サイズが不正です。");
        if (interlace is not (0 or 1))
            throw new NotSupportedException("未対応の PNG インターレース方式です。");

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new InvalidDataException("PNG のカラータイプが不正です。"),
        };
        var validDepth = colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            _ => bitDepth is 8 or 16,
        };
        if (!validDepth)
            throw new InvalidDataException("PNG のビット深度が不正です。");
        if (colorType == 3 && palette is null)
            throw new InvalidDataException("PNG にパレットがありません。");

        var bitsPerPixel = channels * bitDepth;
        var filterStep = Math.Max(1, bitsPerPixel / 8);

        var passes = interlace == 0 ? 1 : 7;
        var raw = ReadScanlines(compressed, width, height, bitsPerPixel, passes);

        var pixels = new byte[width * height * 4];
        var state = new PngPixelConverter(colorType, bitDepth, channels, palette, transparency);
        var rawPos = 0;
        for (var pass = 0; pass < passes; pass++)
        {
            var (startX, startY, stepX, stepY) =
                interlace == 0
                    ? (0, 0, 1, 1)
                    : (AdamStartX[pass], AdamStartY[pass], AdamStepX[pass], AdamStepY[pass]);
            var passWidth = (width - startX + stepX - 1) / stepX;
            var passHeight = (height - startY + stepY - 1) / stepY;
            if (passWidth <= 0 || passHeight <= 0)
                continue;

            var rowBytes = ((passWidth * bitsPerPixel) + 7) / 8;
            var previous = new byte[rowBytes];
            for (var y = 0; y < passHeight; y++)
            {
                var filter = raw[rawPos];
                var row = raw.AsSpan(rawPos + 1, rowBytes);
                Unfilter(filter, row, previous, filterStep);

                var destY = startY + (y * stepY);
                for (var x = 0; x < passWidth; x++)
                {
                    var destX = startX + (x * stepX);
                    state.Convert(row, x, pixels.AsSpan(((destY * width) + destX) * 4, 4));
                }

                previous = row.ToArray();
                rawPos += 1 + rowBytes;
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static byte[] ReadScanlines(
        MemoryStream compressed,
        int width,
        int height,
        int bitsPerPixel,
        int passes
    )
    {
        long total = 0;
        for (var pass = 0; pass < passes; pass++)
        {
            var (startX, startY, stepX, stepY) =
                passes == 1
                    ? (0, 0, 1, 1)
                    : (AdamStartX[pass], AdamStartY[pass], AdamStepX[pass], AdamStepY[pass]);
            var passWidth = (width - startX + stepX - 1) / stepX;
            var passHeight = (height - startY + stepY - 1) / stepY;
            if (passWidth <= 0 || passHeight <= 0)
                continue;
            total += (long)passHeight * (1 + (((passWidth * bitsPerPixel) + 7) / 8));
        }

        if (total > int.MaxValue)
            throw new InvalidDataException("PNG の画像サイズが大きすぎます。");

        // zlib の圧縮率は最大でも約 1032 倍。宣言サイズがそれを超えるなら、確保前に壊れたデータとして弾く
        if (total > (compressed.Length * MaxZlibRatio) + 1024)
            throw new InvalidDataException("PNG の画像サイズが圧縮データに対して不正です。");

        var raw = new byte[total];
        compressed.Position = 0;
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress, true);
        try
        {
            zlib.ReadExactly(raw);
        }
        catch (EndOfStreamException e)
        {
            throw new InvalidDataException("PNG の画像データが不足しています。", e);
        }

        return raw;
    }

    private static void Unfilter(byte filter, Span<byte> row, ReadOnlySpan<byte> previous, int step)
    {
        switch (filter)
        {
            case 0:
                break;
            case 1:
                for (var i = step; i < row.Length; i++)
                    row[i] = (byte)(row[i] + row[i - step]);
                break;
            case 2:
                for (var i = 0; i < row.Length; i++)
                    row[i] = (byte)(row[i] + previous[i]);
                break;
            case 3:
                for (var i = 0; i < row.Length; i++)
                {
                    var left = i >= step ? row[i - step] : 0;
                    row[i] = (byte)(row[i] + ((left + previous[i]) >> 1));
                }

                break;
            case 4:
                for (var i = 0; i < row.Length; i++)
                {
                    var left = i >= step ? row[i - step] : 0;
                    var upLeft = i >= step ? previous[i - step] : 0;
                    row[i] = (byte)(row[i] + Paeth(left, previous[i], upLeft));
                }

                break;
            default:
                throw new InvalidDataException("PNG のフィルタータイプが不正です。");
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
            return a;
        return pb <= pc ? b : c;
    }

    private static RgbaImage DecodeBmp(byte[] data)
    {
        if (data.Length < 26)
            throw new InvalidDataException("BMP のヘッダーが壊れています。");

        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(10));
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(14));

        int width,
            height,
            bitCount,
            compression = 0,
            colorsUsed = 0;
        var paletteEntrySize = 4;
        if (headerSize == 12)
        {
            width = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(18));
            height = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(20));
            bitCount = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(24));
            paletteEntrySize = 3;
        }
        else if (headerSize >= 40 && data.Length >= 14 + 40)
        {
            width = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(18));
            height = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(22));
            bitCount = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(28));
            compression = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(30));
            colorsUsed = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(46));
        }
        else
        {
            throw new NotSupportedException("未対応の BMP ヘッダー形式です。");
        }

        if (height == int.MinValue)
            throw new InvalidDataException("BMP の画像サイズが不正です。");

        var topDown = height < 0;
        height = Math.Abs(height);
        if (width <= 0 || height == 0 || (long)width * height > int.MaxValue / 4)
            throw new InvalidDataException("BMP の画像サイズが不正です。");
        if (compression is not (0 or 3 or 6))
            throw new NotSupportedException("圧縮された BMP には対応していません。");
        if (bitCount is not (1 or 2 or 4 or 8 or 16 or 24 or 32))
            throw new NotSupportedException("未対応の BMP ビット深度です。");

        var stride = (int)((((long)width * bitCount) + 31) / 32 * 4);
        if (pixelOffset < 0 || pixelOffset + ((long)stride * height) > data.Length)
            throw new InvalidDataException("BMP の画像データが不足しています。");

        byte[]? palette = null;
        if (bitCount <= 8)
        {
            var count = colorsUsed > 0 ? colorsUsed : 1 << bitCount;
            var paletteStart = 14 + headerSize;
            if (paletteStart + ((long)count * paletteEntrySize) > data.Length)
                throw new InvalidDataException("BMP のパレットが壊れています。");

            // B,G,R(,予約) の並びを R,G,B へ並べ替える
            palette = new byte[count * 3];
            for (var i = 0; i < count; i++)
            {
                var entry = paletteStart + (i * paletteEntrySize);
                palette[(i * 3) + 0] = data[entry + 2];
                palette[(i * 3) + 1] = data[entry + 1];
                palette[(i * 3) + 2] = data[entry];
            }
        }

        uint maskR = 0,
            maskG = 0,
            maskB = 0,
            maskA = 0;
        if (bitCount is 16 or 32)
        {
            if (compression is 3 or 6)
            {
                if (data.Length < 70)
                    throw new InvalidDataException("BMP のヘッダーが壊れています。");

                // 40 バイトのヘッダーではマスクがヘッダー直後に置かれ、V2 以降ではヘッダー内にある
                maskR = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(54));
                maskG = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(58));
                maskB = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(62));
                if (compression == 6 || headerSize >= 56)
                    maskA = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(66));
            }
            else if (bitCount == 16)
            {
                (maskR, maskG, maskB) = (0x7C00, 0x03E0, 0x001F);
            }
            else
            {
                (maskR, maskG, maskB) = (0x00FF0000, 0x0000FF00, 0x000000FF);
            }
        }

        var pixels = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var source = data.AsSpan(pixelOffset + (row * stride), stride);
            var destY = topDown ? row : height - 1 - row;
            var destination = pixels.AsSpan(destY * width * 4, width * 4);

            for (var x = 0; x < width; x++)
            {
                var d = destination.Slice(x * 4, 4);
                switch (bitCount)
                {
                    case <= 8:
                        var bit = x * bitCount;
                        var shift = 8 - bitCount - (bit & 7);
                        var index = (source[bit >> 3] >> shift) & ((1 << bitCount) - 1);
                        if ((index * 3) + 2 >= palette!.Length)
                            throw new InvalidDataException("BMP のパレット番号が範囲外です。");
                        d[0] = palette[index * 3];
                        d[1] = palette[(index * 3) + 1];
                        d[2] = palette[(index * 3) + 2];
                        d[3] = 255;
                        break;
                    case 24:
                        d[0] = source[(x * 3) + 2];
                        d[1] = source[(x * 3) + 1];
                        d[2] = source[x * 3];
                        d[3] = 255;
                        break;
                    default:
                        var value =
                            bitCount == 16
                                ? BinaryPrimitives.ReadUInt16LittleEndian(source[(x * 2)..])
                                : BinaryPrimitives.ReadUInt32LittleEndian(source[(x * 4)..]);
                        d[0] = ExtractChannel(value, maskR);
                        d[1] = ExtractChannel(value, maskG);
                        d[2] = ExtractChannel(value, maskB);
                        d[3] = maskA == 0 ? (byte)255 : ExtractChannel(value, maskA);
                        break;
                }
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static byte ExtractChannel(uint value, uint mask)
    {
        if (mask == 0)
            return 0;

        var shift = BitOperations.TrailingZeroCount(mask);
        var max = mask >> shift;
        return (byte)((ulong)((value & mask) >> shift) * 255 / max);
    }
}
