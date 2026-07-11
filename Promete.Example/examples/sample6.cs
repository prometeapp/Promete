using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("sample6.demo", "スプライトの回転テスト")]
public class SpriteRotateTestScene : Scene
{
    private readonly Keyboard _keyboard;
    private readonly Mouse _mouse;
    private readonly Sprite _sprite;
    private readonly Texture2D _tSolid;
    private readonly Container _wrapper;
    private float _angle;

    public SpriteRotateTestScene(Keyboard keyboard, Mouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;

        _tSolid = App.TextureFactory.CreateSolid(Color.Chartreuse, (32, 32));

        Root =
        [
            _wrapper = new Container()
                .Scale((2, 2))
                .Children(
                    Shape.CreateLine(-32, 0, 32, 0, Color.Red),
                    Shape.CreateLine(0, -32, 0, 32, Color.Blue),
                    _sprite = new Sprite(_tSolid)
                ),
        ];
    }

    public override void OnUpdate()
    {
        _angle += Time.DeltaTime * 90;
        if (_angle > 360)
            _angle -= 360;
        _sprite.Angle = _angle.Degrees;

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyDown)
            _wrapper.Scale = _wrapper.Scale.X == 1 ? (2, 2) : (1, 1);

        _wrapper.Location = _mouse.Position;
    }
}
