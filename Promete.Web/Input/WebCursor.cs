using Silk.NET.Core;
using Silk.NET.Input;

namespace Promete.Web.Input;

/// <summary>
/// 何もしない <see cref="ICursor"/> です。
/// </summary>
internal sealed class WebCursor : ICursor
{
    public CursorType Type { get; set; }

    public StandardCursor StandardCursor { get; set; }

    public CursorMode CursorMode { get; set; }

    public bool IsConfined { get; set; }

    public int HotspotX { get; set; }

    public int HotspotY { get; set; }

    public RawImage Image { get; set; }

    public bool IsSupported(CursorMode mode) => mode == CursorMode.Normal;

    public bool IsSupported(StandardCursor standardCursor) => false;
}
