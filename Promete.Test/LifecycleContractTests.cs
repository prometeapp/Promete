using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// バックエンドとアプリケーションの間のライフサイクルの契約を確かめるテスト。
/// </summary>
public class LifecycleContractTests
{
    private static List<string> Log { get; } = [];

    [Fact]
    public void OnStart_ShouldCompleteBeforeFirstUpdate()
    {
        Log.Clear();
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<LifecycleContractTests>()
            .BuildWithHeadless();

        app.Start += () => Log.Add("App.Start");
        app.Update += () =>
        {
            Log.Add("App.Update");
            app.Exit();
        };

        app.Run<RecordingScene>();

        Log.Should()
            .StartWith(
                ["Scene.Start", "App.Start", "Scene.Update", "App.Update"],
                "最初の更新より前に、シーンの OnStart と Start イベントが完了しているはず"
            );
    }

    internal sealed class RecordingScene : Scene
    {
        public override void OnStart() => Log.Add("Scene.Start");

        public override void OnUpdate() => Log.Add("Scene.Update");
    }
}
