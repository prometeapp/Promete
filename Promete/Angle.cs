using System;

namespace Promete;

/// <summary>
/// 角度を表す構造体です。内部的には度数法で値を保持します。
/// </summary>
public struct Angle : IEquatable<Angle>
{
    /// <summary>
    /// 角度を度数法で取得または設定します。
    /// </summary>
    public float Degrees { get; set; }

    /// <summary>
    /// 角度をラジアンで取得または設定します。
    /// </summary>
    public float Radians
    {
        get => Degrees * MathF.PI / 180f;
        set => Degrees = value * 180f / MathF.PI;
    }

    private Angle(float degrees)
    {
        Degrees = degrees;
    }

    /// <summary>
    /// 度数法の値から <see cref="Angle"/> を生成します。
    /// </summary>
    public static Angle FromDegrees(float degrees) => new(degrees);

    /// <summary>
    /// ラジアンの値から <see cref="Angle"/> を生成します。
    /// </summary>
    public static Angle FromRadians(float radians) => new(radians * 180f / MathF.PI);

    /// <summary>
    /// 0度を表す <see cref="Angle"/> です。
    /// </summary>
    public static readonly Angle Zero = new(0);

    // --- 暗黙変換 ---

    /// <summary>
    /// <c>float</c>（度数法）から <see cref="Angle"/> へ暗黙変換します。
    /// </summary>
    public static implicit operator Angle(float degrees) => new(degrees);

    /// <summary>
    /// <see cref="Angle"/> から <c>float</c>（度数法）へ暗黙変換します。
    /// </summary>
    public static implicit operator float(Angle angle) => angle.Degrees;

    // --- 算術演算子 ---

    public static Angle operator +(Angle a, Angle b) => new(a.Degrees + b.Degrees);
    public static Angle operator -(Angle a, Angle b) => new(a.Degrees - b.Degrees);
    public static Angle operator -(Angle a) => new(-a.Degrees);
    public static Angle operator *(Angle a, float scalar) => new(a.Degrees * scalar);
    public static Angle operator *(float scalar, Angle a) => new(scalar * a.Degrees);
    public static Angle operator /(Angle a, float scalar) => new(a.Degrees / scalar);
    public static Angle operator %(Angle a, float value) => new(a.Degrees % value);

    // --- 比較演算子 ---

    public static bool operator ==(Angle a, Angle b) => a.Degrees == b.Degrees;
    public static bool operator !=(Angle a, Angle b) => a.Degrees != b.Degrees;

    // --- IEquatable / Object ---

    public bool Equals(Angle other) => Degrees.Equals(other.Degrees);
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override int GetHashCode() => Degrees.GetHashCode();

    public override string ToString() => $"{Degrees}°";
}
