namespace Promete.Graphics.Fonts;

/// <summary>
/// 禁則処理の方法を定義します。
/// </summary>
public enum KinsokuMode
{
    /// <summary>
    /// 禁則処理を行いません。
    /// </summary>
    None,

    /// <summary>
    /// 句読点や閉じ括弧を行頭に、開き括弧を行末に置かないようにします。
    /// </summary>
    Standard,
}
