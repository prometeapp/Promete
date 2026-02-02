using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("graphics/pieSprite.demo", "PieSpriteの例（円形プログレスバー/ワイプ）")]
public class PieSpriteDemo(ConsoleLayer console, Keyboard keyboard) : Scene
{
    private PieSprite _progressBar = null!;
    private PieSprite _progressBar2 = null!;
    private Text _label1 = null!;
    private Text _label2 = null!;

    public override void OnStart()
    {
        console.Print("Press [ESC] to return");

        var font = Font.GetDefault(18);
        var texture = Window.TextureFactory.Load("assets/rect.png");

        _progressBar = new PieSprite(texture)
            .Location(Window.Width / 4f - 64, Window.Height / 2f - 64)
            .Size(128, 128);

        _label1 = new Text("通常の例", font, Color.Lime)
            .Location(_progressBar.Location.X, _progressBar.Location.Y - 30);

        _progressBar2 = new PieSprite(texture, Color.Cyan)
            .Location(Window.Width * 3f / 4 - 64, Window.Height / 2f - 64)
            .Size(128, 128);

        _label2 = new Text("StartPercentを併用する例", font, Color.Lime)
            .Location(_progressBar2.Location.X, _progressBar2.Location.Y - 30);

        Root.AddRange(_progressBar, _progressBar2, _label1, _label2);
    }

    public override void OnUpdate()
    {
        // 自動アニメーション（0% → 100% → 0%をループ）
        var progress = (Window.TotalTime * 20.0f) % 100;

        _progressBar.Percent = progress;
        _progressBar2.Percent = MathHelper.EaseOut(progress / 100f, 0, 100);
        _progressBar2.StartPercent = MathHelper.EaseIn(progress / 100f, 0, 100);

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }
}
