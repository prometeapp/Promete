namespace Promete.Graphics;

/// <summary>
/// テクスチャの生成時に指定する、サンプリングの設定です。
/// </summary>
/// <remarks>
/// 既定値 (<c>default</c>) は、ピクセルパーフェクトな 2D 描画に適した <see cref="TextureFilterMode.Nearest"/> と
/// <see cref="TextureAddressMode.Clamp"/> です。
/// </remarks>
/// <param name="Filter">拡大・縮小時の補間方法。</param>
/// <param name="Address">UV 座標が範囲外の場合のサンプリング方法。</param>
public readonly record struct TextureOptions(
    TextureFilterMode Filter = TextureFilterMode.Nearest,
    TextureAddressMode Address = TextureAddressMode.Clamp
)
{
    /// <summary>
    /// 既定の設定を取得します。
    /// </summary>
    public static TextureOptions Default => default;
}
