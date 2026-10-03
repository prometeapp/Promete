using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("sample8.demo", "スプライトの回転テスト3")]
public class SpriteRotateTest3Scene : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly List<Container> _allIchigos = [];
    private readonly Texture2D _tIchigo;
    private float _angle;
    private bool _isPlaying = true;

    public SpriteRotateTest3Scene(ConsoleLayer console, Keyboard keyboard)
    {
        _console = console;
        _keyboard = keyboard;

        _tIchigo = App.TextureFactory.Load("assets/ichigo.png");

        var parent = CreateIchigo(View.Size / 2);
        var root = parent;
        _allIchigos.Add(parent);

        for (var i = 1; i < 16; i++)
        {
            var child = CreateIchigo((32 + (i * 4), 0));

            parent.Add(child);
            _allIchigos.Add(child);
            parent = child;
        }

        Root = [root];
    }

    public override void OnUpdate()
    {
        _console.Clear();
        _console.Print("Angle: " + _angle);
        _console.Print("[SPACE]: Toggle Rotation");
        _console.Print("[ESC]: return");

        if (_isPlaying)
        {
            _angle += Time.DeltaTime * 30;
            if (_angle > 360)
                _angle -= 360;
        }

        _allIchigos.ForEach(i => i.Angle = _angle.Degrees);

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyDown)
            _isPlaying ^= true;

        if (_keyboard.Left.IsKeyDown)
        {
            _angle = (int)(_angle - 1);
            if (_angle < 0)
                _angle = 360;
        }

        if (_keyboard.Right.IsKeyDown)
        {
            _angle = (int)(_angle + 1);
            if (_angle > 360)
                _angle = 0;
        }
    }

    private Container CreateIchigo(Vector location)
    {
        return new Container().Location(location).Children(new Sprite(_tIchigo));
    }
}
