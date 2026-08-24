using System.Drawing;

namespace Promete.Graphics.Fonts;

/// <summary>
/// レイアウトによって配置位置が確定したグリフを表します。
/// </summary>
/// <param name="Glyph">配置されたグリフ。</param>
/// <param name="Options">このグリフのラスタライズに用いるオプション。</param>
/// <param name="Position">
/// ベースライン上のペン位置。実際の描画位置は、これにグリフのベアリングを加算して求めます。
/// </param>
/// <param name="Color">このグリフを描画する色。</param>
/// <param name="LineIndex">このグリフが属する行のインデックス。</param>
/// <param name="CharIndex">元のプレーンテキストにおける文字のインデックス。</param>
public readonly record struct PlacedGlyph(
    GlyphInfo Glyph,
    GlyphRenderOptions Options,
    VectorInt Position,
    Color Color,
    int LineIndex,
    int CharIndex
);
