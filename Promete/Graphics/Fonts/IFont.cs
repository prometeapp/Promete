namespace Promete.Graphics.Fonts;

/// <summary>
///     Prometeで利用できるフォントを表すインターフェイス。
/// </summary>
public interface IFont
{
    /// <summary>
    ///     テキストを含む矩形のサイズを取得します。
    /// </summary>
    /// <param name="text">テキスト。</param>
    /// <param name="options">テキストの描画オプション。</param>
    /// <returns>テキストを含む矩形のサイズ。</returns>
    public Rect GetTextBounds(string text, TextRenderingOptions options);

    /// <summary>
    ///     指定した文字列を描画したテクスチャを生成します。
    /// </summary>
    /// <param name="factory">テクスチャの生成に使用するファクトリ。</param>
    /// <param name="text">描画するテキスト。</param>
    /// <param name="options">テキストの描画オプション。</param>
    /// <returns>生成されたテクスチャ。</returns>
    public Texture2D GenerateTexture(TextureFactory factory, string text, TextRenderingOptions options);
}
