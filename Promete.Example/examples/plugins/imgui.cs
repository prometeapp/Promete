using System.Drawing;
using Promete.Example.Kernel;
using Promete.ImGui;
using Promete.Input;
using Promete.Nodes;
using Promete.Windowing;
using UI = ImGuiNET.ImGui;

namespace Promete.Example.examples.plugins;

[Demo("/plugins/imgui.demo", "ImGui example")]
public class ImGuiExampleScene(Keyboard keyboard, ImGuiPlugin imgui) : Scene
{
    private Sprite? _ichigo;

    public override void OnStart()
    {
        View.Mode = WindowMode.Resizable;
        imgui.Render += OnRender;
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        imgui.Render -= OnRender;
        _ichigo?.Destroy();
        _ichigo?.Texture?.Dispose();
    }

    private void OnRender()
    {
        UI.Begin("ImGui Window");

        UI.Text("Hello, ImGui from Promete!");
        if (UI.Button($"{(_ichigo == null ? "Show" : "Hide")} Ichigo"))
            ToggleIchigo();

        if (_ichigo != null)
        {
            var alpha = (int)_ichigo.TintColor.A;
            if (UI.DragInt("alpha", ref alpha, 1, 0, 255))
                _ichigo.TintColor = Color.FromArgb(alpha, _ichigo.TintColor);

            var vec = _ichigo.Size.ToNumerics();
            if (UI.DragFloat2("size", ref vec))
                _ichigo.Size = vec.ToPrometeInt();
        }

        if (UI.Button("Back"))
            App.LoadScene<MainScene>();
        UI.End();

        UI.ShowDemoWindow();
    }

    private void ToggleIchigo()
    {
        if (_ichigo == null)
        {
            var texture = App.TextureFactory.Load("./assets/ichigo.png");
            _ichigo = new Sprite(texture).Location(16, 16).Scale(4, 4);
            Root.Add(_ichigo);
        }
        else
        {
            Root.Remove(_ichigo);
            _ichigo.Destroy();
            _ichigo.Texture?.Dispose();
            _ichigo = null;
        }
    }
}
