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
/// <remarks>
/// <see cref="Size"/>、<see cref="Scale"/>、<see cref="Title"/> は、canvas とドキュメントに反映します。
/// canvas の描画バッファは <c>Size * Scale</c> で、拡大はピクセルを保ったまま行います。
/// それ以外の機能 (<see cref="GameViewFeature"/>) には対応せず、設定しても値を保持するだけです。
/// スクリーンショットは <see cref="NotSupportedException"/> をスローします。
/// </remarks>
internal sealed class WebGameView : IGLGameView
{
    private readonly string _canvasSelector;

    public WebGameView(GL gl, WindowOptions options, string canvasSelector)
    {
        GL = gl;
        _canvasSelector = canvasSelector;
        Location = options.Location;
        IsFullScreen = options.IsFullScreen;
        Mode = options.Mode;
        Size = options.Size;
        Scale = options.Scale;
        Title = options.Title;
    }

    public event Action<FileDroppedEventArgs>? FileDropped;

    public event Action? Resize;

    public GL GL { get; }

    public VectorInt FramebufferSize => Size * Scale;

    public VectorInt Location { get; set; }

    public VectorInt Size
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            ApplyCanvasSize();
        }
    }

    public VectorInt ActualSize => Size;

    public int Scale
    {
        get;
        set
        {
            if (value is not 1 and not 2 and not 4 and not 8)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Scale must be 1, 2, 4, or 8."
                );
            if (field == value)
                return;
            field = value;
            ApplyCanvasSize();
        }
    } = 1;

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

    public bool IsFullScreen { get; set; }

    public bool TopMost { get; set; }

    public float PixelRatio => 1f;

    public string Title
    {
        get;
        set
        {
            field = value;
            CanvasInterop.SetTitle(value);
        }
    } = string.Empty;

    public WindowMode Mode { get; set; }

    public Texture2D TakeScreenshot() => throw new NotSupportedException();

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public bool IsSupported(GameViewFeature feature) => feature is GameViewFeature.Title;

    private void ApplyCanvasSize()
    {
        var size = FramebufferSize;
        CanvasInterop.SetCanvasSize(_canvasSelector, size.X, size.Y);
        Resize?.Invoke();
    }
}
