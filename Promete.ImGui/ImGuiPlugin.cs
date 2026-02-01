using System.Runtime.InteropServices;
using ImGuiNET;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Promete.ImGui;

/// <summary>
/// ImGUI との連携を提供する Promete プラグインです。起動時のカスタマイズが必要な場合は、継承し、OnConfigureメソッドをオーバーライドしてください。
/// 本プラグインは、Prometeが OpenGL デスクトップバックエンドである場合にのみ使用できます。
/// </summary>
public class ImGuiPlugin : IInitializable
{
    private ImGuiController _controller;

    private readonly PrometeApp _app;
    private readonly IWindow _window;

    /// <summary>
    /// ImGuiPlugin の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="app"></param>
    /// <param name="window"></param>
    /// <exception cref="NotSupportedException">OpenGL デスクトップバックエンドでない場合スローされます。</exception>
    public ImGuiPlugin(PrometeApp app, IWindow window)
    {
        _window = window;
        _app = app;

        _window.Destroy += OnWindowDestroy;
        _window.Render += OnWindowRender;
    }

    public void OnStart()
    {
        // PrometeがOpenGLバックエンドでなければ例外をスローする
        if (_window is not OpenGLDesktopWindow glWindow)
            throw new NotSupportedException("Promete.ImGui only supports OpenGL backend.");

        _controller = new ImGuiController(glWindow.GL, glWindow.NativeWindow, glWindow._RawInputContext, OnConfigure);
    }

    /// <summary>
    /// ウィンドウのスケーリング値と同期するかどうかを取得または設定します。
    /// </summary>
    public bool IsSyncronizeWithWindowScaling { get; set; }

    /// <summary>
    /// ImGUIの初期設定を行います。
    /// </summary>
    /// <param name="io"></param>
    protected virtual void OnConfigure(ImGuiIOPtr io)
    {
    }

    private unsafe void OnConfigure()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.NativePtr->IniFilename = null;
        OnConfigure(io);
    }

    private void OnWindowRender()
    {
        _controller.Update(_window.DeltaTime);
        if (IsSyncronizeWithWindowScaling) ImGuiNET.ImGui.GetIO().FontGlobalScale = _window.Scale * _window.PixelRatio;
        Render?.Invoke();
        _controller.Render();
    }

    private void OnWindowDestroy()
    {
        _controller.Dispose();
    }

    public event Action? Render;
}
