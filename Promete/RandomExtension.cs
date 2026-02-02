using System;
using System.Drawing;

namespace Promete;

/// <summary>
/// 乱数に関する拡張メソッドを提供します。
/// </summary>
public static class RandomExtension
{
    /// <summary>
    /// ランダムな色を生成します。
    /// </summary>
    public static Color NextColor(this Random r, int max = 256)
    {
        return Color.FromArgb(r.Next(max), r.Next(max), r.Next(max));
    }

    /// <summary>
    /// ランダムな <see cref="Vector" /> を生成します。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="xMax">X 座標の最大値。</param>
    /// <param name="yMax">Y 座標の最大値。</param>
    /// <returns>生成された <see cref="Vector" />。</returns>
    public static Vector NextVector(this Random r, int xMax, int yMax)
    {
        return (r.Next(xMax), r.Next(yMax));
    }

    /// <summary>
    /// ランダムな <see cref="VectorInt" /> を生成します。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="xMax">X 座標の最大値。</param>
    /// <param name="yMax">Y 座標の最大値。</param>
    /// <returns>生成された <see cref="VectorInt" />。</returns>
    public static VectorInt NextVectorInt(this Random r, int xMax, int yMax)
    {
        return (r.Next(xMax), r.Next(yMax));
    }

    /// <summary>
    /// ランダムな <see cref="Vector" /> を生成します。<see cref="NextVector(Random, int, int)" /> と異なり、小数部分もランダムに生成されます。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="xMax">X 座標の最大値。</param>
    /// <param name="yMax">Y 座標の最大値。</param>
    /// <returns>生成された <see cref="Vector" />。</returns>
    public static Vector NextVectorFloat(this Random r, int xMax = 1, int yMax = 1)
    {
        return ((float)r.NextDouble() * xMax, (float)r.NextDouble() * yMax);
    }

    /// <summary>
    /// ランダムな <see cref="Vector" /> を生成します。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="max">最大値を表す <see cref="Vector" />。</param>
    /// <returns>生成された <see cref="Vector" />。</returns>
    public static Vector NextVector(this Random r, Vector max)
    {
        return NextVector(r, (int)max.X, (int)max.Y);
    }

    /// <summary>
    /// ランダムな <see cref="VectorInt" /> を生成します。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="max">最大値を表す <see cref="VectorInt" />。</param>
    /// <returns>生成された <see cref="VectorInt" />。</returns>
    public static VectorInt NextVectorInt(this Random r, VectorInt max)
    {
        return NextVectorInt(r, max.X, max.Y);
    }

    /// <summary>
    /// ランダムな <see cref="Vector" /> を生成します。<see cref="NextVector(Random, Vector)" /> と異なり、小数部分もランダムに生成されます。
    /// </summary>
    /// <param name="r">この <see cref="Random" /> オブジェクト。</param>
    /// <param name="max">最大値を表す <see cref="Vector" />。</param>
    /// <returns>生成された <see cref="Vector" />。</returns>
    public static Vector NextVectorFloat(this Random r, Vector max)
    {
        return ((float)r.NextDouble() * max.X, (float)r.NextDouble() * max.Y);
    }
}
