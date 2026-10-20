using FluentAssertions;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PrometeApp"/> の内部のフック (<see cref="ISceneLifecycleHook"/>) が、シーンの生成と破棄を正しい順序で知らせることのテスト。
/// </summary>
public sealed class SceneLifecycleHookTests
{
    private static List<string> Log { get; } = [];

    private static PrometeApp BuildApp()
    {
        Log.Clear();
        var app = PrometeApp.Create().UseScenesFrom<SceneLifecycleHookTests>().BuildWithHeadless();
        app.AddSceneLifecycleHook(new RecordingHook());
        app.SceneWillChange += _ => Log.Add("WillChange");
        return app;
    }

    [Fact]
    public void AddSceneLifecycleHook_Null_ShouldThrow()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        var act = () => app.AddSceneLifecycleHook(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CurrentScene_ShouldFollowTransitions()
    {
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneLifecycleHookTests>()
            .BuildWithHeadless();
        Log.Clear();
        Scene? beforeRun = null;
        Scene? afterLoad = null;
        Scene? afterPush = null;
        Scene? afterPop = null;

        beforeRun = app.CurrentScene;
        app.RunWith<HookSceneA>(() =>
        {
            afterLoad = app.CurrentScene;
            app.PushScene<HookSceneB>();
            afterPush = app.CurrentScene;
            app.PopScene();
            afterPop = app.CurrentScene;
        });

        beforeRun.Should().BeNull();
        afterLoad.Should().BeOfType<HookSceneA>();
        afterPush.Should().BeOfType<HookSceneB>();
        afterPop.Should().BeSameAs(afterLoad);
    }

    [Fact]
    public void InitialLoad_ShouldNotifyConstruction()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() => { });

