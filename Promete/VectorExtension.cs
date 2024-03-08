namespace Promete;

public static class VectorExtension
{
	public static System.Numerics.Vector2 ToNumerics(this VectorInt vector) => new(vector.X, vector.Y);

	public static VectorInt ToPrometeInt(this System.Numerics.Vector2 vector) => new((int)vector.X, (int)vector.Y);

	public static System.Numerics.Vector2 ToNumerics(this Vector vector) => new(vector.X, vector.Y);

	public static Vector ToPromete(this System.Numerics.Vector2 vector) => new(vector.X, vector.Y);
}
