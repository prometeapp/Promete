namespace Promete.Graphics.Fonts;

/// <summary>
/// ラスタライズされたグリフの画像を表します。
/// ピクセル形式は RGBA8888 に正規化されており、
/// 単色グリフの場合は RGB が白、アルファに濃度が格納されます。
/// </summary>
/// <param name="Pixels">RGBA8888 形式のピクセルデータ。</param>
/// <param name="Size">画像のサイズ。</param>
/// <param name="Bearing">
/// ベースライン原点から画像左上までのオフセット。
/// Y 軸は下方向を正とするため、通常は負の値になります。
/// </param>
public readonly record struct GlyphBitmap(byte[] Pixels, VectorInt Size, VectorInt Bearing)
{
    /// <summary>
    /// 空のグリフ画像を取得します。
    /// </summary>
    public static readonly GlyphBitmap Empty = new([], VectorInt.Zero, VectorInt.Zero);

    /// <summary>
    /// 描画すべき実体を持たないかどうかを取得します。
    /// </summary>
    public bool IsEmpty => Size.X <= 0 || Size.Y <= 0;
}
