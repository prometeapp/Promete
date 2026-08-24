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
    private readonly Texture2D _tIchigo;
    private float _angle = 0;
    private bool _isPlaying = true;

    public SpriteZTestScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse)
    {
        _console = console;
        _keyboard = keyboard;
        _mouse = mouse;

        _tIchigo = App.TextureFactory.Load("assets/ichigo.png");

        for (var i = 1; i < 255; i++)
        {
            var pos = Random.Shared.NextVectorInt(View.Width, View.Height);
            var ichigo = new Sprite(_tIchigo).Location(pos);
            ichigo.ZIndex = pos.Y;

            Root.Add(ichigo);
        }

        _mainIchigo = new Sprite(_tIchigo).Scale(2, 2);
        Root.Add(_mainIchigo);
    }

    public override void OnUpdate()
    {
        _mainIchigo.Location = _mouse.Position;
        _mainIchigo.ZIndex = _mouse.Position.Y;

        _console.Clear();
        _console.Print("FPS: " + Time.FramePerSeconds);

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyDown)
            _isPlaying ^= true;
    }
}
