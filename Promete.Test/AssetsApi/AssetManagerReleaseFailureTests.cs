using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 解放の途中でリソースの Dispose が例外を投げても、残りのアセットを最後まで解放することのテスト。
/// </summary>
public sealed class AssetManagerReleaseFailureTests
{
    private readonly InMemoryAssetSource _source = new(
        ("a.txt", "A"),
        ("b.txt", "B"),
        ("c.txt", "C"),
        ("d.txt", "D")
    );

    public AssetManagerReleaseFailureTests()
    {
        ScriptedScene.Script.Reset();
    }

    [Fact]
    public void SceneRelease_WhenDisposeThrows_ShouldReleaseRemainingAndThrowAggregate()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        Exception? thrown = null;
        List<ReleaseProbe> probes = [];

        app.RunWith<ScriptedSceneA>(() =>
        {
            probes = LoadProbes(
                assets,
                AssetScope.Scene,
                ("a.txt", true),
                ("b.txt", false),
                ("c.txt", true)
            );
            thrown = Catch(() => app.LoadScene<ScriptedEmptyScene>());
            app.CurrentScene.Should().BeOfType<ScriptedEmptyScene>("遷移自体は終わっているはず");
        });

        thrown
            .Should()
            .BeOfType<AggregateException>()
            .Which.InnerExceptions.Should()
            .HaveCount(2)
            .And.AllBeOfType<ProbeException>();
        probes
            .Should()
            .AllSatisfy(p => p.DisposeCount.Should().Be(1, "例外があっても残りを解放するはず"));
    }

    [Fact]
    public void LoadScene_OnStartThrowsAndReleaseThrows_ShouldSurfaceOnStartException()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        Exception? thrown = null;
        List<ReleaseProbe> probes = [];

        app.RunWith<ScriptedSceneA>(() =>
        {
            probes = LoadProbes(assets, AssetScope.Scene, ("a.txt", true), ("b.txt", false));
            thrown = Catch(() => app.LoadScene<ScriptedThrowingOnStartScene>());
        });

        thrown
            .Should()
            .BeOfType<InvalidOperationException>("解放の例外が、元の OnStart の例外を隠さないはず")
            .Which.Message.Should()
            .Be("OnStart で失敗しました。");
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
    }

    [Fact]
    public void LoadScene_ConstructorThrowsAndReleaseThrows_ShouldSurfaceConstructorException()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        Exception? thrown = null;
        List<ReleaseProbe> probes = [];

        app.RunWith<ScriptedSceneA>(() =>
        {
            probes = LoadProbes(assets, AssetScope.Scene, ("a.txt", true), ("b.txt", false));
            thrown = Catch(() => app.LoadScene<ScriptedThrowingCtorScene>());
        });

        thrown
            .Should()
            .BeOfType<InvalidOperationException>("解放の例外が、元の生成の例外を隠さないはず")
            .Which.Message.Should()
            .Be("シーンの生成に失敗しました。");
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
    }

    [Fact]
    public void PopScene_OnResumeThrowsAndReleaseThrows_ShouldSurfaceOnResumeException()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        Exception? thrown = null;
        List<ReleaseProbe> probes = [];

        app.RunWith<ScriptedThrowingOnResumeScene>(() =>
        {
            app.PushScene<ScriptedSceneB>();
            probes = LoadProbes(assets, AssetScope.Scene, ("a.txt", true), ("b.txt", false));
            thrown = Catch(() => app.PopScene());
        });

        thrown
            .Should()
            .BeOfType<InvalidOperationException>("解放の例外が、元の OnResume の例外を隠さないはず")
            .Which.Message.Should()
            .Be("OnResume で失敗しました。");
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
    }

    [Fact]
    public void AppDestroy_WhenReleaseThrows_ShouldStillReleaseStackedScenesGlobalAndFileSystem()
    {
        // OnDestroy の中で Dispose されるので using しない
        var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        List<ReleaseProbe> lower = [];
        List<ReleaseProbe> upper = [];
        List<ReleaseProbe> global = [];

        app.RunWith<ScriptedSceneA>(() =>
        {
            lower = LoadProbes(assets, AssetScope.Scene, ("a.txt", false));
            global = LoadProbes(assets, AssetScope.Global, ("d.txt", false));
            app.PushScene<ScriptedSceneB>();
            upper = LoadProbes(assets, AssetScope.Scene, ("b.txt", true), ("c.txt", false));
        });

        var act = () => app.OnDestroy();

        act.Should().Throw<Exception>("解放の例外は、最後まで処理してから伝えるはず");
        upper.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
        lower
            .Should()
            .AllSatisfy(p =>
                p.DisposeCount.Should()
                    .Be(1, "今のシーンの解放の例外で、スタックのシーンの解放が飛ばされないはず")
            );
        global
            .Should()
            .AllSatisfy(p => p.DisposeCount.Should().Be(1, "AssetManager の Dispose まで届くはず"));
        _source.DisposeCount.Should().Be(1, "FileSystem の Dispose まで届くはず");
    }

    [Fact]
    public void Unload_WhenDisposeThrows_ShouldReleaseRemainingAndThrowAggregate()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var probes = LoadProbes(
            assets,
            AssetScope.Global,
            ("a.txt", true),
            ("b.txt", false),
            ("c.txt", true)
        );

        var act = () => assets.Unload(AssetScope.Global);

        act.Should().Throw<AggregateException>().Which.InnerExceptions.Should().HaveCount(2);
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
        var reloaded = assets.Load(
            "a.txt",
            "probe",
            s => new ReleaseProbe(s.ReadAllText(), false),
            AssetScope.Global
        );
        reloaded.Should().NotBeSameAs(probes[0], "解放したものはキャッシュから外れているはず");
    }

    [Fact]
    public void UnloadPath_WhenDisposeThrows_ShouldReleaseAllEntriesOfPath()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var first = assets.Load(
            "a.txt",
            "one",
            s => new ReleaseProbe(s.ReadAllText(), true),
            AssetScope.Global
        );
        var second = assets.Load(
            "a.txt",
            "two",
            s => new ReleaseProbe(s.ReadAllText(), false),
            AssetScope.Global
        );

        var act = () => assets.Unload("a.txt");

        act.Should().Throw<AggregateException>();
        first.DisposeCount.Should().Be(1);
        second.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_WhenDisposeThrows_ShouldReleaseRemainingAndDisposeFileSystem()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var probes = LoadProbes(
            assets,
            AssetScope.Global,
            ("a.txt", true),
            ("b.txt", false),
            ("c.txt", true)
        );

        var act = () => assets.Dispose();

        act.Should().Throw<AggregateException>().Which.InnerExceptions.Should().HaveCount(2);
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
        _source.DisposeCount.Should().Be(1, "FileSystem.Dispose は finally で行うはず");
        assets.Invoking(a => a.Dispose()).Should().NotThrow("2 回目は何もしないはず");
    }

    [Fact]
    public void PendingReleases_WhenDisposeThrows_ShouldReleaseRemainingAndNotLeaveThemQueued()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var probes = LoadProbes(
            assets,
            AssetScope.Global,
            ("a.txt", true),
            ("b.txt", false),
            ("c.txt", true)
        );
        AssetsTestApp.RunOnWorkerThread(() => assets.Unload(AssetScope.Global));
        probes
            .Should()
            .AllSatisfy(p =>
                p.DisposeCount.Should().Be(0, "メインスレッド以外の解放は次のフレームに回るはず")
            );

        var pump = () => app.PumpFrame();

        pump.Should().Throw<AggregateException>().Which.InnerExceptions.Should().HaveCount(2);
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
        pump.Should().NotThrow("解放の予約はキューに残らないはず");
        probes.Should().AllSatisfy(p => p.DisposeCount.Should().Be(1));
    }

    private static List<ReleaseProbe> LoadProbes(
        AssetManager assets,
        AssetScope scope,
        params (string Path, bool Throws)[] items
    )
    {
        return items
            .Select(item =>
                assets.Load(
                    item.Path,
                    "probe",
                    s => new ReleaseProbe(s.ReadAllText(), item.Throws),
                    scope
                )
            )
            .ToList();
    }

    private static Exception? Catch(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Dispose の回数を数え、指定されたら Dispose で例外を投げるリソース。
    /// </summary>
    internal sealed class ReleaseProbe(string content, bool throws) : IDisposable
    {
        private int _disposeCount;

        public string Content { get; } = content;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            if (throws)
                throw new ProbeException($"{Content} の解放に失敗しました。");
        }
    }

    internal sealed class ProbeException(string message) : Exception(message);
}
