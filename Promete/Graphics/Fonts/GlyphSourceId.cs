using System.Threading;

namespace Promete.Graphics.Fonts;

/// <summary>
/// <see cref="IGlyphSource" /> の識別子を採番します。
/// </summary>
/// <remarks>
/// 識別子はグリフアトラスのキャッシュキーに使われるため、
/// 実装の種類を問わずプロセス全体で一意である必要があります。
/// <see cref="IGlyphSource" /> を実装する場合は、必ずここから採番してください。
/// </remarks>
public static class GlyphSourceId
{
    private static int _next;

    /// <summary>
    /// 新しい識別子を採番します。
    /// </summary>
    public static int Next()
    {
        return Interlocked.Increment(ref _next);
    }
}
