using FluentAssertions;
using Promete.Headless;
using Promete.Nodes;

namespace Promete.Test;

public class ScenelessRunTests
{
    [Fact]
    public void Run_WithoutScene_ShouldStartUpdateAndExit()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        Container? rootAtStart = null;
        var updated = false;

        app.Start += () => rootAtStart = app.Root;
        app.Update += () =>
        {
            updated = true;
            app.Exit();
        };

        var status = app.Run();

        status.Should().Be(0, "Exit() の既定ステータスコードで終了するはず");
        rootAtStart.Should().NotBeNull("シーンを指定しなくても Root が初期化されるはず");
        updated.Should().BeTrue("シーンなしでも更新ループが回るはず");
    }
}
