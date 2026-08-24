using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/pixelSnap.demo", "ピクセルスナップのテスト")]
public class PixelSnap : Scene
{
    private readonly ConsoleLayer _console;
    private readonly Keyboard _keyboard;
    private readonly Texture2D _tIchigo;

    public PixelSnap(ConsoleLayer console, Keyboard keyboard)
    {
        _console = console;
        _keyboard = keyboard;
        _tIchigo = App.TextureFactory.Load("assets/ichigo.png");

        // 中央ピボット + 端数を含む位置で、スナップ有無による描画差を比較する
        Root =
        [
            new Sprite(_tIchigo).Location(96.5f, 128.5f).Pivot(0.5f, 0.5f),
            new Sprite(_tIchigo).Location(192.5f, 128.5f).Pivot(0.5f, 0.5f).PixelSnap(false),
            new Text("snap ON")
                .Location(96, 160)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Top),
            new Text("snap OFF")
                .Location(192, 160)
                .Pivot(HorizontalAlignment.Center, VerticalAlignment.Top),
        ];
    }

    public override void OnStart()
    {
        _console.Print("Pixel Snap Test");
        _console.Print("[ESC] to return");
    }

    public override void OnUpdate()
    {
        if (_keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _tIchigo.Dispose();
    }
}
