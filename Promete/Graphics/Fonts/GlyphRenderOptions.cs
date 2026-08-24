using System;

namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフを取得・ラスタライズする際のオプションを表します。
/// この構造体はグリフアトラスのキャッシュキーの一部として使用されるため、
/// 同一の値であれば同一のグリフ画像が得られる必要があります。
/// </summary>
/// <param name="Size">
/// 目標とするフォントサイズ (ピクセル単位の高さ)。
/// グリフアトラスのキャッシュが際限なく増えないよう、
/// 指定した値は <see cref="SizeQuantum" /> 単位に丸められます。
/// </param>
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
    /// フォントサイズを丸める単位。
    /// </summary>
    /// <remarks>
    /// フォントサイズを連続的に変化させると、そのすべてが別のグリフとして
    /// アトラスへ積まれてしまいます。丸めることでキャッシュの種類を抑えます。
    /// </remarks>
    public const float SizeQuantum = 0.5f;

    /// <inheritdoc cref="GlyphRenderOptions(float, bool, FontStyle, int)" />
    /// <remarks>
    /// <c>with</c> 式による変更にも丸めが適用されるよう、設定時に丸めています。
    /// </remarks>
    public float Size
    {
        get;
        init => field = Quantize(value);
    } = Quantize(Size);

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

    /// <summary>
    /// フォントサイズを <see cref="SizeQuantum" /> 単位に丸めます。
    /// </summary>
    private static float Quantize(float size)
    {
        return MathF.Round(size / SizeQuantum, MidpointRounding.AwayFromZero) * SizeQuantum;
    }
}
