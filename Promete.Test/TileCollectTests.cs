using System.Drawing;
using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

namespace Promete.Test;

/// <summary>
/// <see cref="ITile.Collect"/> の既定実装と、<see cref="Tilemap"/> からの呼び出しのテスト。
/// </summary>
public class TileCollectTests
{
    private static readonly VectorInt TileSize = (16, 16);

    [Fact]
    public void 既定のCollectはテクスチャ描画コマンドを積む()
    {
        var texture = new Texture2D(1, TileSize, _ => { });
        var map = new Tilemap(TileSize, renderingMode: TilemapRenderingMode.RenderAll);
        map.SetTile(2, 3, new FixedTextureTile(texture), Color.Red);

        var commands = CollectAll(map);

        var batch = commands.Should().ContainSingle().Subject.Should().BeOfType<DrawTextureBatchedCommand>().Subject;
        batch.Texture.Handle.Should().Be(1);
        batch.Items.Should().ContainSingle();
    }

    [Fact]
    public void Collectだけ実装したタイルはGetTextureなしで描画できる()
    {
        var tile = new RecordingTile();
        var map = new Tilemap(TileSize, renderingMode: TilemapRenderingMode.RenderAll);
        map.SetTile(2, 3, tile, Color.Red);

        var commands = CollectAll(map);

        commands.Should().ContainSingle().Which.Should().BeOfType<DrawPrimitiveCommand>();
        tile.Location.Should().Be((VectorInt)(2, 3));
        tile.Tint.Should().Be(Color.Red);
    }

    [Fact]
    public void GetTextureを実装しないタイルのGetTextureは例外になる()
    {
        ITile tile = new RecordingTile();
        var map = new Tilemap(TileSize);

        var act = () => tile.GetTexture(map, (0, 0));

        act.Should().Throw<NotSupportedException>();
    }

    private static List<IRenderCommand> CollectAll(Tilemap map)
    {
        var queue = new RenderCommandQueue();
        var captured = new List<IRenderCommand>();
        queue.RegisterRunner(new CapturingRunner<DrawPrimitiveCommand>(captured));
        queue.RegisterRunner(new CapturingRunner<DrawTextureBatchedCommand>(captured));

        map.BeforeRender();
        map.Collect(queue, new RenderContext { WindowSize = (640, 480) });
        queue.ProcessAndFlush();
        return captured;
    }

    private sealed class FixedTextureTile(Texture2D texture) : ITile
    {
        public Texture2D GetTexture(Tilemap map, VectorInt tileLocation) => texture;

        public void Destroy() { }
    }

    private sealed class RecordingTile : ITile
    {
        public VectorInt Location { get; private set; }

        public Color Tint { get; private set; }

        public void Collect(RenderCommandQueue queue, Tilemap map, VectorInt tileLocation, Color tint)
        {
            Location = tileLocation;
            Tint = tint;
            queue.Enqueue(
                new DrawPrimitiveCommand
                {
                    WorldVertices = [],
                    ShapeType = ShapeType.Rect,
                    Color = tint,
                }
            );
        }

        public void Destroy() { }
    }

    private sealed class CapturingRunner<T>(List<IRenderCommand> sink) : CommandRunner<T>
        where T : IRenderCommand
    {
        public override void Execute(T command) => sink.Add(command);
    }
}