        Log.Should().Equal("Constructing", "A.ctor", "Constructed(A)", "WillChange", "A.Start");
    }

    [Fact]
    public void LoadScene_ShouldReleasePreviousAfterNewOnStart()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            app.LoadScene<HookSceneB>();
        });

        Log.Should()
            .Equal(
                "A.Destroy",
                "Constructing",
                "B.ctor",
                "Constructed(B)",
                "WillChange",
                "B.Start",
                "Released(A)"
            );
    }

    [Fact]
    public void LoadScene_WhenOnStartThrows_ShouldStillReleasePrevious()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            var act = () => app.LoadScene<ThrowingOnStartScene>();
            act.Should().Throw<InvalidOperationException>();
        });

        Log.Should()
            .Equal(
                "A.Destroy",
                "Constructing",
                "ThrowingOnStart.ctor",
                "Constructed(ThrowingOnStart)",
                "WillChange",
                "ThrowingOnStart.Start",
                "Released(A)"
            );
    }

    [Fact]
    public void LoadScene_WhenConstructorThrows_ShouldNotifyFailureAndReleasePreviousImmediately()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            var act = () => app.LoadScene<ThrowingCtorScene>();
            act.Should().Throw<InvalidOperationException>();
        });

        Log.Should()
            .Equal(
                "A.Destroy",
                "Constructing",
                "ThrowingCtor.ctor",
                "ConstructionFailed",
                "Released(A)"
            );
    }

    [Fact]
    public void PushScene_ShouldNotReleaseAnything()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            app.PushScene<HookSceneB>();
        });

        Log.Should()
            .Equal("A.Pause", "Constructing", "B.ctor", "Constructed(B)", "WillChange", "B.Start");
    }

    [Fact]
    public void PushScene_WhenConstructorThrows_ShouldNotifyFailureAndResume()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            var act = () => app.PushScene<ThrowingCtorScene>();
            act.Should().Throw<InvalidOperationException>();
        });

        Log.Should()
            .Equal(
                "A.Pause",
                "Constructing",
                "ThrowingCtor.ctor",
                "ConstructionFailed",
                "A.Resume"
            );
    }

    [Fact]
    public void PopScene_ShouldReleaseTopAfterOnResume()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            app.PushScene<HookSceneB>();
            Log.Clear();
            app.PopScene();
        });

        Log.Should().Equal("B.Destroy", "WillChange", "A.Resume", "Released(B)");
    }

    [Fact]
    public void PopScene_WhenOnResumeThrows_ShouldStillReleaseTop()
    {
        using var app = BuildApp();

        app.RunWith<ThrowingOnResumeScene>(() =>
        {
            app.PushScene<HookSceneB>();
            Log.Clear();
            var act = () => app.PopScene();
            act.Should().Throw<InvalidOperationException>();
        });

        Log.Should().Equal("B.Destroy", "WillChange", "ThrowingOnResume.Resume", "Released(B)");
    }

    [Fact]
    public void ClearSceneStack_ShouldReleaseEachRightAfterOnDestroy()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            app.PushScene<HookSceneB>();
            app.PushScene<HookSceneC>();
            Log.Clear();
            app.ClearSceneStack();
        });

        Log.Should().Equal("B.Destroy", "Released(B)", "A.Destroy", "Released(A)");
    }

    [Fact]
    public void AppDestroy_ShouldReleaseCurrentAndStackedScenes()
    {
        // アプリの終了 (バックエンドが呼ぶ OnDestroy)。OnDestroy の中で Dispose されるので using しない
        var app = BuildApp();

        app.RunWith<HookSceneA>(() => app.PushScene<HookSceneB>());
        Log.Clear();
        app.OnDestroy();

        Log.Should().Equal("B.Destroy", "Released(B)", "A.Destroy", "Released(A)");
    }

    [Fact]
    public void NestedLoad_InOnStart_ShouldReleaseInnerThenOuter()
    {
        using var app = BuildApp();

        app.RunWith<HookSceneA>(() =>
        {
            Log.Clear();
            app.LoadScene<NestingScene>();
        });

        app.CurrentScene.Should().BeOfType<HookSceneC>();
        Log.Should()
            .Equal(
                "A.Destroy",
                "Constructing",
                "Nesting.ctor",
                "Constructed(Nesting)",
                "WillChange",
                "Nesting.Start",
                "Nesting.Destroy",
                "Constructing",
                "C.ctor",
                "Constructed(C)",
                "WillChange",
                "C.Start",
                "Released(Nesting)",
                "Nesting.StartEnd",
                "Released(A)"
            );
    }

    [Fact]
    public void Hooks_ShouldBeCalledInRegistrationOrder()
    {
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneLifecycleHookTests>()
            .BuildWithHeadless();
        Log.Clear();
        app.AddSceneLifecycleHook(new RecordingHook("1:"));
        app.AddSceneLifecycleHook(new RecordingHook("2:"));

        app.RunWith<HookSceneA>(() => { });

        Log.Should()
            .Equal(
                "1:Constructing",
                "2:Constructing",
                "A.ctor",
                "1:Constructed(A)",
                "2:Constructed(A)",
                "A.Start"
            );
    }

    [Fact]
    public void SceneTransitions_WithoutHooks_ShouldBehaveAsBefore()
    {
        // フックが無いときは何も変わらない (既存の SceneTransitionFailureTests と同じ観点の確認)
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneLifecycleHookTests>()
            .BuildWithHeadless();
        Log.Clear();

        app.RunWith<HookSceneA>(() =>
        {
            app.PushScene<HookSceneB>();
            app.PopScene();
            app.LoadScene<HookSceneC>();
        });

        Log.Should()
            .Equal(
                "A.ctor",
                "A.Start",
                "A.Pause",
                "B.ctor",
                "B.Start",
                "B.Destroy",
                "A.Resume",
                "A.Destroy",
                "C.ctor",
                "C.Start"
            );
    }

    private static string NameOf(Scene scene) =>
        scene.GetType().Name.Replace("Scene", "").Replace("Hook", "");

    private sealed class RecordingHook(string prefix = "") : ISceneLifecycleHook
    {
        public void OnSceneConstructing() => Log.Add(prefix + "Constructing");

        public void OnSceneConstructed(Scene scene) =>
            Log.Add($"{prefix}Constructed({NameOf(scene)})");

        public void OnSceneConstructionFailed() => Log.Add(prefix + "ConstructionFailed");

        public void OnSceneReleased(Scene scene) => Log.Add($"{prefix}Released({NameOf(scene)})");
    }

    internal abstract class LoggingScene : Scene
    {
        public LoggingScene()
        {
            Log.Add(Name + ".ctor");
        }

        protected string Name => NameOf(this);

        public override void OnStart() => Log.Add(Name + ".Start");

        public override void OnDestroy() => Log.Add(Name + ".Destroy");

        public override void OnPause() => Log.Add(Name + ".Pause");

        public override void OnResume() => Log.Add(Name + ".Resume");
    }

    internal sealed class HookSceneA : LoggingScene { }

    internal sealed class HookSceneB : LoggingScene { }

    internal sealed class HookSceneC : LoggingScene { }

    internal sealed class ThrowingOnStartScene : LoggingScene
    {
        public override void OnStart()
        {
            base.OnStart();
            throw new InvalidOperationException("OnStart で失敗しました。");
        }
    }

    internal sealed class ThrowingOnResumeScene : LoggingScene
    {
        public override void OnResume()
        {
            base.OnResume();
            throw new InvalidOperationException("OnResume で失敗しました。");
        }
    }

    internal sealed class ThrowingCtorScene : Scene
    {
        public ThrowingCtorScene()
        {
            Log.Add("ThrowingCtor.ctor");
            throw new InvalidOperationException("シーンの生成に失敗しました。");
        }
    }

    internal sealed class NestingScene : LoggingScene
    {
        public override void OnStart()
        {
            base.OnStart();
            App.LoadScene<HookSceneC>();
            Log.Add(Name + ".StartEnd");
        }
    }
}
