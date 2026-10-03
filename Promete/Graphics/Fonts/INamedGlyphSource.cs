namespace Promete.Graphics.Fonts;

/// <summary>
/// 名前によってグリフを引くことができる <see cref="IGlyphSource" /> を表します。
/// </summary>
/// <remarks>
/// PTML の <c>&lt;tex=名前&gt;</c> による外字の差し込みは、この抽象を通じて解決されます。
/// </remarks>
public interface INamedGlyphSource
{
    /// <summary>
    /// 名前に対応するコードポイントの取得を試みます。
    /// </summary>
    /// <param name="name">グリフに割り当てられた名前。</param>
    /// <param name="codepoint">対応するコードポイント。</param>
    /// <returns>名前が登録されていれば <c>true</c>。</returns>
    public bool TryGetCodepointByName(string name, out int codepoint);
}
