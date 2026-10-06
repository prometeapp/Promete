using System;

namespace Promete.Graphics.Imaging;

/// <summary>
/// RGBA8888 形式でピクセルを保持する、メモリ上の画像です。
/// </summary>
internal sealed class RgbaImage(int width, int height, byte[] pixels)
{
    /// <summary>
    /// 画像の幅を取得します。
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// 画像の高さを取得します。
    /// </summary>
    public int Height { get; } = height;

    /// <summary>
    /// 左上から右下の順に並んだ RGBA8888 のピクセルデータを取得します。
    /// </summary>
    public byte[] Pixels { get; } = pixels;

    /// <summary>
    /// 指定した位置のピクセルを取得します。
    /// </summary>
    public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
    {
        var i = ((y * Width) + x) * 4;
        return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
    }

    /// <summary>
    /// 指定した領域を切り抜いた新しい画像を生成します。
    /// </summary>
    public RgbaImage Crop(int x, int y, int cropWidth, int cropHeight)
    {
        if (x < 0 || y < 0 || cropWidth < 0 || cropHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (x + cropWidth > Width || y + cropHeight > Height)
            throw new ArgumentOutOfRangeException(nameof(cropWidth));

        var result = new byte[cropWidth * cropHeight * 4];
        for (var row = 0; row < cropHeight; row++)
        {
            Pixels
                .AsSpan((((y + row) * Width) + x) * 4, cropWidth * 4)
                .CopyTo(result.AsSpan(row * cropWidth * 4));
        }

        return new RgbaImage(cropWidth, cropHeight, result);
    }

    /// <summary>
    /// 上下を反転した新しい画像を生成します。
    /// </summary>
    public RgbaImage FlipVertical()
    {
        var stride = Width * 4;
        var result = new byte[Pixels.Length];
        for (var row = 0; row < Height; row++)
        {
            Pixels
                .AsSpan(row * stride, stride)
                .CopyTo(result.AsSpan((Height - 1 - row) * stride, stride));
        }

        return new RgbaImage(Width, Height, result);
    }
}
