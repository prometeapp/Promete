using System;

namespace Promete.Graphics.Fonts;

/// <summary>
/// フォントの読み込みやグリフの生成に失敗した場合にスローされる例外です。
/// </summary>
public class FontException : Exception
{
    public FontException(string message)
        : base(message) { }

    public FontException(string message, Exception innerException)
        : base(message, innerException) { }
}
