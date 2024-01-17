using System;

namespace Promete.Elements.Renderer.GL;

internal static class VectorExtension
{
	private static float Dpi => /* TODO */ 1;

	public static Vector ToDeviceCoord(this Vector v)
		=> v * Dpi;

	public static Vector ToVirtualCoord(this Vector v)
		=> v / Dpi;

	public static VectorInt ToDeviceCoord(this VectorInt v)
		=> (VectorInt)((Vector)v * Dpi);

	public static VectorInt ToVirtualCoord(this VectorInt v)
		=> (VectorInt)((Vector)v / Dpi);
}
