using ImGuiNET;
using Promete.Backends.GL;
using Promete.Backends.SilkNetCommon;
using Promete.Backends.Vulkan;

namespace Promete.ImGui;

/// <summary>
/// ImGUI との連携を提供する Promete プラグインです。起動時のカスタマイズが必要な場合は、継承し、OnConfigureメソッドをオーバーライドしてください。
/// 本プラグインは、Prometeが OpenGL または Vulkan のデスクトップバックエンドである場合に使用できます。
/// </summary>
public class ImGuiPlugin(PrometeApp app, InputProvider provider) : IInitializable
{
    private IImGuiController? _controller;

    public event Action? Render;

    /// <summary>
    /// ウィンドウのスケーリング値と同期するかどうかを取得または設定します。
    /// </summary>
    public bool IsSyncronizeWithWindowScaling { get; set; }

    public void OnStart()
    {
        _controller = app.View switch
        {
            OpenGLDesktopGameView glView => new OpenGLImGuiController(
                new Silk.NET.OpenGL.Extensions.ImGui.ImGuiController(
                    glView.GL,
                    glView.NativeWindow,
                    provider.CreateInput(),
                    OnConfigure
                )
            ),
            VulkanDesktopGameView vkView => new VulkanImGuiController(
                vkView,
                provider.CreateInput(),
                OnConfigure
            ),
            _ => throw new NotSupportedException(
                "Promete.ImGui only supports OpenGL and Vulkan desktop backends."
            ),
        };

        app.Destroy += OnWindowDestroy;
        app.PostRender += OnWindowRender;
    }

    /// <summary>
    /// ImGUIの初期設定を行います。
    /// </summary>
    /// <param name="io"></param>
    protected virtual void OnConfigure(ImGuiIOPtr io) { }

    private unsafe void OnConfigure()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.NativePtr->IniFilename = null;
        OnConfigure(io);
    }

    private void OnWindowRender()
    {
        _controller.Update(app.Time.DeltaTime);
        if (IsSyncronizeWithWindowScaling)
            ImGuiNET.ImGui.GetIO().FontGlobalScale = app.View.Scale * app.View.PixelRatio;
        Render?.Invoke();
        _controller.Render();
    }

    private void OnWindowDestroy()
    {
        _controller.Dispose();
    }
}
