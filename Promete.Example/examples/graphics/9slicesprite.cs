using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("graphics/9slicesprite.demo", "9-slice Spriteの例")]
public class NineSliceSpriteTest(ConsoleLayer console, Keyboard keyboard) : Scene
{
#pragma warning disable
    private static Sprite _sprite;
    private static NineSliceSprite _nineSlice;
    private static Text _t1;
    private static Text _t2;
#pragma warning restore

    public override void OnStart()
    {
        console.Print("Press ESC to return");

        var font = Font.GetDefault(18);

        var normalSprite = Window.TextureFactory.Load("assets/rect.png");
        var nineSliceSprite = Window.TextureFactory.Load9Sliced("assets/rect.png", 16, 16, 16, 16);

        _sprite = new Sprite(normalSprite);
        _nineSlice = new NineSliceSprite(nineSliceSprite);
        _t1 = new Text("Sprite", font, Color.Lime);
        _t2 = new Text("9-slice Sprite", font, Color.Lime);

        UpdateLocation();

        Root.AddRange(_sprite, _nineSlice, _t1, _t2);
    }

    public override void OnUpdate()
    {
        UpdateLocation();

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    private void UpdateLocation()
    {
        _sprite.Location = (Window.Width / 4 - 128, 64);
        _nineSlice.Location = (Window.Width / 4 + 32, 64);

        _t1.Location = (_sprite.Location.X, _sprite.Location.Y - 24);
        _t2.Location = (_nineSlice.Location.X, _nineSlice.Location.Y - 24);

        _sprite.Width = _nineSlice.Width = (int)(64 + 64 * Math.Abs(Math.Sin(Window.TotalTime * 2)));
        _sprite.Height = _nineSlice.Height = (int)(64 + 256 * Math.Abs(Math.Sin(Window.TotalTime * 2)));
    }
}
