using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Promete.Windowing;

namespace Promete.Backends.Headless;

public class HeadlessGameView : IGameView
{
    public VectorInt Location { get; set; }
    public VectorInt Size { get; set; }
    public VectorInt ActualSize => Size;
    public int Scale { get; set; } = 1;
    public int X { get => Location.X; set => Location = (value, Y); }
    public int Y { get => Location.Y; set => Location = (X, value); }
    public int Width { get => Size.X; set => Size = (value, Height); }
    public int Height { get => Size.Y; set => Size = (Width, value); }
    public int ActualWidth => Width;
    public int ActualHeight => Height;
    public bool IsVisible { get; set; } = true;
    public bool IsFocused => true;
    public bool IsFullScreen { get; set; }
    public bool TopMost { get; set; }
    public float PixelRatio => 1f;
    public string Title { get; set; } = "";
    public WindowMode Mode { get; set; }

    public Texture2D TakeScreenshot() => default;

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default) => Task.CompletedTask;

    public event Action<FileDroppedEventArgs>? FileDropped;
    public event Action? Resize;
}
