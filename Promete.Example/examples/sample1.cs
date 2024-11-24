using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples;

[Demo("/sample1.demo", @"""Hello world!""")]
public class Sample1ExampleScene(ConsoleLayer console, Keyboard keyboard) : Scene
{
    public override void OnStart()
    {
        console.Print("Hello, world!");
        console.Print("Press [ESC] to exit");
    }

    public override void OnUpdate()
    {
        if (keyboard.Escape.IsKeyDown) App.LoadScene<MainScene>();
    }
}
