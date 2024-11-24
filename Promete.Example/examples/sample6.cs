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
    private readonly Sprite sprite;
    private readonly Texture2D tSolid;
    private readonly Container wrapper;
    private float angle;

    public SpriteRotateTestScene(Keyboard keyboard, Mouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;

        tSolid = Window.TextureFactory.CreateSolid(Color.Chartreuse, (32, 32));

        Root =
        [
            wrapper = new Container()
                .Scale((2, 2))
                .Children(
                    Shape.CreateLine(-32, 0, 32, 0, Color.Red),
                    Shape.CreateLine(0, -32, 0, 32, Color.Blue),
                    sprite = new Sprite(tSolid)
                )
        ];
    }

    public override void OnUpdate()
    {
        angle += Window.DeltaTime * 90;
        if (angle > 360) angle -= 360;
        sprite.Angle = angle;

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();

        if (_keyboard.Space.IsKeyDown) wrapper.Scale = wrapper.Scale.X == 1 ? (2, 2) : (1, 1);

        wrapper.Location = _mouse.Position;
    }
}
