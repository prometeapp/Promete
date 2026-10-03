namespace Promete.Graphics.Fonts;

/// <summary>
/// テキストを折り返す方法を定義します。
/// </summary>
public enum WrapMode
{
    /// <summary>
    /// 折り返しを行いません。改行は明示的な改行文字によってのみ発生します。
    /// </summary>
    None,

    /// <summary>
    /// 幅を超えた位置の文字で折り返します。単語の途中でも改行されます。
    /// </summary>
    Character,

    /// <summary>
    /// 単語の区切りで折り返します。
    /// </summary>
    Word,

    /// <summary>
    /// 和文は任意の文字で、欧文は単語の区切りで折り返します。
    /// 和欧混在のテキストに適しています。
    /// </summary>
    Mixed,
}
