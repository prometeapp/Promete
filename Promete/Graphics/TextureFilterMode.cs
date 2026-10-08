namespace Promete.Graphics;

/// <summary>
/// テクスチャを拡大・縮小して描画する際の、ピクセルの補間方法を表します。
/// </summary>
public enum TextureFilterMode
{
    /// <summary>
    /// 最近傍補間です。ピクセルの境界がくっきりと保たれます。
    /// </summary>
    Nearest = 0,

    /// <summary>
    /// 線形補間です。ピクセルの境界が滑らかになります。
    /// </summary>
    Linear = 1,
}
