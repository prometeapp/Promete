namespace Promete.Graphics.Fonts;

/// <summary>
/// 行を組み立てるために必要なグリフの情報を表します。ラスタライズは行われていません。
/// </summary>
/// <remarks>
/// 描画位置の決定に必要なベアリングやビットマップサイズは、
/// ラスタライズ結果である <see cref="GlyphBitmap" /> 側が保持します。
/// レイアウトは送り幅と行の高さのみで行を組み立てられるため、ここでは寸法を持ちません。
/// </remarks>
public readonly record struct GlyphInfo
{
    /// <summary>
    /// このグリフを提供する <see cref="IGlyphSource" /> を取得します。
    /// フォールバックチェーンを辿った結果、実際にグリフを持っていたソースが格納されます。
    /// </summary>
    public required IGlyphSource Source { get; init; }

    /// <summary>
    /// <see cref="Source" /> 内でのグリフのインデックスを取得します。
    /// </summary>
    public required uint GlyphIndex { get; init; }

    /// <summary>
    /// このグリフに対応するコードポイントを取得します。
    /// </summary>
    public required int Codepoint { get; init; }

    /// <summary>
    /// 次のグリフまでの送り幅を取得します。
    /// </summary>
    public required float Advance { get; init; }
}
