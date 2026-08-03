using FluentAssertions;
using Promete.Graphics.Rendering;
using Promete.Graphics.Rendering.Commands;
using Promete.Nodes;
using Xunit.Abstractions;

namespace Promete.Test;

/// <summary>
/// デモシーン TrimClampDebugScene と同じノード構成でトリム矩形を検証する。
/// 画面で見えている挙動が意図通りかを確認するための診断テスト。
/// </summary>
public class TrimSceneShapeTests(ITestOutputHelper output)
{
    private const int TrimTop = 140;
    private const int TrimHeight = 150;
    private const int OuterWidth = 400;
    private const int InnerWidth = 300;
    private const int InnerScreenX = 50;

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(-200)]
    public void デモシーン構成のトリム矩形を出力する(int outerX)
    {
        var queue = new RenderCommandQueue();
        var captured = new List<BeginTrimCommand>();
        queue.RegisterRunner(new CapturingRunner(captured));

        var outer = new Container().Size(OuterWidth, TrimHeight);
        outer.Location = (outerX, TrimTop);

        var inner = new Container().Size(InnerWidth, TrimHeight);
        inner.Location = (InnerScreenX - outerX, 0);
        outer.Add(inner);

        outer.BeforeRender();
        inner.BeforeRender();

        var ctx = new RenderContext { WindowSize = (640, 480) };
        queue.PushTrim(outer, ctx);
        queue.PushTrim(inner, ctx);
        queue.ProcessAndFlush();

        var o = captured[0];
        var i = captured[1];

        output.WriteLine($"outerX = {outerX}");
        output.WriteLine($"  外側トリム: x={o.X}..{o.X + o.Width}  (青紫の帯はここで切れる)");
        output.WriteLine($"  内側トリム: x={i.X}..{i.X + i.Width}  (緑の帯はここで切れる)");
        output.WriteLine($"  青紫の帯の実体: 画面 x={outerX}..{outerX + OuterWidth}");
        output.WriteLine($"  緑の帯の実体  : 画面 x={InnerScreenX}..{InnerScreenX + InnerWidth}");

        // 外側の帯は外側トリムを超えて見えてはならない
        (o.X + o.Width).Should().BeLessThanOrEqualTo(outerX + OuterWidth);
    }

    private sealed class CapturingRunner(List<BeginTrimCommand> sink)
        : CommandRunner<BeginTrimCommand>
    {
        public override void Execute(BeginTrimCommand command) => sink.Add(command);
    }
}
