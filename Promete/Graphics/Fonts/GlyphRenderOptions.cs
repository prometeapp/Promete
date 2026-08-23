namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフを取得・ラスタライズする際のオプションを表します。
/// この構造体はグリフアトラスのキャッシュキーの一部として使用されるため、
/// 同一の値であれば同一のグリフ画像が得られる必要があります。
/// </summary>
/// <param name="Size">目標とするフォントサイズ (ピクセル単位の高さ)。</param>
/// <param name="IsAntialiased">アンチエイリアスを有効にするかどうか。</param>
/// <param name="BorderThickness">縁取りの太さ。0 の場合は縁取りを行いません。</param>
public readonly record struct GlyphRenderOptions(
    float Size = 16,
    bool IsAntialiased = true,
    int BorderThickness = 0
)
{
    /// <summary>
    /// 縁取りを行うかどうかを取得します。
    /// </summary>
    public bool HasBorder => BorderThickness > 0;
}
