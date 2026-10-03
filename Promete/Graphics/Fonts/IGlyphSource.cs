using System;

namespace Promete.Graphics.Fonts;

/// <summary>
/// コードポイントからグリフを供給するオブジェクトを表します。
/// ベクターフォント、画像ベースのビットマップフォント、外字などが実装します。
/// </summary>
/// <remarks>
/// テキストのレイアウトおよびラスタライズは、この抽象にのみ依存します。
/// 実装は「コードポイントからグリフを引く」ことだけに責任を持ち、
/// 行分割や整列といったレイアウトの責務は持ちません。
/// </remarks>
public interface IGlyphSource : IDisposable
{
    /// <summary>
    /// このソースを一意に識別する ID を取得します。
    /// グリフアトラスのキャッシュキーに使用されます。
    /// </summary>
    public int SourceId { get; }

    /// <summary>
    /// 指定したオプションにおけるフォントメトリクスを取得します。
    /// </summary>
    public FontMetrics GetMetrics(in GlyphRenderOptions options);

    /// <summary>
    /// 指定したコードポイントのグリフ情報を取得します。ラスタライズは行いません。
    /// </summary>
    /// <param name="codepoint">Unicode コードポイント。</param>
    /// <param name="options">グリフのオプション。</param>
    /// <param name="glyph">取得されたグリフ情報。</param>
    /// <returns>このソースがグリフを持っていれば <c>true</c>。</returns>
    public bool TryGetGlyph(int codepoint, in GlyphRenderOptions options, out GlyphInfo glyph);

    /// <summary>
    /// 指定したグリフをラスタライズします。グリフアトラスへの充填時にのみ呼ばれます。
    /// </summary>
    public GlyphBitmap Rasterize(in GlyphInfo glyph, in GlyphRenderOptions options);

    /// <summary>
    /// 2 つのコードポイント間のカーニング量を取得します。
    /// カーニングに対応しない場合は 0 を返します。
    /// </summary>
    public float GetKerning(int left, int right, in GlyphRenderOptions options);
}
