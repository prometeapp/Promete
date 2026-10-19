using System;

namespace Promete.UI;

/// <summary>
/// アンカー (ドロップダウン本体など) を基準にして、ポップアップを画面内に収まる位置へ配置する計算を提供します。
/// </summary>
public static class PopupPlacement
{
    /// <summary>
    /// ポップアップの表示位置を計算します。
    /// </summary>
    /// <remarks>
    /// <paramref name="preferred"/> の辺に収まればそこに、収まらなければ反対側に配置します。
    /// どちらにも収まらない場合は、空きが広い方に配置します。
    /// アンカーと直交する方向は <paramref name="align"/> で揃えたうえで、画面内に収まるよう補正します。
    /// </remarks>
    /// <param name="anchor">アンカーの矩形 (画面上の座標)。右端・下端は <c>Location + Size</c> として扱います。</param>
    /// <param name="popupSize">ポップアップのサイズ。</param>
    /// <param name="viewSize">画面のサイズ。</param>
    /// <param name="preferred">優先する表示位置。</param>
    /// <param name="align">アンカーに対する揃え位置。</param>
    /// <param name="gap">アンカーとポップアップの間隔。</param>
    public static PopupPlacementResult Compute(
        Rect anchor,
        Vector popupSize,
        Vector viewSize,
        PopupSide preferred = PopupSide.Bottom,
        PopupAlign align = PopupAlign.Start,
        float gap = 0
    )
    {
        var side = ChooseSide(anchor, popupSize, viewSize, preferred, gap);
        var isVertical = side is PopupSide.Bottom or PopupSide.Top;

        // 表示する辺の方向 (主軸) の位置
        var primary = side switch
        {
            PopupSide.Bottom => anchor.Top + anchor.Height + gap,
            PopupSide.Top => anchor.Top - gap - popupSize.Y,
            PopupSide.Right => anchor.Left + anchor.Width + gap,
            _ => anchor.Left - gap - popupSize.X,
        };

        // 主軸と直交する方向の位置
        var cross = isVertical
            ? AlignAxis(anchor.Left, anchor.Width, popupSize.X, align)
            : AlignAxis(anchor.Top, anchor.Height, popupSize.Y, align);

        var location = isVertical
            ? new Vector(
                ClampToView(cross, popupSize.X, viewSize.X),
                ClampToView(primary, popupSize.Y, viewSize.Y)
            )
            : new Vector(
                ClampToView(primary, popupSize.X, viewSize.X),
                ClampToView(cross, popupSize.Y, viewSize.Y)
            );

        var space = Math.Max(0, SpaceOf(side, anchor, viewSize, gap));
        var available = isVertical ? new Vector(viewSize.X, space) : new Vector(space, viewSize.Y);
        return new PopupPlacementResult(location, side, available);
    }

    private static PopupSide ChooseSide(
        Rect anchor,
        Vector popupSize,
        Vector viewSize,
        PopupSide preferred,
        float gap
    )
    {
        var opposite = Opposite(preferred);
        var preferredSpace = SpaceOf(preferred, anchor, viewSize, gap);
        var oppositeSpace = SpaceOf(opposite, anchor, viewSize, gap);

        if (preferredSpace >= Extent(preferred, popupSize))
            return preferred;
        if (oppositeSpace >= Extent(opposite, popupSize))
            return opposite;

        // どちらにも収まらない場合は、空きが広い方 (同じなら優先側)
        return oppositeSpace > preferredSpace ? opposite : preferred;
    }

    private static float SpaceOf(PopupSide side, Rect anchor, Vector viewSize, float gap) =>
        side switch
        {
            PopupSide.Bottom => viewSize.Y - (anchor.Top + anchor.Height) - gap,
            PopupSide.Top => anchor.Top - gap,
            PopupSide.Right => viewSize.X - (anchor.Left + anchor.Width) - gap,
            _ => anchor.Left - gap,
        };

    private static float Extent(PopupSide side, Vector popupSize) =>
        side is PopupSide.Bottom or PopupSide.Top ? popupSize.Y : popupSize.X;

    private static PopupSide Opposite(PopupSide side) =>
        side switch
        {
            PopupSide.Bottom => PopupSide.Top,
            PopupSide.Top => PopupSide.Bottom,
            PopupSide.Right => PopupSide.Left,
            _ => PopupSide.Right,
        };

    private static float AlignAxis(
        float anchorStart,
        float anchorLength,
        float popupLength,
        PopupAlign align
    ) =>
        align switch
        {
            PopupAlign.Center => anchorStart + (anchorLength - popupLength) / 2f,
            PopupAlign.End => anchorStart + anchorLength - popupLength,
            _ => anchorStart,
        };

    private static float ClampToView(float value, float popupLength, float viewLength) =>
        popupLength >= viewLength ? 0 : Math.Clamp(value, 0, viewLength - popupLength);
}
