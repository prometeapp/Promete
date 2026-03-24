using ImGuiNET;
using Promete.Backends.GL;
using Promete.Backends.SilkNetCommon;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Promete.ImGui;

/// <summary>
/// ImGUI との連携を提供する Promete プラグインです。起動時のカスタマイズが必要な場合は、継承し、OnConfigureメソッドをオーバーライドしてください。
/// 本プラグインは、Prometeが OpenGL デスクトップバックエンドである場合にのみ使用できます。
/// </summary>
public class ImGuiPlugin(PrometeApp app, InputProvider provider) : IInitializable
{
    private ImGuiController _controller;

    public void OnStart()
    {
        // PrometeがOpenGLバックエンドでなければ例外をスローする
        if (app.View is not OpenGLDesktopGameView glView)
            throw new NotSupportedException("Promete.ImGui only supports OpenGL backend.");

        _controller = new ImGuiController(glView.GL, glView.NativeWindow, provider.CreateInput(), OnConfigure);

        app.Destroy += OnWindowDestroy;
        app.PostRender += OnWindowRender;
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
        _controller.Update(app.Time.DeltaTime);
        if (IsSyncronizeWithWindowScaling) ImGuiNET.ImGui.GetIO().FontGlobalScale = app.View.Scale * app.View.PixelRatio;
        Render?.Invoke();
        _controller.Render();
    }

    private void OnWindowDestroy()
    {
        _controller.Dispose();
    }

    public event Action? Render;
}
