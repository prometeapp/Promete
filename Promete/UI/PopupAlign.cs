namespace Promete.UI;

/// <summary>
/// ポップアップを、アンカーに対して表示する辺と直交する方向にどう揃えるかを表します。
/// 上下に表示する場合は左右方向、左右に表示する場合は上下方向の揃え位置になります。
/// </summary>
public enum PopupAlign
{
    /// <summary>アンカーの左端 (上下に表示する場合) または上端 (左右に表示する場合) に揃えます。</summary>
    Start,

    /// <summary>アンカーの中央に揃えます。</summary>
    Center,

    /// <summary>アンカーの右端 (上下に表示する場合) または下端 (左右に表示する場合) に揃えます。</summary>
    End,
}
