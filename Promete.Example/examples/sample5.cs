using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;

namespace Promete.Example.examples;

[Demo("/sample5.demo", "10000スプライトを表示してFPSを計測します")]
public class BenchmarkScene(Keyboard keyboard) : Scene
{
    private readonly Random _rnd = new();
    private bool _initialized;
    private Texture2D _strawberry;

    public override async void OnStart()
    {
        _strawberry = App.TextureFactory.Load("assets/ichigo.png");
        App.NextFrame(Init);
        View.Title = "Initializing in background thread...";
    }

    public override void OnUpdate()
    {
        if (!_initialized)
            return;
        View.Title = $"{Time.FramePerSeconds} FPS";

        if (keyboard.Escape.IsKeyUp)
            App.LoadScene<MainScene>();
    }

    public override void OnDestroy()
    {
        _strawberry.Dispose();
    }

    private async void Init()
    {
        await Task.Factory.StartNew(() =>
        {
            for (var i = 0; i < 10000; i++)
            {
                var sprite = new Sprite(_strawberry).Location(
                    _rnd.NextVector(View.Width, View.Height)
                );
                App.NextFrame(() => Root.Add(sprite));
            }

            _initialized = true;
        });
    }
}
