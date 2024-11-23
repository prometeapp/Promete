using System.Drawing;

namespace Promete.Nodes;

/// <summary>
/// Provide rendering primitive shapes.
/// </summary>
public class Shape : Node
{
	public Color Color { get; }
	public int LineWidth { get; }
	public Color? LineColor { get; }
	public VectorInt[] Vertices { get; }
	public ShapeType Type { get; }

	private Shape(Color c, ShapeType type, int lineWidth, Color? lineColor, params VectorInt[] vertices)
	{
		Color = c;
		LineWidth = lineWidth;
		LineColor = lineColor;

		Vertices = vertices;
		Type = type;
	}

	public static Shape CreatePixel(VectorInt p, Color color)
		=> new(color, ShapeType.Pixel, 0, null, p);

	public static Shape CreatePixel(int x, int y, Color color)
		=> CreatePixel((x, y), color);

	public static Shape CreateLine(VectorInt start, VectorInt end, Color color, int lineWidth = 1)
		=> new(color, ShapeType.Line, lineWidth, null, start, end);

	public static Shape CreateLine(int sx, int sy, int ex, int ey, Color color, int lineWidth = 1)
		=> CreateLine((sx, sy), (ex, ey), color, lineWidth);

	public static Shape CreateRect(VectorInt start, VectorInt end, Color color, int lineWidth = 0,
		Color? lineColor = null)
		=> new(color, ShapeType.Rect, lineWidth, lineColor,
			(start.X, start.Y),
			(start.X, end.Y),
			(end.X, end.Y),
			(end.X, start.Y)
		);

	public static Shape CreateRect(int sx, int sy, int ex, int ey, Color color, int lineWidth = 0,
		Color? lineColor = null)
		=> CreateRect((sx, sy), (ex, ey), color, lineWidth, lineColor);

	public static Shape CreateTriangle(VectorInt v1, VectorInt v2, VectorInt v3, Color color, int lineWidth = 0,
		Color? lineColor = null)
		=> new(color, ShapeType.Triangle, lineWidth, lineColor, v1, v2, v3);

	public static Shape CreateTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color, int lineWidth = 0,
		Color? lineColor = null)
		=> CreateTriangle((x1, y1), (x2, y2), (x3, y3), color, lineWidth, lineColor);
}
