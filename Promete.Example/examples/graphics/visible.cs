using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("graphics/visible.demo", "スプライトの表示・非表示")]
public class visible : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly Texture2D _tIchigo;

    private readonly Sprite _sprite;

    public visible(ConsoleLayer console, Keyboard keyboard)
    {
        _console = console;
        _keyboard = keyboard;
        _tIchigo = Window.TextureFactory.Load("assets/ichigo.png");

        _sprite = new Sprite(_tIchigo)
            .Scale(8, 8)
            .Location(320, 240)
            .Pivot(HorizontalAlignment.Center, VerticalAlignment.Center);

        Root.Add(_sprite);
    }

    public override void OnStart()
    {
        _console.Print("[SPACE] to toggle visibility");
        _console.Print("[ESC] to return");
    }

    public override void OnUpdate()
    {
        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyUp)
        {
            _sprite.IsVisible = !_sprite.IsVisible;
        }
    }

    public override void OnDestroy()
    {
        _tIchigo.Dispose();
    }
}
