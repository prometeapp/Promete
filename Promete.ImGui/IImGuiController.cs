namespace Promete.ImGui;

/// <summary>
/// バックエンドごとの ImGui コントローラの共通インターフェースです。
/// </summary>
internal interface IImGuiController : IDisposable
{
    /// <summary>フレームの状態を更新し、ImGui の新しいフレームを開始します。</summary>
    public void Update(float deltaTime);

    /// <summary>ImGui の描画データをレンダリングします。</summary>
    public void Render();
}

/// <summary>
/// Silk.NET の OpenGL 用 ImGuiController のアダプターです。
/// </summary>
internal sealed class OpenGLImGuiController(Silk.NET.OpenGL.Extensions.ImGui.ImGuiController controller)
    : IImGuiController
{
    public void Update(float deltaTime) => controller.Update(deltaTime);

    public void Render() => controller.Render();

    public void Dispose() => controller.Dispose();
}
