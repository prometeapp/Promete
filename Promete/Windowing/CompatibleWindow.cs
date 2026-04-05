#pragma warning disable CS0618 // 型またはメンバーが旧型式です
using System;
using System.Threading;
using System.Threading.Tasks;
using Promete.Graphics;
using Silk.NET.Input;

namespace Promete.Windowing;

/// <summary>
/// 旧来の <see cref="IWindow"/> インターフェイスとの互換性を維持するための実装。
/// </summary>
public class CompatibleWindow(PrometeApp app) : IWindow
{
    public event Action? Start
    {
        add => app.Start += value;
        remove => app.Start -= value;
    }

    public event Action? Update
    {
        add => app.Update += value;
        remove => app.Update -= value;
    }

    public event Action? Render
    {
        add => app.Render += value;
        remove => app.Render -= value;
    }

    public event Action? Destroy
    {
        add => app.Destroy += value;
        remove => app.Destroy -= value;
    }

    public event Action? PreUpdate
    {
        add => app.PreUpdate += value;
        remove => app.PreUpdate -= value;
    }

    public event Action? PostUpdate
    {
        add => app.PostUpdate += value;
        remove => app.PostUpdate -= value;
    }

    public event Action<FileDroppedEventArgs>? FileDropped
    {
        add => app.View.FileDropped += value;
        remove => app.View.FileDropped -= value;
    }

    public event Action? Resize
    {
        add => app.View.Resize += value;
        remove => app.View.Resize -= value;
    }

    public VectorInt Location
    {
        get => app.View.Location;
        set => app.View.Location = value;
    }

    public VectorInt Size
    {
        get => app.View.Size;
        set => app.View.Size = value;
    }

    public VectorInt ActualSize => app.View.ActualSize;

    public int Scale
    {
        get => app.View.Scale;
        set => app.View.Scale = value;
    }

    public int X
    {
        get => app.View.X;
        set => app.View.X = value;
    }

    public int Y
    {
        get => app.View.Y;
        set => app.View.Y = value;
    }

    public int Width
    {
        get => app.View.Width;
        set => app.View.Width = value;
    }

    public int Height
    {
        get => app.View.Height;
        set => app.View.Height = value;
    }

    public int ActualWidth => app.View.ActualWidth;
    public int ActualHeight => app.View.ActualHeight;

    public bool IsVisible
    {
        get => app.View.IsVisible;
        set => app.View.IsVisible = value;
    }

    public bool IsFocused => app.View.IsFocused;

    public bool IsFullScreen
    {
        get => app.View.IsFullScreen;
        set => app.View.IsFullScreen = value;
    }

    public bool TopMost
    {
        get => app.View.TopMost;
        set => app.View.TopMost = value;
    }

    public float TotalTime => app.Time.TotalTime;
    public float DeltaTime => app.Time.DeltaTime;
    public long FramePerSeconds => app.Time.FramePerSeconds;
    public long UpdatePerSeconds => app.Time.UpdatePerSeconds;
    public long TotalFrame => app.Time.TotalFrame;

    // 初期化後の変更はバックエンドに反映されないため、互換用のスタブ
    public bool IsVsyncMode { get; set; }

    public int TargetFps
    {
        get => app.Time.TargetFps;
        set => app.Time.TargetFps = value;
    }

    public int TargetUps
    {
        get => app.Time.TargetUps;
        set => app.Time.TargetUps = value;
    }

    public float TimeScale
    {
        get => app.Time.TimeScale;
        set => app.Time.TimeScale = value;
    }

    public float TotalTimeWithoutScale => app.Time.TotalTimeWithoutScale;
    public float PixelRatio => app.View.PixelRatio;

    public string Title
    {
        get => app.View.Title;
        set => app.View.Title = value;
    }

    public WindowMode Mode
    {
        get => app.View.Mode;
        set => app.View.Mode = value;
    }

    public IInputContext? _RawInputContext => app.TryGetPlugin<IInputContext>(out var ctx) ? ctx : null;

    public TextureFactoryBase TextureFactory => app.TextureFactory;

    public void Run(WindowOptions opts)
    {
        throw new NotSupportedException("CompatibleWindow.Run() はサポートされていません。app.Run() を使用してください。");
    }

    public void Exit() => app.Exit();

    public Texture2D TakeScreenshot() => app.View.TakeScreenshot();

    public Task SaveScreenshotAsync(string path, CancellationToken ct = default) =>
        app.View.SaveScreenshotAsync(path, ct);
}
