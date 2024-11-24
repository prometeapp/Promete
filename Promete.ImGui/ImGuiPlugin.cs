using System.Runtime.InteropServices;
using ImGuiNET;
using Promete.Windowing;
using Promete.Windowing.GLDesktop;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Promete.ImGui;

/// <summary>
///     ImGUI との連携を提供する Promete プラグインです。このクラスは継承できません。
///     本プラグインは、Prometeが OpenGL デスクトップバックエンドである場合にのみ使用できます。
/// </summary>
public sealed class ImGuiPlugin
{
    private readonly PrometeApp app;

    private readonly ImGuiController controller;
    private readonly IWindow window;

    private ImFontPtr font;
    private nint fontData;

    /// <summary>
    ///     ImGuiPlugin の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="app"></param>
    /// <param name="window"></param>
    /// <exception cref="NotSupportedException">OpenGL デスクトップバックエンドでない場合スローされます。</exception>
    public ImGuiPlugin(PrometeApp app, IWindow window)
    {
        this.window = window;
        this.app = app;
        // PrometeがOpenGLバックエンドでなければ例外をスローする
        if (window is not OpenGLDesktopWindow glWindow)
            throw new NotSupportedException("Promete.ImGui only supports OpenGL backend.");

        controller = new ImGuiController(glWindow.GL, glWindow.NativeWindow, window._RawInputContext, ConfigureImGui);

        this.window.Destroy += OnWindowDestroy;
        this.window.Render += OnWindowRender;
    }

    /// <summary>
    ///     ウィンドウのスケーリング値と同期するかどうかを取得または設定します。
    /// </summary>
    public bool IsSyncronizeWithWindowScaling { get; set; }

    private unsafe void ConfigureImGui()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.NativePtr->IniFilename = null;
    }

    private void OnWindowRender()
    {
        controller.Update(window.DeltaTime);
        if (IsSyncronizeWithWindowScaling) ImGuiNET.ImGui.GetIO().FontGlobalScale = window.Scale * window.PixelRatio;
        Render?.Invoke();
        controller.Render();
    }

    private void OnWindowDestroy()
    {
        controller.Dispose();
        Marshal.FreeCoTaskMem(fontData);
        font.Destroy();
    }

    public event Action? Render;
}
