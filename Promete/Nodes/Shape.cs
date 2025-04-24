using System.Drawing;

namespace Promete.Nodes;

/// <summary>
/// プリミティブ図形のレンダリングを提供します。
/// </summary>
public class Shape : Node
{
    private Shape(Color c, ShapeType type, int lineWidth, Color? lineColor, params VectorInt[] vertices)
    {
        Color = c;
        LineWidth = lineWidth;
        LineColor = lineColor;

        Vertices = vertices;
        Type = type;
    }

    /// <summary>
    /// 図形の塗りつぶし色を取得します。
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// 線の幅を取得します。
    /// </summary>
    public int LineWidth { get; }

    /// <summary>
    /// 線の色を取得します。
    /// </summary>
    public Color? LineColor { get; }

    /// <summary>
    /// 図形の頂点配列を取得します。
    /// </summary>
    public VectorInt[] Vertices { get; }

    /// <summary>
    /// 図形の種類を取得します。
    /// </summary>
    public ShapeType Type { get; }

    /// <summary>
    /// ピクセルを作成します。
    /// </summary>
    /// <param name="p">ピクセルの位置</param>
    /// <param name="color">ピクセルの色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreatePixel(VectorInt p, Color color)
    {
        return new Shape(color, ShapeType.Pixel, 0, null, p);
    }

    /// <summary>
    /// ピクセルを作成します。
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    /// <param name="color">ピクセルの色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreatePixel(int x, int y, Color color)
    {
        return CreatePixel((x, y), color);
    }

    /// <summary>
    /// 線を作成します。
    /// </summary>
    /// <param name="start">開始点</param>
    /// <param name="end">終了点</param>
    /// <param name="color">線の色</param>
    /// <param name="lineWidth">線の幅</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateLine(VectorInt start, VectorInt end, Color color, int lineWidth = 1)
    {
        return new Shape(color, ShapeType.Line, lineWidth, null, start, end);
    }

    /// <summary>
    /// 線を作成します。
    /// </summary>
    /// <param name="sx">開始点のX座標</param>
    /// <param name="sy">開始点のY座標</param>
    /// <param name="ex">終了点のX座標</param>
    /// <param name="ey">終了点のY座標</param>
    /// <param name="color">線の色</param>
    /// <param name="lineWidth">線の幅</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateLine(int sx, int sy, int ex, int ey, Color color, int lineWidth = 1)
    {
        return CreateLine((sx, sy), (ex, ey), color, lineWidth);
    }

    /// <summary>
    /// 四角形を作成します。
    /// </summary>
    /// <param name="start">左上の点</param>
    /// <param name="end">右下の点</param>
    /// <param name="color">塗りつぶしの色</param>
    /// <param name="lineWidth">枠線の幅</param>
    /// <param name="lineColor">枠線の色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateRect(VectorInt start, VectorInt end, Color color, int lineWidth = 0,
        Color? lineColor = null)
    {
        return new Shape(color, ShapeType.Rect, lineWidth, lineColor,
            (start.X, start.Y),
            (start.X, end.Y),
            (end.X, end.Y),
            (end.X, start.Y)
        );
    }

    /// <summary>
    /// 四角形を作成します。
    /// </summary>
    /// <param name="sx">左上のX座標</param>
    /// <param name="sy">左上のY座標</param>
    /// <param name="ex">右下のX座標</param>
    /// <param name="ey">右下のY座標</param>
    /// <param name="color">塗りつぶしの色</param>
    /// <param name="lineWidth">枠線の幅</param>
    /// <param name="lineColor">枠線の色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateRect(int sx, int sy, int ex, int ey, Color color, int lineWidth = 0,
        Color? lineColor = null)
    {
        return CreateRect((sx, sy), (ex, ey), color, lineWidth, lineColor);
    }

    /// <summary>
    /// 三角形を作成します。
    /// </summary>
    /// <param name="v1">頂点1</param>
    /// <param name="v2">頂点2</param>
    /// <param name="v3">頂点3</param>
    /// <param name="color">塗りつぶしの色</param>
    /// <param name="lineWidth">枠線の幅</param>
    /// <param name="lineColor">枠線の色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateTriangle(VectorInt v1, VectorInt v2, VectorInt v3, Color color, int lineWidth = 0,
        Color? lineColor = null)
    {
        return new Shape(color, ShapeType.Triangle, lineWidth, lineColor, v1, v2, v3);
    }

    /// <summary>
    /// 三角形を作成します。
    /// </summary>
    /// <param name="x1">頂点1のX座標</param>
    /// <param name="y1">頂点1のY座標</param>
    /// <param name="x2">頂点2のX座標</param>
    /// <param name="y2">頂点2のY座標</param>
    /// <param name="x3">頂点3のX座標</param>
    /// <param name="y3">頂点3のY座標</param>
    /// <param name="color">塗りつぶしの色</param>
    /// <param name="lineWidth">枠線の幅</param>
    /// <param name="lineColor">枠線の色</param>
    /// <returns>作成されたShape</returns>
    public static Shape CreateTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color, int lineWidth = 0,
        Color? lineColor = null)
    {
        return CreateTriangle((x1, y1), (x2, y2), (x3, y3), color, lineWidth, lineColor);
    }
}
