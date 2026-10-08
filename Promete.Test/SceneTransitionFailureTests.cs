using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// シーンの生成に失敗したあとも、シーン遷移を続けられることを確かめるテスト。
/// </summary>
public class SceneTransitionFailureTests
{
    private static List<string> Log { get; } = [];

    [Fact]
    public void LoadScene_WhenConstructorThrows_ShouldNotDestroyPreviousSceneTwice()
    {
        Log.Clear();
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneTransitionFailureTests>()
            .BuildWithHeadless();

        Exception? thrown = null;
        app.Start += () =>
        {
            app.LoadScene<SceneA>();
            var act = () => app.LoadScene<ThrowingScene>();
            act.Should().Throw<InvalidOperationException>();
            app.Root.Should().BeNull("破棄済みのシーンは現在のシーンとして残らないはず");

            try
            {
                app.LoadScene<SceneB>();
            }
            catch (Exception e)
            {
                thrown = e;
            }

            app.Exit();
        };

        app.Run();

        thrown.Should().BeNull("生成に失敗したあとも、次の遷移は成功するはず");
        Log.Should().Equal("A.Start", "A.Destroy", "B.Start");
    }

    [Fact]
    public void PushScene_WhenConstructorThrows_ShouldRestorePreviousScene()
    {
        Log.Clear();
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneTransitionFailureTests>()
            .BuildWithHeadless();

        var popped = true;
        app.Start += () =>
        {
            app.LoadScene<SceneA>();
            var root = app.Root;
            var act = () => app.PushScene<ThrowingScene>();
            act.Should().Throw<InvalidOperationException>();
            app.Root.Should().BeSameAs(root, "プッシュ前のシーンが現在のシーンに戻るはず");

            popped = app.PopScene();
            app.Exit();
        };

        app.Run();

        popped.Should().BeFalse("失敗したプッシュで、スタックにシーンが残らないはず");
        Log.Should().Equal("A.Start", "A.Pause", "A.Resume");
    }

    internal sealed class SceneA : Scene
    {
        public override void OnStart() => Log.Add("A.Start");

        public override void OnDestroy() => Log.Add("A.Destroy");

        public override void OnPause() => Log.Add("A.Pause");

        public override void OnResume() => Log.Add("A.Resume");
    }

    internal sealed class SceneB : Scene
    {
        public override void OnStart() => Log.Add("B.Start");

        public override void OnDestroy() => Log.Add("B.Destroy");
    }

    internal sealed class ThrowingScene : Scene
    {
        public ThrowingScene()
        {
            throw new InvalidOperationException("シーンの生成に失敗しました。");
        }
    }
}
