namespace Promete.Graphics.Fonts;

/// <summary>
/// グリフアトラス上のグリフを一意に識別するキーです。
/// </summary>
/// <param name="SourceId">グリフを提供した <see cref="IGlyphSource" /> の ID。</param>
/// <param name="GlyphIndex">ソース内におけるグリフのインデックス。</param>
/// <param name="Options">ラスタライズ時のオプション。</param>
internal readonly record struct GlyphKey(
    int SourceId,
    uint GlyphIndex,
    GlyphRenderOptions Options
);
