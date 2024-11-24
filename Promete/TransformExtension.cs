using System;
using System.Runtime.CompilerServices;

namespace Promete;

public static class TransformExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector Rotate(this Vector point, float angleInRadian)
    {
        var cos = MathF.Cos(angleInRadian);
        var sin = MathF.Sin(angleInRadian);
        return (point.X * cos - point.Y * sin, point.X * sin + point.Y * cos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector Translate(this Vector point, Vector delta)
    {
        return (point.X + delta.X, point.Y + delta.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector Scale(this Vector point, Vector scale)
    {
        return point * scale;
    }
}
