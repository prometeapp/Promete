using System;
using System.Numerics;

namespace Promete;

/// <summary>
/// 2次元の整数ベクトルを表します。
/// </summary>
public struct VectorInt(int x, int y) : IEquatable<VectorInt>
{
    /// <summary>
    /// このベクトルのX座標を取得または設定します。
    /// </summary>
    public int X { get; set; } = x;

    /// <summary>
    /// このベクトルのY座標を取得または設定します。
    /// </summary>
    public int Y { get; set; } = y;

    /// <summary>
    /// このベクトルの大きさを取得します。
    /// </summary>
    public float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// このベクトルの単位ベクトルを取得します。
    /// </summary>
    public Vector Normalized => (X / Magnitude, Y / Magnitude);

    public static VectorInt operator +(VectorInt v1, VectorInt v2)
    {
        return (v1.X + v2.X, v1.Y + v2.Y);
    }

    public static VectorInt operator -(VectorInt v1, VectorInt v2)
    {
        return (v1.X - v2.X, v1.Y - v2.Y);
    }

    public static VectorInt operator *(VectorInt v1, int v2)
    {
        return (v1.X * v2, v1.Y * v2);
    }

    public static VectorInt operator *(VectorInt v1, VectorInt v2)
    {
        return (v1.X * v2.X, v1.Y * v2.Y);
    }

    public static VectorInt operator /(VectorInt v1, int v2)
    {
        return (v1.X / v2, v1.Y / v2);
    }

    public static VectorInt operator /(VectorInt v1, VectorInt v2)
    {
        return (v1.X / v2.X, v1.Y / v2.Y);
    }

    public static VectorInt operator -(VectorInt v1)
    {
        return (-v1.X, -v1.Y);
    }

    public static implicit operator Vector(VectorInt v1)
    {
        return new Vector(v1.X, v1.Y);
    }

    public static bool operator ==(VectorInt v1, VectorInt v2)
    {
        return v1.X == v2.X && v1.Y == v2.Y;
    }

    public static bool operator !=(VectorInt v1, VectorInt v2)
    {
        return v1.X != v2.X || v1.Y != v2.Y;
    }

    public static implicit operator VectorInt((int x, int y) v1)
    {
        return new VectorInt(v1.x, v1.y);
    }

    /// <summary>
    /// 2つのベクトルがなす角を取得します。
    /// </summary>
    /// <param name="from">始点。</param>
    /// <param name="to">終点。</param>
    /// <returns>ラジアン単位の角度。</returns>
    public static float Angle(VectorInt from, VectorInt to)
    {
        return MathF.Atan2(to.Y - from.Y, to.X - from.X);
    }

    /// <summary>
    /// 2つのベクトルのユークリッド距離を取得します。
    /// <param name="from">始点。</param>
    /// <param name="to">終点。</param>
    /// <returns>距離。</returns>
    /// </summary>
    public static float Distance(VectorInt from, VectorInt to)
    {
        return MathF.Sqrt(
            MathF.Abs((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y))
        );
    }

    /// <summary>
    /// 2つのベクトルの内積を計算します。
    /// </summary>
    public static int Dot(VectorInt v1, VectorInt v2)
    {
        return v1.X * v1.Y + v2.X * v2.Y;
    }

    /// <summary>
    /// このベクトルと指定したベクトルの内積を計算します。
    /// </summary>
    public int Dot(VectorInt v)
    {
        return Dot(this, v);
    }

    /// <summary>
    ///     Compare this object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is VectorInt vec && Equals(vec);
    }

    /// <summary>
    ///     Compare this object.
    /// </summary>
    public bool Equals(VectorInt other)
    {
        return X == other.X &&
               Y == other.Y;
    }

    /// <summary>
    ///     Get the hash value of this object.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    ///     Get angle of this vector.
    /// </summary>
    public float Angle()
    {
        return MathF.Atan2(Y, X);
    }

    /// <summary>
    ///     Get the direction of the specified vector relative to this vector.
    /// </summary>
    public float Angle(VectorInt to)
    {
        return Angle(this, to);
    }

    /// <summary>
    ///     Get the distance between two vectors.
    /// </summary>
    public float Distance(VectorInt to)
    {
        return Distance(this, to);
    }

    /// <summary>
    ///     Check if this vector is in the specified range.
    /// </summary>
    public bool In(Rect rect)
    {
        var topLeft = rect.Location;
        var bottomRight = rect.Location + rect.Size - One;
        return X >= topLeft.X && X <= bottomRight.X &&
               Y >= topLeft.Y && Y <= bottomRight.Y;
    }

    /// <summary>
    ///     Check if this vector is in the specified range.
    /// </summary>
    public bool In(Vector location, Vector size)
    {
        return In(new Rect(location, size));
    }

    /// <summary>
    ///     Deconstructs x and y.
    /// </summary>
    public void Deconstruct(out int x, out int y)
    {
        (x, y) = (X, Y);
    }

    /// <summary>
    ///     Get formatted string of this vector.
    /// </summary>
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    /// <summary>
    ///     Get <c>new VectorInt(0, 0)</c>.
    /// </summary>
    public static readonly VectorInt Zero = (0, 0);

    /// <summary>
    ///     Get <c>new VectorInt(1, 1)</c>.
    /// </summary>
    public static readonly VectorInt One = (1, 1);

    /// <summary>
    ///     Get <c>new VectorInt(-1, 0)</c>.
    /// </summary>
    public static readonly VectorInt Left = (-1, 0);

    /// <summary>
    ///     Get <c>new VectorInt(0, -1)</c>.
    /// </summary>
    public static readonly VectorInt Up = (0, -1);

    /// <summary>
    ///     Get <c>new VectorInt(1, 0)</c>.
    /// </summary>
    public static readonly VectorInt Right = (1, 0);

    /// <summary>
    ///     Get <c>new VectorInt(0, 1)</c>.
    /// </summary>
    public static readonly VectorInt Down = (0, 1);

    public static VectorInt From(Vector2 vec)
    {
        return ((int)vec.X, (int)vec.Y);
    }
}
