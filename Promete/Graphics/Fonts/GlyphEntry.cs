namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフアトラスに登録済みのグリフを表します。
/// </summary>
/// <param name="Texture">アトラス上の該当領域を指すテクスチャ。</param>
/// <param name="Size">グリフ画像のサイズ。</param>
/// <param name="Bearing">
/// ベースライン原点から画像左上までのオフセット。Y 軸は下方向を正とします。
/// </param>
public readonly record struct GlyphEntry(Texture2D Texture, VectorInt Size, VectorInt Bearing)
{
    /// <summary>
    /// 描画すべき実体を持たないかどうかを取得します。
    /// </summary>
    public bool IsEmpty => Size.X <= 0 || Size.Y <= 0;
}
