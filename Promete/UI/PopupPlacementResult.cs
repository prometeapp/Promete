namespace Promete.UI;

/// <summary>
/// <see cref="PopupPlacement.Compute"/> の計算結果です。
/// </summary>
/// <param name="Location">ポップアップの左上の座標。</param>
/// <param name="Side">実際に採用された、アンカーに対する表示位置。</param>
/// <param name="AvailableSize">その位置に表示できる最大のサイズ。ポップアップの中身がこれを超える場合は、スクロールなどで収める必要があります。</param>
public readonly record struct PopupPlacementResult(
    Vector Location,
    PopupSide Side,
    Vector AvailableSize
);
