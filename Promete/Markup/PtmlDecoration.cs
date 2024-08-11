using System;

namespace Promete.Markup;

public record struct PtmlDecoration(int Start, int End, string TagName, string Attribute);
