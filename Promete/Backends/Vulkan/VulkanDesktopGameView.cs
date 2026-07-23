using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Promete.Platforms;
using Promete.Windowing;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using IWindow = Silk.NET.Windowing.IWindow;

namespace Promete.Backends.Vulkan;

/// <summary>
/// Vulkan デスクトップバックエンドにおける <see cref="IGameView"/> の実装です。
/// </summary>
public class VulkanDesktopGameView : IGameView
{
    private readonly PrometeApp _app;

    public VulkanDesktopGameView(PrometeApp app, IWindow window)
    {
        NativeWindow = window;
        _app = app;
        NativeWindow.Load += OnLoad;
        NativeWindow.Resize += OnResize;
        NativeWindow.FileDrop += OnFileDrop;
        window.FocusChanged += v => IsFocused = v;
    }

    public event Action<FileDroppedEventArgs>? FileDropped;
    public event Action? Resize;

    public IWindow NativeWindow { get; }

    public VectorInt Location
    {
        get => (NativeWindow.Position.X, NativeWindow.Position.Y);
        set => NativeWindow.Position = new Vector2D<int>(value.X, value.Y);
    }

    public VectorInt Size
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            UpdateWindowSize();
        }
    } = (640, 480);

    public VectorInt ActualSize =>
        new VectorInt(NativeWindow.FramebufferSize.X, NativeWindow.FramebufferSize.Y) / Scale;

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
            field = value;
            UpdateWindowSize();
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

    public bool IsVisible
    {
        get => NativeWindow.IsVisible;
        set => NativeWindow.IsVisible = value;
    }

    public bool IsFocused { get; private set; }

    public bool IsFullScreen
    {
        get => NativeWindow.WindowState == WindowState.Fullscreen;
        set => NativeWindow.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
    }

    public bool TopMost
    {
        get => NativeWindow.TopMost;
        set => NativeWindow.TopMost = value;
    }

    public float PixelRatio =>
        NativeWindow.Size.X == 0 ? 1 : NativeWindow.FramebufferSize.X / NativeWindow.Size.X;

    public string Title
    {
        get => NativeWindow.Title;
        set
        {
            if (NativeWindow.Title == value)
                return;
            NativeWindow.Title = value;
            MacNativeHelper.SetMenuBarTitle(value);
        }
    }

    public WindowMode Mode
    {
        get =>
            NativeWindow.WindowBorder switch
            {
                WindowBorder.Fixed => WindowMode.Fixed,
                WindowBorder.Hidden => WindowMode.NoFrame,
                WindowBorder.Resizable => WindowMode.Resizable,
                _ => throw new InvalidOperationException("unexpected window state"),
            };
        set =>
            NativeWindow.WindowBorder = value switch
            {
                WindowMode.Fixed => WindowBorder.Fixed,
                WindowMode.NoFrame => WindowBorder.Hidden,
                WindowMode.Resizable => WindowBorder.Resizable,
                _ => throw new ArgumentException(null, nameof(value)),
            };
    }

    // TODO: Phase 4 で vkCmdCopyImageToBuffer によるスクリーンショットを実装する
    public Texture2D TakeScreenshot()
    {
        throw new NotSupportedException(
            "Vulkan バックエンドはまだスクリーンショットをサポートしていません。"
        );
    }

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Vulkan バックエンドはまだスクリーンショットをサポートしていません。"
        );
    }

    public void UpdateWindowSize()
    {
        NativeWindow.Size = new Vector2D<int>(Size.X, Size.Y) * Scale;
    }

    private void OnLoad()
    {
        UpdateWindowSize();

        _app.OnStart();
    }

    private void OnResize(Vector2D<int> vec)
    {
        Size = (VectorInt)((Vector)ActualSize / PixelRatio);
        Resize?.Invoke();
    }

    private void OnFileDrop(string[] files)
    {
        FileDropped?.Invoke(new FileDroppedEventArgs(files));
    }
}
