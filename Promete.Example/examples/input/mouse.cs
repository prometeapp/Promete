using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.input;

[Demo("input/mouse.demo", "マウスの使用例")]
public class mouse(Mouse mouse, Keyboard keyboard, ConsoleLayer console) : Scene
{
    private readonly List<Vector> _scrolls = [];

    public override void OnUpdate()
    {
        if (mouse.Scroll != Vector.Zero)
        {
            _scrolls.Add(mouse.Scroll);
        }

        console.Clear();
        console.Print("Press ESC to exit");
        console.Print($"Pos: {mouse.Position}");
        DebugButton(MouseButtonType.Left);
        DebugButton(MouseButtonType.Middle);
        DebugButton(MouseButtonType.Right);
        DebugButton(MouseButtonType.Side1);
        DebugButton(MouseButtonType.Side2);
        console.Print("Scrolls:");
        foreach (var item in _scrolls)
            console.Print("  " + item);

        if (keyboard.Escape.IsKeyDown)
        {
            App.LoadScene<MainScene>();
        }
    }

    private void DebugButton(MouseButtonType type)
    {
        var btn = mouse[type];
        console.Print(
            $"{type.ToString(),-10} Press:{btn.IsPressed} Down:{btn.IsButtonDown}\tUp:{btn.IsButtonUp}\tTime:{btn.ElapsedTime}\t\tFrame:{btn.ElapsedFrameCount}");
    }
}
