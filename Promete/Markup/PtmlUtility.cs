namespace Promete.Markup;

/// <summary>
/// PTMLに関するユーティリティクラス。
/// </summary>
public static class PtmlUtility
{
    /// <summary>
    /// PTMLタグをエスケープします。
    /// </summary>
    /// <param name="ptml">PTML文字列。</param>
    /// <returns>エスケープされたPTMl文字列。</returns>
    public static string Encode(string ptml)
    {
        return ptml
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    /// エスケープされたPTMLタグをデコードします。
    /// </summary>
    /// <param name="ptml">エスケープされたPTML文字列。</param>
    /// <returns>デコードされたPTML文字列。</returns>
    public static string Decode(string ptml)
    {
        return ptml
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&");
    }
}
