#pragma warning disable CS0067
using Promete.Backends;
using Promete.Backends.GL;
using Promete.Graphics;
using Promete.Windowing;
using Silk.NET.OpenGL;

namespace Promete.Web;

/// <summary>
/// ブラウザの canvas を画面とする <see cref="IGLGameView"/> です。
/// </summary>
internal sealed class WebGameView(GL gl, WindowOptions options) : IGLGameView
{
    public event Action<FileDroppedEventArgs>? FileDropped;

    public event Action? Resize;

    public GL GL { get; } = gl;

    public VectorInt FramebufferSize => Size * Scale;

    public VectorInt Location { get; set; } = options.Location;

    public VectorInt Size { get; set; } = options.Size;

    public VectorInt ActualSize => Size;

    public int Scale { get; set; } = options.Scale;

    public int X
    {
        get => Location.X;
        set => Location = (value, Y);
    }

    public int Y
    {
        get => Location.Y;
        set => Location = (X, value);
    }

    public int Width
    {
        get => Size.X;
        set => Size = (value, Height);
    }

    public int Height
    {
        get => Size.Y;
        set => Size = (Width, value);
    }

    public int ActualWidth => ActualSize.X;

    public int ActualHeight => ActualSize.Y;

    public bool IsVisible { get; set; } = true;

    public bool IsFocused => true;

    public bool IsFullScreen { get; set; } = options.IsFullScreen;

    public bool TopMost { get; set; }

    public float PixelRatio => 1f;

    public string Title { get; set; } = options.Title;

    public WindowMode Mode { get; set; } = options.Mode;

    public Texture2D TakeScreenshot() => throw new NotSupportedException();

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
