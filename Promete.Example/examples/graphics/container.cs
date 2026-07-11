using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/container.demo", "ノードをコンテナーにいくつか追加する例")]
public class ContainerExampleScene(ConsoleLayer console, Keyboard keyboard, Mouse mouse) : Scene
{
    private readonly Container _container = new();
    private Texture2D _ichigo;

    public override void OnStart()
    {
        _ichigo = App.TextureFactory.Load("assets/ichigo.png");
        Root.Add(_container);

        var canvas = new Container().Location(400, 200);

        var random = new Random(300);

        VectorInt Rnd()
        {
            return random.NextVectorInt(256, 256);
        }

        for (var i = 0; i < 120; i++)
        {
            var (v1, v2, v3) = (Rnd(), Rnd(), Rnd());
            switch (random.Next(4))
            {
                case 0:
                    canvas.Add(Shape.CreateLine(v1, v2, random.NextColor()));
                    break;
                case 1:
                    canvas.Add(
                        Shape.CreateRect(
                            v1,
                            v2,
                            random.NextColor(),
                            random.Next(4),
                            random.NextColor()
                        )
                    );
                    break;
                case 2:
                    canvas.Add(Shape.CreatePixel(v1, random.NextColor()));
                    break;
                case 3:
                    canvas.Add(
                        Shape.CreateTriangle(
                            v1,
                            v2,
                            v3,
                            random.NextColor(),
                            random.Next(4),
                            random.NextColor()
                        )
                    );
                    break;
            }
        }

        _container.Add(new Text("O", Graphics.Fonts.Font.GetDefault(32), Color.White));

        _container.Add(canvas);

        for (var i = 0; i < 8; i++)
            _container.Add(
                new Sprite(_ichigo)
                {
                    Location = random.NextVector(View.Width, View.Height),
                    Scale = Vector.One + (random.NextVectorFloat() * 7),
                    TintColor = random.NextColor(),
                }
            );

        console.Print("Scroll to move");
        console.Print("Press ↑ to scale up");
        console.Print("Press ↓ to scale down");
        console.Print("Press ESC to return");
    }

    public override void OnUpdate()
    {
        if (keyboard.Up)
            _container.Scale += Vector.One * 0.25f * Time.DeltaTime;
        if (keyboard.Down)
            _container.Scale -= Vector.One * 0.25f * Time.DeltaTime;
        _container.Location += mouse.Scroll * (-1, 1);
        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _ichigo.Dispose();
    }
}
