using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("sample7.demo", "スプライトの回転テスト2")]
public class SpriteRotateTest2Scene : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly Sprite _spriteChild;
    private readonly Texture2D _tChild;
    private readonly Texture2D _tParent;
    private readonly Container _wrapper;
    private float _angle;
    private bool _isPlaying = true;
    private int _mode;
    private Sprite _spriteParent;

    public SpriteRotateTest2Scene(ConsoleLayer console, Keyboard keyboard)
    {
        _tParent = App.TextureFactory.CreateSolid(Color.DarkBlue, (160, 120));
        _tChild = App.TextureFactory.CreateSolid(Color.Chocolate, (32, 32));

        _console = console;
        _keyboard = keyboard;

        Root =
        [
            _wrapper = new Container()
                .Scale((2, 2))
                .Location(320, 240)
                .Children(
                    _spriteParent = new Sprite(_tParent).Location(0, 0),
                    _spriteChild = new Sprite(_tChild).Location(32, 32)
                ),
        ];
    }

    private string ModeText =>
        _mode switch
        {
            0 => "Rotate Parent",
            1 => "Rotate Child",
            2 => "Rotate Both",
            _ => "Unknown",
        };

    public override void OnUpdate()
    {
        _console.Clear();
        _console.Print("Angle: " + _angle);
        _console.Print("Mode: " + ModeText);
        _console.Print("[1]: Change Mode");
        _console.Print("[SPACE]: Toggle Rotation");
        _console.Print("[ESC]: return");

        if (_isPlaying)
        {
            _angle += Time.DeltaTime * 90;
            if (_angle > 360)
                _angle -= 360;
        }

        switch (_mode)
        {
            case 0:
                _wrapper.Angle = _angle.Degrees;
                break;
            case 1:
                _spriteChild.Angle = _angle.Degrees;
                break;
            case 2:
                _wrapper.Angle = _angle.Degrees;
                _spriteChild.Angle = _angle.Degrees;
                break;
        }

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Number1.IsKeyDown)
        {
            _wrapper.Angle = _spriteChild.Angle = 0.Degrees;
            _mode = (_mode + 1) % 3;
        }

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
}
