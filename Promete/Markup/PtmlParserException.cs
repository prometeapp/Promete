using System;

namespace Promete.Markup;

public class PtmlParserException(string message, int position) : Exception($"PTML parsing error at the position {position}:\n{message}");
