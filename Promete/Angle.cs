using System;

namespace Promete;

/// <summary>
/// 角度を表す構造体です。内部的には度数法で値を保持します。
/// </summary>
public struct Angle : IEquatable<Angle>
{
    /// <summary>
    /// 0度を表す <see cref="Angle"/> です。
    /// </summary>
    public static readonly Angle Zero = new(0);

    private float _degrees;

    private Angle(float degrees)
    {
        _degrees = degrees;
    }


    /// <summary>
    /// 角度を度数法の <c>float</c> 値として返します。
    /// </summary>
    public float ToDegrees() => _degrees;

    /// <summary>
    /// 角度をラジアンの <c>float</c> 値として返します。
    /// </summary>
    public float ToRadians() => _degrees * MathF.PI / 180f;

    /// <summary>
    /// 度数法の値から <see cref="Angle"/> を生成します。
    /// </summary>
    public static Angle FromDegrees(float degrees) => new(degrees);

    /// <summary>
    /// ラジアンの値から <see cref="Angle"/> を生成します。
    /// </summary>
    public static Angle FromRadians(float radians) => new(radians * 180f / MathF.PI);

    // --- 算術演算子 ---

    public static Angle operator +(Angle a, Angle b) => new(a._degrees + b._degrees);
    public static Angle operator -(Angle a, Angle b) => new(a._degrees - b._degrees);
    public static Angle operator -(Angle a) => new(-a._degrees);
    public static Angle operator *(Angle a, float scalar) => new(a._degrees * scalar);
    public static Angle operator *(float scalar, Angle a) => new(scalar * a._degrees);
    public static Angle operator /(Angle a, float scalar) => new(a._degrees / scalar);
    public static Angle operator %(Angle a, float value) => new(a._degrees % value);

    // --- 比較演算子 ---

    public static bool operator ==(Angle a, Angle b) => a._degrees == b._degrees;
    public static bool operator !=(Angle a, Angle b) => a._degrees != b._degrees;

    // --- IEquatable / Object ---

    public bool Equals(Angle other) => _degrees.Equals(other._degrees);
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override int GetHashCode() => _degrees.GetHashCode();

    public override string ToString() => $"{_degrees}°";
}
