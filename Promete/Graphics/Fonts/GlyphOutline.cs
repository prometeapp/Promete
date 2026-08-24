using System;

namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフの縁取りを生成します。
/// </summary>
/// <remarks>
/// 縁取りはラスタライズ後のアルファを膨張させて求めるため、
/// アウトラインを持たないビットマップフォントや外字に対しても同じように適用できます。
/// </remarks>
internal static class GlyphOutline
{
    /// <summary>
    /// グリフを指定した太さだけ太らせた画像を生成します。
    /// </summary>
    /// <remarks>
    /// 生成される画像は本体を覆う大きさになります。
    /// 縁取りを描画したうえに本体を重ねることで、縁取り付きの文字になります。
    /// </remarks>
    public static GlyphBitmap Create(in GlyphBitmap source, int thickness)
    {
        if (thickness <= 0 || source.IsEmpty)
            return source;

        var width = source.Size.X + (thickness * 2);
        var height = source.Size.Y + (thickness * 2);
        var pixels = new byte[width * height * 4];
        var squaredThickness = thickness * thickness;

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var alpha = MaxAlphaAround(source, x - thickness, y - thickness, thickness, squaredThickness);
            if (alpha == 0)
                continue;

            var offset = ((y * width) + x) * 4;
            pixels[offset] = 255;
            pixels[offset + 1] = 255;
            pixels[offset + 2] = 255;
            pixels[offset + 3] = alpha;
        }

        return new GlyphBitmap(
            pixels,
            new VectorInt(width, height),
            source.Bearing - new VectorInt(thickness, thickness)
        );
    }

    /// <summary>
    /// 指定した位置を中心とする円内で、最も濃いアルファ値を求めます。
    /// </summary>
    private static byte MaxAlphaAround(
        in GlyphBitmap source,
        int centerX,
        int centerY,
        int radius,
        int squaredRadius
    )
    {
        byte max = 0;

        for (var dy = -radius; dy <= radius; dy++)
        for (var dx = -radius; dx <= radius; dx++)
        {
            if ((dx * dx) + (dy * dy) > squaredRadius)
                continue;

            var x = centerX + dx;
            var y = centerY + dy;
            if (x < 0 || y < 0 || x >= source.Size.X || y >= source.Size.Y)
                continue;

            var alpha = source.Pixels[(((y * source.Size.X) + x) * 4) + 3];
            if (alpha <= max)
                continue;

            max = alpha;
            if (max == 255)
                return max;
        }

        return max;
    }
}
