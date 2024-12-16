using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/pivot.demo", "Pivotのテスト")]
public class pivot : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly Texture2D _tIchigo;

    private readonly Sprite _spriteTopLeft, _spriteCenter, _spriteBottomRight;

    public pivot(ConsoleLayer console, Keyboard keyboard)
    {
        _console = console;
        _keyboard = keyboard;
        _tIchigo = Window.TextureFactory.Load("assets/ichigo.png");

        var loc1 = new VectorInt(96, 128);
        var loc2 = new VectorInt(192, 128);
        var loc3 = new VectorInt(288, 128);

        Root =
        [
            // _spriteTopLeft の中心を示す十字線
            Shape.CreateLine(loc1 + VectorInt.Left * 32, loc1 + VectorInt.Right * 32, Color.Gray),
            Shape.CreateLine(loc1 + VectorInt.Up * 32, loc1 + VectorInt.Down * 32, Color.Gray),

            // _spriteCenter の中心を示す十字線
            Shape.CreateLine(loc2 + VectorInt.Left * 32, loc2 + VectorInt.Right * 32, Color.Gray),
            Shape.CreateLine(loc2 + VectorInt.Up * 32, loc2 + VectorInt.Down * 32, Color.Gray),

            // _spriteBottomRight の中心を示す十字線
            Shape.CreateLine(loc3 + VectorInt.Left * 32, loc3 + VectorInt.Right * 32, Color.Gray),
            Shape.CreateLine(loc3 + VectorInt.Up * 32, loc3 + VectorInt.Down * 32, Color.Gray),

            _spriteTopLeft = new Sprite(_tIchigo)
                .Scale(2, 2)
                .Location(loc1),
            _spriteCenter = new Sprite(_tIchigo)
                .Scale(2, 2)
                .Location(loc2)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Center),
            _spriteBottomRight = new Sprite(_tIchigo)
                .Scale(2, 2)
                .Location(loc3)
                .Pivot(HorizontalAlignment.Right, VerticalAlignment.Bottom),

            new Text("(0, 0)")
                .Location(loc1 + VectorInt.Up * 40)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Bottom),

            new Text("(0.5, 0.5)")
                .Location(loc2 + VectorInt.Up * 40)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Bottom),

            new Text("(1, 1)")
                .Location(loc3 + VectorInt.Up * 40)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Bottom),
        ];
    }

    public override void OnStart()
    {
        _console.Print("Pivot Test");
        _console.Print("[ESC] to return");
    }

    public override void OnUpdate()
    {
        _spriteTopLeft.Angle = _spriteCenter.Angle =
            _spriteBottomRight.Angle = (_spriteBottomRight.Angle + 180 * Window.DeltaTime) % 360;

        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _tIchigo.Dispose();
    }
}
