using System.Drawing;
using Promete.Backends;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;
using Promete.Windowing;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/tilemap2.demo", "タイルマップを作成し動かします")]
public class Tilemap2ExampleScene(
    PrometeApp app,
    IGameView view,
    ITimeProvider time,
    Keyboard keyboard,
    Mouse mouse,
    ConsoleLayer console
) : Scene
{
    private readonly Random _random = new();
    private readonly Texture2D[] _textures = app.TextureFactory.LoadSpriteSheet(
        "assets/tiles.png",
        4,
        1,
        (16, 16)
    );
    private bool _hudVisible = true;
    private Tilemap? _map;
    private VectorInt _previousMousePosition;

    public override void OnStart()
    {
        view.Mode = WindowMode.Resizable;

        var tiles = _textures.Select(tex => new Tile(tex)).ToArray();
        _map = new Tilemap((16, 16));
        var g = new Container();
        g.Add(Shape.CreateLine((-128, 0), (127, 0), Color.Red));
        g.Add(Shape.CreateLine((0, -128), (0, 127), Color.Blue));
        Root.Add(_map);
        Root.Add(g);

        for (var i = 0; i < 32768; i++)
            _map.SetTile(
                // Determine the random position
                _random.NextVectorInt(view.Width * 8 / 16, view.Height * 8 / 16)
                    - (view.Size / 4 / 16),
                tiles[Random.Shared.Next(tiles.Length)]
            );

        _map.RenderingMode = TilemapRenderingMode.Scan;
    }

    public override void OnUpdate()
    {
        console.Clear();
        if (_hudVisible)
        {
            console.Print("[W] Key: Scroll Up");
            console.Print("[A] Key: Scroll Left");
            console.Print("[S] Key: Scroll Right");
            console.Print("[D] Key: Scroll Down");
            console.Print("[Z] Key: Zoom In");
            console.Print("[X] Key: Zoom Out");
            console.Print("[H] Key: Hide HUD");
            console.Print("[R] Key: Toggle Rendering Mode");
            console.Print("[ESC] Key: Return");
            console.Print("... You can also use dragging mouse to scroll the map");
            console.Print("");
            console.Print("Window Size: " + view.Size);
            console.Print("Tilemap Location: " + Root.Location);
            console.Print("Rendering Mode: " + _map.RenderingMode);
        }

        if (keyboard.Escape.IsKeyUp)
            app.LoadScene<MainScene>();

        if (mouse[MouseButtonType.Left])
            Root.Location += mouse.Position - _previousMousePosition;

        view.Title = time.FramePerSeconds + "FPS";

        var delta = 128 * Time.DeltaTime;
        if (keyboard.W)
            Root.Location += Vector.Up * delta;
        if (keyboard.A)
            Root.Location += Vector.Left * delta;
        if (keyboard.S)
            Root.Location += Vector.Down * delta;
        if (keyboard.D)
            Root.Location += Vector.Right * delta;
        if (keyboard.H.IsKeyDown)
            _hudVisible = !_hudVisible;
        if (keyboard.R.IsKeyDown)
        {
            _map.RenderingMode = _map.RenderingMode switch
            {
                TilemapRenderingMode.Auto => TilemapRenderingMode.RenderAll,
                TilemapRenderingMode.RenderAll => TilemapRenderingMode.Scan,
                TilemapRenderingMode.Scan => TilemapRenderingMode.Auto,
                _ => throw new InvalidOperationException(),
            };
        }

        if (keyboard.Z.IsKeyDown)
            Root.Scale *= 2.0f;
        if (keyboard.X.IsKeyDown)
            Root.Scale *= 0.5f;
        _map.Angle += (
            mouse.Scroll.Y > 0 ? 1
            : mouse.Scroll.Y < 0 ? -1
            : 0
        ).Degrees;

        _previousMousePosition = mouse.Position;
    }

    public override void OnDestroy()
    {
        foreach (var t in _textures)
        {
            t.Dispose();
        }
    }
}
