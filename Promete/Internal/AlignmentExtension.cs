using System;
using SixLabors.ImageSharp;

namespace Promete.Internal;

internal static class ImageSharpConverterExtension
{
    internal static Color ToSixLabors(this System.Drawing.Color color)
    {
        return Color.FromRgba(color.R, color.G, color.B, color.A);
    }

    internal static SixLabors.Fonts.VerticalAlignment ToSixLabors(this VerticalAlignment alignment)
    {
        return alignment switch
        {
            VerticalAlignment.Top => SixLabors.Fonts.VerticalAlignment.Top,
            VerticalAlignment.Center => SixLabors.Fonts.VerticalAlignment.Center,
            VerticalAlignment.Bottom => SixLabors.Fonts.VerticalAlignment.Bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
        };
    }

    internal static SixLabors.Fonts.HorizontalAlignment ToSixLabors(this HorizontalAlignment alignment)
    {
        return alignment switch
        {
            HorizontalAlignment.Left => SixLabors.Fonts.HorizontalAlignment.Left,
            HorizontalAlignment.Center => SixLabors.Fonts.HorizontalAlignment.Center,
            HorizontalAlignment.Right => SixLabors.Fonts.HorizontalAlignment.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
        };
    }
}
