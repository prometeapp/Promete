using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples;

[Demo("/sample3.demo", "ドラッグアンドドロップの例")]
public class Sample3ExampleScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    public override void OnStart()
    {
        Window.FileDropped += OnFileDrop;
        console.Print("Drop some files");
        console.Print("Press [ESC] to return");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        Window.FileDropped -= OnFileDrop!;
    }

    private void OnFileDrop(FileDroppedEventArgs e)
    {
        foreach (var path in e.Pathes) console.Print($"Dropped file is {path}");
    }
}
