using System.Drawing;
using Promete.Example.Kernel;
using Promete.Graphics;
using Promete.Input;
using Promete.Nodes;
using Promete.Windowing;

namespace Promete.Example.examples.graphics;

[Demo("/graphics/tilemap2.demo", "タイルマップを作成し動かします")]
public class Tilemap2ExampleScene(
    PrometeApp app,
    IWindow window,
    Keyboard keyboard,
    Mouse mouse,
    ConsoleLayer console
) : Scene
{
    private readonly Random random = new();
    private readonly Texture2D[] _textures = window.TextureFactory.LoadSpriteSheet(
        "assets/tiles.png",
        4,
        1,
        (16, 16)
    );
    private bool hudVisible = true;
    private Tilemap map;
    private VectorInt previousMousePosition;

    public override void OnStart()
    {
        window.Mode = WindowMode.Resizable;

        var tiles = _textures.Select(tex => new Tile(tex)).ToArray();
        map = new Tilemap((16, 16));
        var g = new Container();
        g.Add(Shape.CreateLine((-128, 0), (127, 0), Color.Red));
        g.Add(Shape.CreateLine((0, -128), (0, 127), Color.Blue));
        Root.Add(map);
        Root.Add(g);

        for (var i = 0; i < 32768; i++)
            map.SetTile(
                // Determine the random position
                random.NextVectorInt(window.Width * 8 / 16, window.Height * 8 / 16)
                    - window.Size / 4 / 16,
                tiles[Random.Shared.Next(tiles.Length)]
            );

        map.RenderingMode = TilemapRenderingMode.Scan;
    }

    public override void OnUpdate()
    {
        console.Clear();
        if (hudVisible)
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
            console.Print("Window Size: " + window.Size);
            console.Print("Tilemap Location: " + Root.Location);
            console.Print("Rendering Mode: " + map.RenderingMode);
        }

        if (keyboard.Escape.IsKeyUp)
            app.LoadScene<MainScene>();

        if (mouse[MouseButtonType.Left])
            Root.Location += mouse.Position - previousMousePosition;

        window.Title = window.FramePerSeconds + "FPS";

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
            hudVisible = !hudVisible;
        if (keyboard.R.IsKeyDown)
        {
            map.RenderingMode = map.RenderingMode switch
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
        map.Angle += (
            mouse.Scroll.Y > 0 ? 1
            : mouse.Scroll.Y < 0 ? -1
            : 0
        ).Degrees;

        previousMousePosition = mouse.Position;
    }

    public override void OnDestroy()
    {
        foreach (var t in _textures)
        {
            t.Dispose();
        }
    }
}
