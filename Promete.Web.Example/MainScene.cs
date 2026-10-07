using System.Drawing;
using Promete;
using Promete.Nodes;

namespace Promete.Web.Example;

/// <summary>
/// ブラウザ上の描画確認用シーン。図形 (DrawPrimitive) とスプライト (DrawTextureBatched) を動かす。
/// </summary>
public class MainScene : Scene
{
    private Sprite[] _sprites = [];
    private Shape _marker = null!;

    public override void OnStart()
    {
        App.BackgroundColor = Color.FromArgb(24, 28, 48);

        Root.Add(Shape.CreateRect(40, 40, 200, 160, Color.IndianRed));
        Root.Add(Shape.CreateRect(60, 60, 220, 180, Color.FromArgb(160, Color.SteelBlue)));
        Root.Add(Shape.CreateTriangle((320, 200), (420, 60), (520, 200), Color.Goldenrod));

        _marker = Shape.CreateRect(0, 0, 11, 11, Color.White);
        Root.Add(_marker);

        var texture = App.TextureFactory.Create(CreateChecker(16), (16, 16));
        _sprites = Enumerable
            .Range(0, 8)
            .Select(i => new Sprite(texture) { Scale = (3, 3), Location = (40 + (i * 64), 260) })
            .ToArray();
        foreach (var sprite in _sprites)
            Root.Add(sprite);
    }

    public override void OnUpdate()
    {
        var t = Time.TotalTime;
        _marker.Location = (300 + (MathF.Cos(t * 2) * 120), 110 + (MathF.Sin(t * 2) * 40));
        for (var i = 0; i < _sprites.Length; i++)
            _sprites[i].Location = (40 + (i * 64), 300 + (MathF.Sin((t * 3) + i) * 30));
    }

    private static byte[] CreateChecker(int size)
    {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var on = ((x / 4) + (y / 4)) % 2 == 0;
            var i = ((y * size) + x) * 4;
            pixels[i] = (byte)(on ? 255 : 40);
            pixels[i + 1] = (byte)(on ? 160 : 40);
            pixels[i + 2] = (byte)(on ? 60 : 120);
            pixels[i + 3] = 255;
        }

        return pixels;
    }
}
