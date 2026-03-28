using System.Drawing;
using Promete.Example.Kernel;
using Promete.ImGui;
using Promete.Input;
using Promete.Nodes;
using Promete.Windowing;
using ImGuiUI = ImGuiNET.ImGui;

namespace Promete.Example.examples.plugins;

[Demo("/plugins/imgui.demo", "ImGui example")]
public class ImGuiExampleScene(Keyboard keyboard, ImGuiPlugin imgui) : Scene
{
    private Sprite? ichigo;

    public override void OnStart()
    {
        Window.Mode = WindowMode.Resizable;
        imgui.Render += OnRender;
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp) App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        imgui.Render -= OnRender;
        ichigo?.Destroy();
        ichigo?.Texture?.Dispose();
    }

    private void OnRender()
    {
        ImGuiUI.Begin("ImGui Window");

        ImGuiUI.Text("Hello, ImGui from Promete!");
        if (ImGuiUI.Button($"{(ichigo == null ? "Show" : "Hide")} Ichigo")) ToggleIchigo();

        if (ichigo != null)
        {
            var alpha = (int)ichigo.TintColor.A;
            if (ImGuiUI.DragInt("alpha", ref alpha, 1, 0, 255)) ichigo.TintColor = Color.FromArgb(alpha, ichigo.TintColor);

            var vec = ichigo.Size.ToNumerics();
            if (ImGuiUI.DragFloat2("size", ref vec)) ichigo.Size = vec.ToPrometeInt();
        }

        if (ImGuiUI.Button("Back")) App.LoadScene<MainScene>();
        ImGuiUI.End();

        ImGuiUI.ShowDemoWindow();
    }

    private void ToggleIchigo()
    {
        if (ichigo == null)
        {
            var texture = App.Window.TextureFactory.Load("./assets/ichigo.png");
            ichigo = new Sprite(texture)
                .Location(16, 16)
                .Scale(4, 4);
            Root.Add(ichigo);
        }
        else
        {
            Root.Remove(ichigo);
            ichigo.Destroy();
            ichigo.Texture?.Dispose();
            ichigo = null;
        }
    }
}
