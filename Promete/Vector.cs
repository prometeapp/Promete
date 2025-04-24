using System;
using System.Numerics;

namespace Promete;

/// <summary>
/// 2Dのベクトルを表します。
/// </summary>
public struct Vector(float x, float y) : IEquatable<Vector>
{
    /// <summary>
    /// このベクトルのX座標を取得または設定します。
    /// </summary>
    public float X { get; set; } = x;

    /// <summary>
    /// このベクトルのY座標を取得または設定します。
    /// </summary>
    public float Y { get; set; } = y;

    /// <summary>
    /// このベクトルの長さを取得します。
    /// </summary>
    public float Magnitude => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    /// このベクトルの単位ベクトルを取得します。
    /// </summary>
    public Vector Normalized => (X / Magnitude, Y / Magnitude);

    /// <summary>
    /// 2つのベクトルを加算します。
    /// </summary>
    public static Vector operator +(Vector v1, Vector v2)
    {
        return (v1.X + v2.X, v1.Y + v2.Y);
    }

    /// <summary>
    /// 2つのベクトルを減算します。
    /// </summary>
    public static Vector operator -(Vector v1, Vector v2)
    {
        return (v1.X - v2.X, v1.Y - v2.Y);
    }

    /// <summary>
    /// ベクトルをスカラーで乗算します。
    /// </summary>
    public static Vector operator *(Vector v1, float v2)
    {
        return (v1.X * v2, v1.Y * v2);
    }

    /// <summary>
    /// 2つのベクトルを要素ごとに乗算します。
    /// </summary>
    public static Vector operator *(Vector v1, Vector v2)
    {
        return (v1.X * v2.X, v1.Y * v2.Y);
    }

    /// <summary>
    /// ベクトルをスカラーで除算します。
    /// </summary>
    public static Vector operator /(Vector v1, float v2)
    {
        return (v1.X / v2, v1.Y / v2);
    }

    /// <summary>
    /// 2つのベクトルを要素ごとに除算します。
    /// </summary>
    public static Vector operator /(Vector v1, Vector v2)
    {
        return (v1.X / v2.X, v1.Y / v2.Y);
    }

    /// <summary>
    /// ベクトルを反転します。
    /// </summary>
    public static Vector operator -(Vector v1)
    {
        return (-v1.X, -v1.Y);
    }

    /// <summary>
    /// 2つのベクトルが等しいかどうかを確認します。
    /// </summary>
    public static bool operator ==(Vector v1, Vector v2)
    {
        return v1.X == v2.X && v1.Y == v2.Y;
    }

    /// <summary>
    /// 2つのベクトルが等しくないかどうかを確認します。
    /// </summary>
    public static bool operator !=(Vector v1, Vector v2)
    {
        return v1.X != v2.X || v1.Y != v2.Y;
    }

    /// <summary>
    /// VectorをVectorIntに明示的に変換します。
    /// </summary>
    public static explicit operator VectorInt(Vector v1)
    {
        return new VectorInt((int)v1.X, (int)v1.Y);
    }

    /// <summary>
    /// タプルからVectorに変換します。
    /// </summary>
    public static implicit operator Vector((float x, float y) v1)
    {
        return new Vector(v1.x, v1.y);
    }

    /// <summary>
    /// VectorをVector2に明示的に変換します。
    /// </summary>
    public static explicit operator Vector2(Vector v)
    {
        return new Vector2(v.X, v.Y);
    }

    /// <summary>
    /// 2つのベクトルがなす角を取得します。
    /// </summary>
    /// <returns>ラジアン単位の角度。</returns>
    public static float Angle(Vector from, Vector to)
    {
        return MathF.Atan2(to.Y - from.Y, to.X - from.X);
    }

    /// <summary>
    /// 2つのベクトル間の距離を取得します。
    /// </summary>
    public static float Distance(Vector from, Vector to)
    {
        return MathF.Sqrt(
            MathF.Abs((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y))
        );
    }

    /// <summary>
    /// 内積を計算します。
    /// </summary>
    public static float Dot(Vector v1, Vector v2)
    {
        return v1.X * v1.Y + v2.X * v2.Y;
    }

    /// <summary>
    /// 内積を計算します。
    /// </summary>
    public float Dot(Vector v)
    {
        return Dot(this, v);
    }

    /// <summary>
    /// このオブジェクトを比較します。
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Vector vector && Equals(vector);
    }

    /// <summary>
    /// このオブジェクトを比較します。
    /// </summary>
    public bool Equals(Vector other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    /// <summary>
    /// このオブジェクトのハッシュ値を取得します。
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <summary>
    /// このベクトルの角度を取得します。
    /// </summary>
    public float Angle()
    {
        return MathF.Atan2(Y, X);
    }

    /// <summary>
    /// このベクトルに対する指定したベクトルの方向を取得します。
    /// </summary>
    public float Angle(Vector to)
    {
        return Angle(this, to);
    }

    /// <summary>
    /// 2つのベクトル間の距離を取得します。
    /// </summary>
    public float Distance(Vector to)
    {
        return Distance(this, to);
    }

    /// <summary>
    /// このベクトルが指定した範囲内にあるかどうかを確認します。
    /// </summary>
    public bool In(Rect rect)
    {
        var topLeft = rect.Location;
        var bottomRight = rect.Location + rect.Size - One;
        return X >= topLeft.X && X <= bottomRight.X &&
               Y >= topLeft.Y && Y <= bottomRight.Y;
    }

    /// <summary>
    /// このベクトルが指定した範囲内にあるかどうかを確認します。
    /// </summary>
    public bool In(Vector location, Vector size)
    {
        return In(new Rect(location, size));
    }

    /// <summary>
    /// xとyを分解します。
    /// </summary>
    public void Deconstruct(out float x, out float y)
    {
        (x, y) = (X, Y);
    }

    /// <summary>
    /// このベクトルの文字列表現を取得します。
    /// </summary>
    public override string ToString()
    {
        return $"({X}, {Y})";
    }

    /// <summary>
    /// <c>new Vector(0, 0)</c> を取得します。
    /// </summary>
    public static readonly Vector Zero = (0, 0);

    /// <summary>
    /// <c>new Vector(1, 1)</c> を取得します。
    /// </summary>
    public static readonly Vector One = (1, 1);

    /// <summary>
    /// <c>new Vector(-1, 0)</c> を取得します。
    /// </summary>
    public static readonly Vector Left = (-1, 0);

    /// <summary>
    /// <c>new Vector(0, -1)</c> を取得します。
    /// </summary>
    public static readonly Vector Up = (0, -1);

    /// <summary>
    /// <c>new Vector(1, 0)</c> を取得します。
    /// </summary>
    public static readonly Vector Right = (1, 0);

    /// <summary>
    /// <c>new Vector(0, 1)</c> を取得します。
    /// </summary>
    public static readonly Vector Down = (0, 1);

    public static Vector From(Vector2 vec)
    {
        return (vec.X, vec.Y);
    }
}
