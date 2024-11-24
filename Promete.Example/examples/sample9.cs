using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("sample9.demo", "Zオーダーのテスト")]
public class SpriteZTestScene : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly Sprite _mainIchigo;
    private readonly Mouse _mouse;
    private readonly Texture2D tIchigo;
    private float angle = 0;
    private bool isPlaying = true;

    public SpriteZTestScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse)
    {
        _console = console;
        _keyboard = keyboard;
        _mouse = mouse;

        tIchigo = Window.TextureFactory.Load("assets/ichigo.png");

        for (var i = 1; i < 255; i++)
        {
            var pos = Random.Shared.NextVectorInt(Window.Width, Window.Height);
            var ichigo = new Sprite(tIchigo)
                .Location(pos);
            ichigo.ZIndex = pos.Y;

            Root.Add(ichigo);
        }

        _mainIchigo = new Sprite(tIchigo)
            .Scale(2, 2);
        Root.Add(_mainIchigo);
    }

    public override void OnUpdate()
    {
        _mainIchigo.Location = _mouse.Position;
        _mainIchigo.ZIndex = _mouse.Position.Y;

        _console.Clear();
        _console.Print("FPS: " + Window.FramePerSeconds);

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyDown) isPlaying ^= true;
    }
}
