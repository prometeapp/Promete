using FluentAssertions;
using Promete.Graphics;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

namespace Promete.Test;

/// <summary>
/// <see cref="DeformedSprite"/> のサイズ・ピボットの算出と、描画コマンドの頂点のテスト。
/// </summary>
public class DeformedSpriteTests
{
    private static readonly Texture2D Texture = new(1, (32, 32), _ => { });

    [Fact]
    public void Issueの初期化例で生成できる()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (32, 4), (4, 48), (32, 64));

        ds.TopLeft.Should().Be(new Vector(0, 0));
        ds.TopRight.Should().Be(new Vector(32, 4));
        ds.BottomLeft.Should().Be(new Vector(4, 48));
        ds.BottomRight.Should().Be(new Vector(32, 64));
    }

    [Fact]
    public void 原点が矩形の左上と一致するとき_ピボットは0()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (32, 4), (4, 48), (32, 64))
        {
            Location = (100, 50),
        };

        ds.Size.Should().Be(new VectorInt(32, 64));
        ds.Pivot.Should().Be(new Vector(0, 0));
        ShouldBeNear(ds.AbsoluteLocation, (100, 50));
    }

    [Fact]
    public void 原点が矩形の内側にあるとき_ピボットは正()
    {
        var ds = new DeformedSprite(Texture, (-10, -20), (30, -20), (-10, 40), (30, 40))
        {
            Location = (100, 50),
        };

        ds.Size.Should().Be(new VectorInt(40, 60));
        ds.Pivot.X.Should().BeApproximately(0.25f, 1e-5f);
        ds.Pivot.Y.Should().BeApproximately(1 / 3f, 1e-5f);
        ShouldBeNear(ds.AbsoluteLocation, (100, 50));
    }

    [Fact]
    public void 原点が矩形の左上より外にあるとき_ピボットは負()
    {
        var ds = new DeformedSprite(Texture, (10, 20), (50, 20), (10, 80), (50, 80))
        {
            Location = (100, 50),
        };

        ds.Size.Should().Be(new VectorInt(40, 60));
        ds.Pivot.X.Should().BeApproximately(-0.25f, 1e-5f);
        ds.Pivot.Y.Should().BeApproximately(-1 / 3f, 1e-5f);
        ShouldBeNear(ds.AbsoluteLocation, (100, 50));
    }

    [Fact]
    public void 幅が0の軸のピボットは0()
    {
        var ds = new DeformedSprite(Texture, (5, 0), (5, 0), (5, 10), (5, 10));

        ds.Size.Should().Be(new VectorInt(0, 10));
        ds.Pivot.Should().Be(new Vector(0, 0));
    }

    [Fact]
    public void 端数のある大きさは切り上げる()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (10.5f, 0), (0, 3.2f), (10.5f, 3.2f));

        ds.Size.Should().Be(new VectorInt(11, 4));
    }

    [Fact]
    public void サイズとピボットへの代入は無視される()
    {
        var ds = new DeformedSprite(Texture, (-10, -20), (30, -20), (-10, 40), (30, 40));
        var pivot = ds.Pivot;

        ds.Size = (999, 999);
        ds.Pivot = (0.5f, 0.5f);

        ds.Size.Should().Be(new VectorInt(40, 60));
        ds.Pivot.Should().Be(pivot);
    }

    [Fact]
    public void 頂点を変更するとサイズとピボットが更新される()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (32, 0), (0, 32), (32, 32));

        ds.TopLeft = (-32, -32);

        ds.Size.Should().Be(new VectorInt(64, 64));
        ds.Pivot.Should().Be(new Vector(0.5f, 0.5f));

        ds.SetVertices((0, 0), (10, 0), (0, 20), (10, 20));

        ds.Size.Should().Be(new VectorInt(10, 20));
        ds.Pivot.Should().Be(new Vector(0, 0));
    }

    [Fact]
    public void 頂点の変更が親のAutoSizeに反映される()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (32, 4), (4, 48), (32, 64));
        var container = new Container { AutoSize = true };
        container.Add(ds);
        container.Size.Should().Be(new VectorInt(32, 64));

        ds.BottomRight = (40, 80);

        container.Size.Should().Be(new VectorInt(40, 80));
    }

    [Fact]
    public void ルート直下では頂点がLocationだけずれる()
    {
        var ds = new DeformedSprite(Texture, (-10, -20), (30, -20), (-10, 40), (30, 40))
        {
            Location = (100, 50),
        };

        var command = CollectSingle(ds);

        ShouldBeNear(command.TopLeft, (90, 30));
        ShouldBeNear(command.TopRight, (130, 30));
        ShouldBeNear(command.BottomLeft, (90, 90));
        ShouldBeNear(command.BottomRight, (130, 90));
    }

    [Fact]
    public void コンテナの変形が頂点に反映される()
    {
        var ds = new DeformedSprite(Texture, (-10, -20), (30, -20), (-10, 40), (30, 40))
        {
            Location = (10, 0),
            Angle = Angle.FromDegrees(90),
        };
        var container = new Container { Location = (100, 100), Scale = (2, 2) };
        container.Add(ds);

        var command = CollectSingle(container);

        // 子の 90° 回転 (x, y) → (-y, x)、子の位置 (10, 0)、親の拡大 2 倍と位置 (100, 100)
        ShouldBeNear(command.TopLeft, (160, 80));
        ShouldBeNear(command.TopRight, (160, 160));
        ShouldBeNear(command.BottomLeft, (40, 80));
        ShouldBeNear(command.BottomRight, (40, 160));
    }

    [Fact]
    public void 頂点を変更すると次の描画コマンドに反映される()
    {
        var ds = new DeformedSprite(Texture, (0, 0), (32, 0), (0, 32), (32, 32));
        CollectSingle(ds);

        ds.BottomRight = (64, 48);
        var command = CollectSingle(ds);

        ShouldBeNear(command.BottomRight, (64, 48));
    }

    [Fact]
    public void テクスチャがなければ描画しない()
    {
        var ds = new DeformedSprite(null, (0, 0), (32, 0), (0, 32), (32, 32));

        CollectAll(ds).Should().BeEmpty();
    }

    private static DrawDeformedTextureCommand CollectSingle(Node node)
    {
        var commands = CollectAll(node);
        return commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<DrawDeformedTextureCommand>()
            .Subject;
    }

    private static List<IRenderCommand> CollectAll(Node node)
    {
        var queue = new RenderCommandQueue();
        var captured = new List<IRenderCommand>();
        queue.RegisterRunner(new CapturingRunner<DrawDeformedTextureCommand>(captured));

        node.BeforeRender();
        node.Collect(queue, new RenderContext { WindowSize = (640, 480) });

        queue.ProcessAndFlush();
        return captured;
    }

    private static void ShouldBeNear(Vector actual, Vector expected)
    {
        actual.X.Should().BeApproximately(expected.X, 1e-3f);
        actual.Y.Should().BeApproximately(expected.Y, 1e-3f);
    }

    private sealed class CapturingRunner<T>(List<IRenderCommand> sink) : CommandRunner<T>
        where T : IRenderCommand
    {
        public override void Execute(T command) => sink.Add(command);
    }
}
