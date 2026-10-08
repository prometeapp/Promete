namespace Promete.Graphics;

/// <summary>
/// テクスチャの UV 座標が 0～1 の範囲を超えた場合の、サンプリング方法を表します。
/// </summary>
public enum TextureAddressMode
{
    /// <summary>
    /// 範囲外では、端のピクセルを引き伸ばします。
    /// </summary>
    Clamp = 0,

    /// <summary>
    /// 範囲外では、テクスチャを繰り返します。
    /// </summary>
    Repeat = 1,

    /// <summary>
    /// 範囲外では、テクスチャを反転しながら繰り返します。
    /// </summary>
    Mirror = 2,
}
