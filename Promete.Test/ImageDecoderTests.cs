using System.Buffers.Binary;
using System.IO.Compression;
using FluentAssertions;
using Promete.Graphics.Imaging;

namespace Promete.Test;

public class ImageDecoderTests
{
    [Fact]
    public void PngEncoderの出力をデコードすると元の画像に戻る()
    {
        byte[] pixels =
        [
            255,
            0,
            0,
            255,
            0,
            255,
            0,
            128,
            0,
            0,
            255,
            0,
            10,
            20,
            30,
            40,
            50,
            60,
            70,
            80,
            90,
            100,
            110,
            120,
        ];
        using var stream = new MemoryStream();
        PngEncoder.Encode(new RgbaImage(3, 2, pixels), stream);
        stream.Position = 0;

        var decoded = ImageDecoder.Decode(stream);

        decoded.Width.Should().Be(3);
        decoded.Height.Should().Be(2);
        decoded.Pixels.Should().Equal(pixels);
    }

    [Fact]
    public void PngのSubフィルターを復元できる()
    {
        // 3x1 グレースケール。値は 10, 25, 45 で、Sub フィルターでは差分 10, 15, 20 が格納される
        var png = BuildPng(3, 1, bitDepth: 8, colorType: 0, interlace: 0, [1, 10, 15, 20]);

        var decoded = ImageDecoder.Decode(png);

        decoded.Pixels.Should().Equal(10, 10, 10, 255, 25, 25, 25, 255, 45, 45, 45, 255);
    }

    [Fact]
    public void Pngの1ビットグレースケールを8ビットへ拡張できる()
    {
        // 8x1、ビット列 10100000
        var png = BuildPng(8, 1, bitDepth: 1, colorType: 0, interlace: 0, [0, 0b1010_0000]);

        var decoded = ImageDecoder.Decode(png);

        decoded.GetPixel(0, 0).Should().Be(((byte)255, (byte)255, (byte)255, (byte)255));
        decoded.GetPixel(1, 0).Should().Be(((byte)0, (byte)0, (byte)0, (byte)255));
        decoded.GetPixel(2, 0).Should().Be(((byte)255, (byte)255, (byte)255, (byte)255));
        decoded.GetPixel(3, 0).Should().Be(((byte)0, (byte)0, (byte)0, (byte)255));
    }

    [Fact]
    public void Pngのパレットと透過情報を反映できる()
    {
        byte[] palette = [255, 0, 0, 0, 0, 255];
        byte[] transparency = [128];
        // 2x1、インデックス 0, 1 (4bit)
        var png = BuildPng(
            2,
            1,
            bitDepth: 4,
            colorType: 3,
            interlace: 0,
            [0, 0x01],
            palette,
            transparency
        );

        var decoded = ImageDecoder.Decode(png);

        decoded.GetPixel(0, 0).Should().Be(((byte)255, (byte)0, (byte)0, (byte)128));
        decoded.GetPixel(1, 0).Should().Be(((byte)0, (byte)0, (byte)255, (byte)255));
    }

    [Fact]
    public void PngのAdam7インターレースを復元できる()
    {
        // 2x2 の場合、パス 0 → (0,0)、パス 5 → (1,0)、パス 6 → 2 行目の 2 画素 の順に並ぶ
        var png = BuildPng(
            2,
            2,
            bitDepth: 8,
            colorType: 0,
            interlace: 1,
            [0, 10, 0, 20, 0, 30, 40]
        );

        var decoded = ImageDecoder.Decode(png);

        decoded.GetPixel(0, 0).R.Should().Be(10);
        decoded.GetPixel(1, 0).R.Should().Be(20);
        decoded.GetPixel(0, 1).R.Should().Be(30);
        decoded.GetPixel(1, 1).R.Should().Be(40);
    }

    [Fact]
    public void Bmpの24ビット画像は下から上の行順で読み込まれる()
    {
        // 2x2。行は 4 バイト境界へパディングされる。下の行 (赤, 緑) が先に格納される
        byte[] rows =
        [
            0,
            0,
            255, /* 赤 */
            0,
            255,
            0, /* 緑 */
            0,
            0,
            255,
            0,
            0, /* 青 */
            255,
            255,
            255, /* 白 */
            0,
            0,
        ];
        var bmp = BuildBmp(2, 2, 24, rows);

        var decoded = ImageDecoder.Decode(bmp);

        decoded.GetPixel(0, 0).Should().Be(((byte)0, (byte)0, (byte)255, (byte)255));
        decoded.GetPixel(1, 0).Should().Be(((byte)255, (byte)255, (byte)255, (byte)255));
        decoded.GetPixel(0, 1).Should().Be(((byte)255, (byte)0, (byte)0, (byte)255));
        decoded.GetPixel(1, 1).Should().Be(((byte)0, (byte)255, (byte)0, (byte)255));
    }

