using System.Numerics;

namespace Promete;

/// <summary>
/// Vector 型の拡張メソッドを提供します。
/// </summary>
public static class VectorExtension
{
    /// <summary>
    /// <see cref="VectorInt"/> を <see cref="Vector2"/> に変換します。
    /// </summary>
    public static Vector2 ToNumerics(this VectorInt vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// <see cref="Vector2"/> を <see cref="VectorInt"/> に変換します。
    /// <para>小数点以下は切り捨てられます。</para>
    /// </summary>
    public static VectorInt ToPrometeInt(this Vector2 vector)
    {
        return new VectorInt((int)vector.X, (int)vector.Y);
    }

    /// <summary>
    /// <see cref="Vector"/> を <see cref="Vector2"/> に変換します。
    /// </summary>
    public static Vector2 ToNumerics(this Vector vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    /// <summary>
    /// <see cref="Vector2"/> を <see cref="Vector"/> に変換します。
    /// </summary>
    public static Vector ToPromete(this Vector2 vector)
    {
        return new Vector(vector.X, vector.Y);
    }
}
