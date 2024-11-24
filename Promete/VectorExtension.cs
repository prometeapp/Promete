using System.Numerics;

namespace Promete;

public static class VectorExtension
{
    public static Vector2 ToNumerics(this VectorInt vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static VectorInt ToPrometeInt(this Vector2 vector)
    {
        return new VectorInt((int)vector.X, (int)vector.Y);
    }

    public static Vector2 ToNumerics(this Vector vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector ToPromete(this Vector2 vector)
    {
        return new Vector(vector.X, vector.Y);
    }
}
