using FluentAssertions;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;

namespace Promete.Test;

/// <summary>
/// <see cref="RenderCommandQueue.PushTrim"/> のトリム矩形計算テスト。
///
/// レビュー指摘 #03 の回帰防止。原点を 0 にクランプする際にサイズを縮めないと、
/// 可視領域がはみ出した量だけ広がり、入れ子トリムの積集合にも漏れる。
/// </summary>
public class TrimCommandQueueTests
{
    private static readonly VectorInt WindowSize = (640, 480);

    [Fact]
    public void 画面内に収まるトリムはそのまま出力される()
    {
        var command = PushTrim(location: (100, 100), size: (200, 150));

        command.X.Should().Be(100);
        command.Y.Should().Be(100);
        command.Width.Should().Be(200);
        command.Height.Should().Be(150);
    }

    [Fact]
    public void 左上にはみ出した分だけ幅と高さが縮む()
    {
        var command = PushTrim(location: (-50, -40), size: (200, 150));

        command.X.Should().Be(0);
        command.Y.Should().Be(0);
        command.Width.Should().Be(150, "左に 50 はみ出した分は可視幅から引かれる");
        command.Height.Should().Be(110, "上に 40 はみ出した分は可視高から引かれる");
    }

    [Fact]
    public void 大きくはみ出しても可視領域は正しく狭まる()
    {
        var command = PushTrim(location: (-400, 100), size: (500, 150));

        command.X.Should().Be(0);
        command.Width.Should().Be(100);
    }

    [Fact]
    public void 完全に画面外へ出た場合は幅がゼロになる()
    {
        var command = PushTrim(location: (-600, 100), size: (500, 150));

        command.X.Should().Be(0);
        command.Width.Should().Be(0, "負の幅を出力してはならない");
    }

    [Fact]
    public void 右下がウィンドウを超える場合はウィンドウ端で切られる()
    {
        var command = PushTrim(location: (-100, 100), size: (800, 150));

        command.X.Should().Be(0);
        command.Width.Should().Be(640);
    }

    /// <summary>
    /// 指摘#03 の本質。外側の過大な幅が内側の積集合へ漏れないことを確認する。
    /// </summary>
    [Fact]
    public void 入れ子トリムで外側のはみ出しが内側の右端に反映される()
    {
        var queue = new RenderCommandQueue();
        var captured = new List<BeginTrimCommand>();
        queue.RegisterRunner(new CapturingRunner(captured));

        var ctx = new RenderContext { WindowSize = WindowSize };

        // 外側: 左へ 200 はみ出す。可視領域は x 0..200 であるべき
        var outer = new Container().Size(400, 150);
        outer.Location = (-200, 140);

        // 内側: 画面 x=50 に置く。中身は x 50..350 に広がる
        var inner = new Container().Size(300, 150);
        inner.Location = (250, 0);
        outer.Add(inner);

        outer.BeforeRender();
        inner.BeforeRender();

        queue.PushTrim(outer, ctx);
        queue.PushTrim(inner, ctx);
        queue.ProcessAndFlush();

        captured.Should().HaveCount(2);

        var outerCommand = captured[0];
        outerCommand.X.Should().Be(0);
        outerCommand.Width.Should().Be(200, "外側の可視幅は 400 - 200 = 200");

        var innerCommand = captured[1];
        var innerRight = innerCommand.X + innerCommand.Width;
        innerRight
            .Should()
            .Be(200, "内側は外側の可視右端 200 で切られる (過大な 350 になってはならない)");
    }

    /// <summary>
    /// 指定位置・サイズのコンテナをトリムし、生成された BeginTrimCommand を返す。
    /// </summary>
    private static BeginTrimCommand PushTrim(VectorInt location, VectorInt size)
    {
        var queue = new RenderCommandQueue();
        var captured = new List<BeginTrimCommand>();
        queue.RegisterRunner(new CapturingRunner(captured));

        var container = new Container().Size(size);
        container.Location = location;
        container.BeforeRender();

        queue.PushTrim(container, new RenderContext { WindowSize = WindowSize });
        queue.ProcessAndFlush();

        captured.Should().ContainSingle();
        return captured[0];
    }

    private sealed class CapturingRunner(List<BeginTrimCommand> sink)
        : CommandRunner<BeginTrimCommand>
    {
        public override void Execute(BeginTrimCommand command) => sink.Add(command);
    }
}
