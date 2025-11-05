using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples;

/// <summary>
/// シーンを使わずに PrometeApp を実行するデモ
/// </summary>
[Demo("/sceneless", "シーンを使わずに起動")]
public class ScenelessDemo(Keyboard keyboard, ConsoleLayer console) : Scene
{
    private int frameCount;

    public override void OnStart()
    {
        console.Print("Sceneless Demo");
        console.Print("このデモは、将来的にはシーンなしで実行できます。");
        console.Print("");
        console.Print("例:");
        console.Print("var app = PrometeApp.Create()");
        console.Print("    .Use<Keyboard>()");
        console.Print("    .Use<ConsoleLayer>()");
        console.Print("    .BuildWithOpenGLDesktop();");
        console.Print("");
        console.Print("app.Window.Start += () => {");
        console.Print("    console.Print(\"Hello, world!\");");
        console.Print("};");
        console.Print("");
        console.Print("app.Window.Update += () => {");
        console.Print("    if (keyboard.Escape.IsKeyDown)");
        console.Print("        app.Exit(0);");
        console.Print("};");
        console.Print("");
        console.Print("return app.Run();");
        console.Print("");
        console.Print("[ESC] 戻る");
    }

    public override void OnUpdate()
    {
        frameCount++;
        
        if (frameCount % 60 == 0)
        {
            console.Print($"Frame: {frameCount}");
        }

        if (keyboard.Escape.IsKeyUp)
        {
            App.LoadScene<MainScene>();
        }
    }
}
