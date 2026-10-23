using System.Diagnostics;
using FluentAssertions;
using Promete.Assets;
using Promete.Headless;
using Promete.Test.AssetsApi;
using Promete.Windowing;

namespace Promete.Test;

/// <summary>
/// ヘッドレスのバックエンドの更新ループのテスト。更新が Run を呼んだスレッド (メインスレッド) で動くこと。
/// </summary>
/// <remarks>
/// 各テストは、ループが止まらない不具合でテストが固まらないよう、一定時間で <see cref="PrometeApp.Exit"/> を呼ぶ見張りを付けて動かす。
/// </remarks>
public sealed class HeadlessBackendLoopTests
{
    private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Run_UpdateShouldRunOnRunThread()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        var runThread = Environment.CurrentManagedThreadId;
        var updateThreads = new List<int>();
        var isMainThread = new List<bool>();
        app.Update += () =>
        {
            updateThreads.Add(Environment.CurrentManagedThreadId);
            isMainThread.Add(app.IsMainThread());
            if (updateThreads.Count >= 3)
                app.Exit();
        };

        RunWithWatchdog(app, () => app.Run());

        updateThreads.Should().HaveCount(3);
        updateThreads
            .Should()
            .OnlyContain(id => id == runThread, "更新は Run を呼んだスレッドで動くはず");
        isMainThread.Should().OnlyContain(b => b);
    }

    [Fact]
    public void Run_SceneScopeInOnUpdate_ShouldWork()
    {
        LoadingScenes.Reset();
        using var app = BuildWithAssets();

        RunWithWatchdog(app, () => app.Run<LoadInUpdateScene>());

        LoadingScenes.Errors.Should().BeEmpty();
        LoadingScenes.Loaded.Should().Equal("update:A");
    }

    [Fact]
    public void Run_LoadSceneFromOnUpdate_NewSceneConstructorCanUseSceneScope()
    {
        LoadingScenes.Reset();
        using var app = BuildWithAssets();

        RunWithWatchdog(app, () => app.Run<SwitchInUpdateScene>());

        LoadingScenes.Errors.Should().BeEmpty();
        LoadingScenes.Loaded.Should().Equal("ctor:A", "start");
    }

    [Fact]
    public void Run_ExitInStart_ShouldNotUpdate()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        var updates = 0;
        app.Start += () => app.Exit();
        app.Update += () => updates++;

        var status = RunWithWatchdog(app, () => app.Run());

        status.Should().Be(0);
        updates.Should().Be(0);
    }

    [Fact]
    public void Run_ShouldReturnExitStatus()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        app.Update += () => app.Exit(3);

        var status = RunWithWatchdog(app, () => app.Run());

        status.Should().Be(3);
    }

    [Fact]
    public void Run_ExitFromAnotherThread_ShouldStopLoop()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        var updates = 0;
        app.Update += () => updates++;
        using var exitTimer = new Timer(_ => app.Exit(), null, 200, Timeout.Infinite);
        var stopwatch = Stopwatch.StartNew();

        RunWithWatchdog(app, () => app.Run());

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        updates.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Run_ShouldTickWithFixedDeltaOfTargetUps()
    {
        using var app = PrometeApp
            .Create()
            .BuildWithHeadless(WindowOptions.Default with { TargetUps = 50 });
        var deltas = new List<float>();
        app.Update += () =>
        {
            deltas.Add(app.Time.DeltaTime);
            if (deltas.Count >= 3)
                app.Exit();
        };

        RunWithWatchdog(app, () => app.Run());

        deltas.Should().AllSatisfy(d => d.Should().BeApproximately(1f / 50, 1e-6f));
        app.Time.TotalFrame.Should().Be(3);
    }

    [Fact]
    public void Run_ShouldPaceUpdatesInRealTime()
    {
        // 今のタイマーと同じく、TargetUps の間隔で更新する (空回りしない)
        using var app = PrometeApp
            .Create()
            .BuildWithHeadless(WindowOptions.Default with { TargetUps = 20 });
        var updates = 0;
        app.Update += () =>
        {
            if (++updates >= 10)
                app.Exit();
        };
        var stopwatch = Stopwatch.StartNew();

        RunWithWatchdog(app, () => app.Run());

        stopwatch
            .Elapsed.Should()
            .BeGreaterThan(TimeSpan.FromMilliseconds(350), "10 回の更新に 1/20 秒ずつかかるはず");
    }

    [Fact]
    public void Run_ExceptionInUpdate_ShouldPropagateFromRun()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();
        app.Update += () => throw new InvalidDataException("更新で失敗しました。");

        var act = () => RunWithWatchdog(app, () => app.Run());

        act.Should().Throw<InvalidDataException>("更新の例外は握りつぶさず、Run から伝わるはず");
    }

    private static PrometeApp BuildWithAssets()
    {
        var source = new TrackingAssetSource().Add("a.txt", TestAssetData.Text("A"));
        return PrometeApp
            .Create()
            .UseScenesFrom<HeadlessBackendLoopTests>()
            .UseAssets(typeof(HeadlessBackendLoopTests).Assembly, o => o.Mount(source))
            .BuildWithHeadless();
    }

    /// <summary>
    /// 見張りを付けて Run を呼ぶ。見張りは一定時間後に Exit する (固まったテストを失敗で終わらせるため)。
    /// </summary>
    private static int RunWithWatchdog(PrometeApp app, Func<int> run)
    {
        using var watchdog = new Timer(
            _ => app.Exit(-1),
            null,
            WatchdogTimeout,
            Timeout.InfiniteTimeSpan
        );
        return run();
    }

    /// <summary>
    /// テストのシーンが記録する結果。
    /// </summary>
    internal static class LoadingScenes
    {
        public static List<string> Loaded { get; } = [];

        public static List<Exception> Errors { get; } = [];

        public static void Reset()
        {
            Loaded.Clear();
            Errors.Clear();
        }

        /// <summary>
        /// 例外を記録してアプリを終了する。ループのスレッドで投げられた例外を、テストのスレッドで確かめるため。
        /// </summary>
        public static void Guard(PrometeApp app, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Errors.Add(e);
                app.Exit();
            }
        }
    }

    /// <summary>
    /// OnUpdate で <see cref="AssetScope.Scene"/> を指定して読むシーン。
    /// </summary>
    internal sealed class LoadInUpdateScene(AssetManager assets) : Scene
    {
        public override void OnUpdate()
        {
            LoadingScenes.Guard(
                App,
                () =>
                {
                    LoadingScenes.Loaded.Add(
                        "update:"
                            + assets.Load("a.txt", "k", s => s.ReadAllText(), AssetScope.Scene)
                    );
                    App.Exit();
                }
            );
        }
    }

    /// <summary>
    /// OnUpdate で <see cref="SceneScopeInConstructorScene"/> へ LoadScene するシーン。
    /// </summary>
    internal sealed class SwitchInUpdateScene : Scene
    {
        public override void OnUpdate()
        {
            LoadingScenes.Guard(App, () => App.LoadScene<SceneScopeInConstructorScene>());
        }
    }

    /// <summary>
    /// コンストラクタで <see cref="AssetScope.Scene"/> を指定して読むシーン。
    /// </summary>
    internal sealed class SceneScopeInConstructorScene : Scene
    {
        public SceneScopeInConstructorScene(AssetManager assets)
        {
            LoadingScenes.Loaded.Add(
                "ctor:" + assets.Load("a.txt", "k", s => s.ReadAllText(), AssetScope.Scene)
            );
        }

        public override void OnStart()
        {
            LoadingScenes.Loaded.Add("start");
            App.Exit();
        }
    }
}
