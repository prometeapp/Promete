using System;

namespace Promete.Markup;

/// <summary>
/// PTMLの解析中にエラーが発生した場合にスローされる例外です。
/// </summary>
public class PtmlParserException(string message, int position)
    : Exception($"PTML parsing error at the position {position}:\n{message}");
