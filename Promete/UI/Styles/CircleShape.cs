using System;
using System.Drawing;
using Promete.Nodes;

namespace Promete.UI.Styles;

/// <summary>
/// Shape に円がないため、1px の高さの横線を積み重ねて塗りつぶした円を作る、スタイル用のヘルパーです。
/// </summary>
internal static class CircleShape
{
    /// <summary>
    /// 塗りつぶした円を作ります。
    /// </summary>
    /// <param name="centerX">中心のX座標。</param>
    /// <param name="centerY">中心のY座標。</param>
    /// <param name="radius">半径。</param>
    /// <param name="color">塗りつぶしの色。</param>
    public static Container Filled(float centerX, float centerY, float radius, Color color)
    {
        var container = new Container();
        var top = (int)MathF.Floor(centerY - radius);
        var bottom = (int)MathF.Ceiling(centerY + radius);

        for (var y = top; y < bottom; y++)
        {
            // 行の中心から円の中心までの距離で、その行の左右の端を求める
            var dy = y + 0.5f - centerY;
            var squared = radius * radius - dy * dy;
            if (squared <= 0)
                continue;

            var half = MathF.Sqrt(squared);
            var left = (int)MathF.Round(centerX - half);
            var right = (int)MathF.Round(centerX + half);
            if (right <= left)
                continue;

            container.Add(Shape.CreateRect((left, y), (right, y + 1), color));
        }

        return container;
    }
}
