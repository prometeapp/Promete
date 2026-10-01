namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// netstandard2.0 には record / init が要求するこの型が存在しないため補う。
    /// ジェネレータは Roslyn にロードされる都合で netstandard2.0 固定であり、回避できない。
    /// </summary>
    internal static class IsExternalInit { }
}