    [Fact]
    public void Bmpの32ビット画像はトップダウンで読み込める()
    {
        // 高さが負の場合、上から下の順に格納される。アルファが 0 のものは不透明として扱われる
        byte[] rows = [0, 0, 255, 0, 255, 0, 0, 0];
        var bmp = BuildBmp(2, -1, 32, rows);

        var decoded = ImageDecoder.Decode(bmp);

        decoded.GetPixel(0, 0).Should().Be(((byte)255, (byte)0, (byte)0, (byte)255));
        decoded.GetPixel(1, 0).Should().Be(((byte)0, (byte)0, (byte)255, (byte)255));
    }

    [Fact]
    public void Bmpの8ビットパレット画像を読み込める()
    {
        byte[] palette =
        [
            0,
            0,
            255,
            0, /* 赤 (BGR0) */
            0,
            255,
            0,
            0, /* 緑 */
        ];
        byte[] rows = [1, 0, 0, 0];
        var bmp = BuildBmp(1, 1, 8, rows, palette, colorsUsed: 2);

        var decoded = ImageDecoder.Decode(bmp);

        decoded.GetPixel(0, 0).Should().Be(((byte)0, (byte)255, (byte)0, (byte)255));
    }

    [Fact]
    public void 未対応の形式は例外になる()
    {
        var act = () => ImageDecoder.Decode(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void 画像データが不足したPngは例外になる()
    {
        var png = BuildPng(4, 4, bitDepth: 8, colorType: 0, interlace: 0, [0, 1, 2]);

        var act = () => ImageDecoder.Decode(png);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void 圧縮データに対して巨大なサイズを宣言したPngは確保前に例外になる()
    {
        var png = BuildPng(20000, 20000, bitDepth: 8, colorType: 6, interlace: 0, [0, 1, 2]);

        var act = () => ImageDecoder.Decode(png);

        act.Should().Throw<InvalidDataException>().WithMessage("*圧縮データ*");
    }

    [Fact]
    public void チャンク長がオーバーフローするPngはInvalidDataExceptionになる()
    {
        var png = BuildPng(1, 1, bitDepth: 8, colorType: 0, interlace: 0, [0, 0]);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(8 + 4 + 4 + 13 + 4), int.MaxValue - 4);

        var act = () => ImageDecoder.Decode(png);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void 高さがint最小値のBmpはInvalidDataExceptionになる()
    {
        var bmp = BuildBmp(1, int.MinValue, 24, new byte[4]);

        var act = () => ImageDecoder.Decode(bmp);

        act.Should().Throw<InvalidDataException>();
    }

    private static byte[] BuildPng(
        int width,
        int height,
        int bitDepth,
        int colorType,
        int interlace,
        byte[] scanlines,
        byte[]? palette = null,
        byte[]? transparency = null
    )
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        header[12] = (byte)interlace;
        WriteChunk(stream, "IHDR", header);

        if (palette is not null)
            WriteChunk(stream, "PLTE", palette);
        if (transparency is not null)
            WriteChunk(stream, "tRNS", transparency);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionMode.Compress, true))
            zlib.Write(scanlines);
        WriteChunk(stream, "IDAT", compressed.ToArray());
        WriteChunk(stream, "IEND", []);
        return stream.ToArray();
    }

    // デコーダは CRC を検証しないため、テスト用のチャンクは CRC を 0 で埋める
    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(System.Text.Encoding.ASCII.GetBytes(type));
        stream.Write(data);
        stream.Write(new byte[4]);
    }

    private static byte[] BuildBmp(
        int width,
        int height,
        int bitCount,
        byte[] pixelData,
        byte[]? palette = null,
        int colorsUsed = 0
    )
    {
        var paletteLength = palette?.Length ?? 0;
        var pixelOffset = 14 + 40 + paletteLength;
        var data = new byte[pixelOffset + pixelData.Length];
        data[0] = (byte)'B';
        data[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(2), data.Length);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(10), pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(22), height);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(28), (short)bitCount);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(46), colorsUsed);
        palette?.CopyTo(data.AsSpan(54));
        pixelData.CopyTo(data.AsSpan(pixelOffset));
        return data;
    }
}
