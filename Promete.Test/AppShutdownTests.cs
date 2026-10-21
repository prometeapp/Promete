using FluentAssertions;
using Promete.Assets;
using Promete.Headless;
using Promete.Test.AssetsApi;

namespace Promete.Test;

/// <summary>
/// アプリの終了 (<see cref="PrometeApp.OnDestroy"/> と <see cref="PrometeApp.Dispose"/>) が、途中の例外で止まらないことのテスト。
/// </summary>
/// <remarks>
/// どのテストもアプリが破棄済み (または破棄に失敗した) 状態で終わるので、<c>using</c> しない。
/// </remarks>
public sealed class AppShutdownTests
{
    private static List<string> Log { get; } = [];

    public AppShutdownTests()
    {
        Log.Clear();
        TrackedPlugin.Reset();
        ScriptedScene.Script.Reset();
    }

    [Fact]
    public void Dispose_WhenAssetManagerThrows_ShouldStillDisposeLaterPluginsAndClearCurrent()
    {
        var source = new InMemoryAssetSource(("a.txt", "A"));
        var app = AssetsTestApp.Build(source, b => b.Use<TrackedPlugin>());
        app.RunWith<ScriptedEmptyScene>(() => LoadThrowingGlobal(app));

        var act = () => app.Dispose();

        act.Should()
            .Throw<Exception>("AssetManager の解放の例外は、最後まで処理してから伝えるはず");
        TrackedPlugin
            .DisposeCount.Should()
            .BeGreaterThan(0, "後から登録したプラグインも破棄されるはず");
        PrometeApp.CurrentOrNull.Should().BeNull("Current は必ず外れるはず");
        var resolve = () => app.GetPlugin<TrackedPlugin>();
        resolve.Should().Throw<ObjectDisposedException>("サービスプロバイダも破棄されるはず");
    }

    [Fact]
    public void OnDestroy_WhenAssetManagerThrows_ShouldStillDisposeLaterPluginsAndClearCurrent()
    {
        var source = new InMemoryAssetSource(("a.txt", "A"));
        var app = AssetsTestApp.Build(source, b => b.Use<TrackedPlugin>());
        var destroyed = 0;
        app.Destroy += () => destroyed++;
        app.RunWith<ScriptedEmptyScene>(() => LoadThrowingGlobal(app));

        var act = () => app.OnDestroy();

        act.Should().Throw<Exception>();
        TrackedPlugin.DisposeCount.Should().BeGreaterThan(0);
        PrometeApp.CurrentOrNull.Should().BeNull();
        destroyed.Should().Be(1);
    }

    [Fact]
    public void OnDestroy_CollectedAggregateExceptions_ShouldBeFlattened()
    {
        var source = new InMemoryAssetSource(("a.txt", "A"));
        var app = AssetsTestApp.Build(source);
        app.Destroy += () => throw new InvalidOperationException("Destroy で失敗しました。");
        app.RunWith<ScriptedEmptyScene>(() => LoadThrowingGlobal(app));

        var act = () => app.OnDestroy();

        act.Should()
            .Throw<AggregateException>()
            .Which.InnerExceptions.Should()
            .NotContain(
                e => e is AggregateException,
                "集めた AggregateException は平らにして、入れ子にしないはず"
            )
            .And.Contain(e => e is AssetManagerReleaseFailureTests.ProbeException)
            .And.Contain(e => e is InvalidOperationException);
    }

    [Fact]
    public void OnDestroy_WithoutHooks_SceneOnDestroyThrows_ShouldKeepOriginalTypeAndFinishShutdown()
    {
        var app = PrometeApp
            .Create()
            .UseScenesFrom<AppShutdownTests>()
            .Use<TrackedPlugin>()
            .BuildWithHeadless();
        var destroyed = 0;
        app.Destroy += () => destroyed++;
        app.RunWith<ShutdownSceneA>(() =>
        {
            app.PushScene<ShutdownSceneB>();
            app.PushScene<ThrowingOnDestroyScene>();
        });

        var act = () => app.OnDestroy();

        act.Should()
            .Throw<InvalidOperationException>("例外が 1 つなら、元の型のまま伝わるはず")
            .WithMessage("OnDestroy で失敗しました。");
        Log.Should()
            .Equal(
                ["Throwing.Destroy", "B.Destroy", "A.Destroy"],
                "今のシーンの例外で、スタックのシーンの破棄を飛ばさないはず"
            );
        TrackedPlugin.DisposeCount.Should().BeGreaterThan(0, "Dispose まで進むはず");
        destroyed.Should().Be(1, "Destroy イベントまで進むはず");
        PrometeApp.CurrentOrNull.Should().BeNull();
    }

    [Fact]
    public void ClearSceneStack_WithoutHooks_SceneOnDestroyThrows_ShouldKeepOriginalTypeAndDestroyRemaining()
    {
        using var app = PrometeApp.Create().UseScenesFrom<AppShutdownTests>().BuildWithHeadless();
        Exception? thrown = null;
        var popped = true;

        app.RunWith<ShutdownSceneA>(() =>
        {
            app.PushScene<ThrowingOnDestroyScene>();
            app.PushScene<ShutdownSceneB>();
            try
            {
                app.ClearSceneStack();
            }
            catch (Exception e)
            {
                thrown = e;
            }

            popped = app.PopScene();
        });

        thrown
            .Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message.Should()
            .Be("OnDestroy で失敗しました。");
        Log.Should().Equal("Throwing.Destroy", "A.Destroy");
        popped.Should().BeFalse("スタックは空になっているはず");
    }

    /// <summary>
    /// Dispose で例外を投げるリソースを、Global で読み込む。
    /// </summary>
    private static void LoadThrowingGlobal(PrometeApp app)
    {
        app.Assets()
            .Load(
                "a.txt",
                "probe",
                s => new AssetManagerReleaseFailureTests.ReleaseProbe(s.ReadAllText(), true),
                AssetScope.Global
            );
    }

    /// <summary>
    /// 破棄された回数を数えるプラグイン。
    /// </summary>
    internal sealed class TrackedPlugin : IDisposable
    {
        private static int _disposeCount;

        public static int DisposeCount => Volatile.Read(ref _disposeCount);

        public static void Reset() => Volatile.Write(ref _disposeCount, 0);

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }

    internal sealed class ShutdownSceneA : Scene
    {
        public override void OnDestroy() => Log.Add("A.Destroy");
    }

    internal sealed class ShutdownSceneB : Scene
    {
        public override void OnDestroy() => Log.Add("B.Destroy");
    }

    internal sealed class ThrowingOnDestroyScene : Scene
    {
        public override void OnDestroy()
        {
            Log.Add("Throwing.Destroy");
            throw new InvalidOperationException("OnDestroy で失敗しました。");
        }
    }
}
