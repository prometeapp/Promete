namespace Promete.Graphics.Fonts;

/// <summary>
/// フォント全体に関わる、ピクセル単位に解決済みのメトリクスを表します。
/// </summary>
/// <param name="Ascender">ベースラインから上方向への最大の高さ。正の値。</param>
/// <param name="Descender">ベースラインから下方向への最大の深さ。負の値。</param>
/// <param name="LineHeight">行送りの既定値。</param>
public readonly record struct FontMetrics(float Ascender, float Descender, float LineHeight)
{
    /// <summary>
    /// アセンダーからディセンダーまでの高さを取得します。
    /// </summary>
    public float Height => Ascender - Descender;
}
