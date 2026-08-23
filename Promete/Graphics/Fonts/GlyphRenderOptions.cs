namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフを取得・ラスタライズする際のオプションを表します。
/// この構造体はグリフアトラスのキャッシュキーの一部として使用されるため、
/// 同一の値であれば同一のグリフ画像が得られる必要があります。
/// </summary>
/// <param name="Size">目標とするフォントサイズ (ピクセル単位の高さ)。</param>
/// <param name="IsAntialiased">アンチエイリアスを有効にするかどうか。</param>
/// <param name="Style">フォントスタイル。専用の字形を持たない場合は合成されます。</param>
/// <param name="BorderThickness">縁取りの太さ。0 の場合は縁取りを行いません。</param>
public readonly record struct GlyphRenderOptions(
    float Size = 16,
    bool IsAntialiased = true,
    FontStyle Style = FontStyle.Normal,
    int BorderThickness = 0
)
{
    /// <summary>
    /// 縁取りを行うかどうかを取得します。
    /// </summary>
    public bool HasBorder => BorderThickness > 0;

    /// <summary>
    /// 太字を合成する必要があるかどうかを取得します。
    /// </summary>
    public bool IsBold => Style is FontStyle.Bold or FontStyle.BoldItalic;

    /// <summary>
    /// 斜体を合成する必要があるかどうかを取得します。
    /// </summary>
    public bool IsItalic => Style is FontStyle.Italic or FontStyle.BoldItalic;
}
