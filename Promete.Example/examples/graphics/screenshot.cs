using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("graphics/screenshot.demo", "スクリーンショットの撮影")]
public class ScreenshotTest(Mouse mouse, Keyboard keyboard) : Scene
{
    private readonly Sprite _sprite = new();
    private Texture2D? _texture;

    public override void OnStart()
    {
        View.Size = (320, 240);
        View.Scale = 2;

        for (var i = 0; i < 100; i++)
        {
            var loc = Random.Shared.NextVectorInt(View.X, View.Y);
            var size = Random.Shared.NextVectorInt(64, 64) + (8, 8);
            Root.Add(Shape.CreateRect(loc, loc + size, Random.Shared.NextColor()));
        }

        Root.Add(_sprite);
    }

    public override void OnUpdate()
    {
        if (keyboard.Space.IsKeyUp)
        {
            _texture?.Dispose();
            _texture = View.TakeScreenshot();
            _sprite.Texture = _texture;
            _sprite.Location = (0, 0);
        }

        _sprite.Location = mouse.Position;
        _sprite.Scale = (0.5f, 0.5f);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        View.Size = (640, 480);
        View.Scale = 1;
    }
}
