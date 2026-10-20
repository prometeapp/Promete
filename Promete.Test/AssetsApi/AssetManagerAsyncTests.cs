using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 非同期の読み込みの共通の規則 (<see cref="AssetManager.LoadAsync{T}(string, string, Func{Stream, Task{T}}, AssetScope, CancellationToken)"/>) のテスト。
/// 実行中の読み込みの共有、完了前のスコープの解放、同期の読み込みとの重なり、キャンセル。
/// </summary>
/// <remarks>
/// テストのメソッドは同期にして、Task をブロックして待つ。await するとメインスレッド以外で続きが動き、<see cref="AssetScope.Scene"/> や解放の時点が変わるため。
/// </remarks>
public sealed class AssetManagerAsyncTests : IDisposable
{
    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("data/a.txt", TestAssetData.Text("A"))
        .Add("data/b.txt", TestAssetData.Text("B"));

    private readonly PrometeApp _app;
    private readonly List<TrackedDisposable> _created = [];
    private int _factoryCalls;

    public AssetManagerAsyncTests()
    {
        ScriptedScene.Script.Reset();
        _app = AssetsTestApp.Build(_source);
    }

    private AssetManager Assets => _app.Assets();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void LoadAsync_ShouldReturnFactoryResult()
    {
        var value = Assets
            .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        value.Content.Should().Be("A");
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void LoadAsync_ShouldNotOpenStreamOnCallingThread()
    {
        var gate = _source.BlockNextOpen("data/a.txt");

        var task = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);

        task.IsCompleted.Should().BeFalse("OpenRead も含めてスレッドプールで行うはず");
        gate.WaitEntered();
        gate.Release();
        task.GetAwaiter().GetResult().Content.Should().Be("A");
    }

    [Fact]
    public void LoadAsync_ConcurrentSameKey_ShouldShareOneLoad()
    {
        var gate = _source.BlockNextOpen("data/a.txt");

        var first = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);
        var second = Assets.LoadAsync("./data/a.txt", "k", ReadAsync, AssetScope.Global);
        gate.WaitEntered();
        gate.Release();

        first.GetAwaiter().GetResult().Should().BeSameAs(second.GetAwaiter().GetResult());
        _factoryCalls.Should().Be(1);
        _source.OpenCount("data/a.txt").Should().Be(1);
    }

    [Fact]
    public void LoadAsync_DifferentKeys_ShouldNotShare()
    {
        var first = Assets.LoadAsync("data/a.txt", "one", ReadAsync, AssetScope.Global);
        var second = Assets.LoadAsync("data/a.txt", "two", ReadAsync, AssetScope.Global);

        first.GetAwaiter().GetResult().Should().NotBeSameAs(second.GetAwaiter().GetResult());
        _factoryCalls.Should().Be(2);
    }

    [Fact]
    public void LoadAsync_AfterCompletion_ShouldShareCacheWithSyncLoad()
    {
        var value = Assets
            .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        Assets.Load("data/a.txt", "k", Read, AssetScope.Global).Should().BeSameAs(value);
        Assets
            .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult()
            .Should()
            .BeSameAs(value);
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void SyncLoad_ThenAsync_ShouldHitCache()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        var task = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);

        task.GetAwaiter().GetResult().Should().BeSameAs(value);
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void LoadAsync_ShouldCloseStreamAfterFactoryTaskCompletes()
    {
        var release = new TaskCompletionSource();
        Stream? seen = null;

        var task = Assets.LoadAsync(
            "data/a.txt",
            "k",
            async s =>
            {
                seen = s;
                await release.Task;
                return new TrackedDisposable(s.ReadAllText());
            },
            AssetScope.Global
        );
        SpinWait.SpinUntil(() => Volatile.Read(ref seen) != null, TimeSpan.FromSeconds(10));
        var openWhilePending = !_source.StreamsOf("data/a.txt").Single().IsDisposed;
        release.SetResult();
        var value = task.GetAwaiter().GetResult();

        openWhilePending.Should().BeTrue("factory の Task が完了するまで閉じないはず");
        value.Content.Should().Be("A");
        _source.StreamsOf("data/a.txt").Single().IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void LoadAsync_LeaveOpen_ShouldHandStreamOverToFactory()
    {
        Stream? kept = null;

        Assets
            .LoadAsync(
                "data/a.txt",
                "k",
                s =>
                {
                    kept = s;
                    return Task.FromResult(0);
                },
                AssetScope.Global,
                leaveOpen: true
            )
            .GetAwaiter()
            .GetResult();

        _source.StreamsOf("data/a.txt").Single().IsDisposed.Should().BeFalse();
        kept!.ReadAllText().Should().Be("A");
        kept.Dispose();
    }

    [Fact]
    public void LoadAsync_FactoryFaults_ShouldPropagateToAllWaitersAndNotCache()
    {
        var gate = _source.BlockNextOpen("data/a.txt");
        Func<Stream, Task<TrackedDisposable>> failing = _ =>
            Task.FromException<TrackedDisposable>(new InvalidDataException("壊れたデータ"));

        var first = Assets.LoadAsync("data/a.txt", "k", failing, AssetScope.Global);
        var second = Assets.LoadAsync("data/a.txt", "k", failing, AssetScope.Global);
        gate.WaitEntered();
        gate.Release();

        var act1 = () => first.GetAwaiter().GetResult();
        var act2 = () => second.GetAwaiter().GetResult();
        act1.Should().Throw<InvalidDataException>();
        act2.Should().Throw<InvalidDataException>();
        _source.StreamsOf("data/a.txt").Should().AllSatisfy(s => s.IsDisposed.Should().BeTrue());

        Assets
            .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult()
            .Content.Should()
            .Be("A");
    }

    [Fact]
    public void LoadAsync_MissingFile_ShouldFaultAndNotCache()
    {
        var task = Assets.LoadAsync("data/missing.txt", "k", ReadAsync, AssetScope.Global);

        var act = () => task.GetAwaiter().GetResult();
        act.Should().Throw<FileNotFoundException>();

        _source.Add("data/missing.txt", TestAssetData.Text("now"));
        Assets
            .LoadAsync("data/missing.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult()
            .Content.Should()
            .Be("now");
    }

    [Fact]
    public void LoadAsync_ArgumentAndScopeErrors_ShouldThrowSynchronously()
    {
        Action invalidPath = () =>
            _ = Assets.LoadAsync(@"data\a.txt", "k", ReadAsync, AssetScope.Global);
        Action nullPath = () => _ = Assets.LoadAsync(null!, "k", ReadAsync, AssetScope.Global);
        Action nullKey = () =>
            _ = Assets.LoadAsync("data/a.txt", null!, ReadAsync, AssetScope.Global);
        Action nullFactory = () =>
            _ = Assets.LoadAsync<TrackedDisposable>("data/a.txt", "k", null!, AssetScope.Global);
        Action undefinedScope = () =>
            _ = Assets.LoadAsync("data/a.txt", "k", ReadAsync, (AssetScope)42);
        Action sceneWithoutScene = () =>
            _ = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Scene);

        invalidPath.Should().Throw<ArgumentException>();
        nullPath.Should().Throw<ArgumentNullException>();
        nullKey.Should().Throw<ArgumentNullException>();
        nullFactory.Should().Throw<ArgumentNullException>();
        undefinedScope.Should().Throw<ArgumentOutOfRangeException>();
        sceneWithoutScene.Should().Throw<InvalidOperationException>();
        _source.OpenCount("data/a.txt").Should().Be(0);
    }

    [Fact]
    public void LoadAsync_AfterDispose_ShouldThrowSynchronously()
    {
        Assets.Dispose();

        Action act = () => _ = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void LoadAsync_SceneScopeFromWorkerThread_ShouldThrowSynchronously()
    {
        Exception? thrown = null;

        _app.RunWith<ScriptedEmptyScene>(() =>
            AssetsTestApp.RunOnWorkerThread(() =>
            {
                try
                {
                    _ = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Scene);
                }
                catch (Exception e)
                {
                    thrown = e;
                }
            })
        );

        thrown.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void LoadAsync_Cancellation_ShouldOnlyStopWaiting()
    {
        var gate = _source.BlockNextOpen("data/a.txt");
        using var cts = new CancellationTokenSource();

        var canceled = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global, cts.Token);
        gate.WaitEntered();
        cts.Cancel();
        var act = () => canceled.GetAwaiter().GetResult();
        act.Should().Throw<OperationCanceledException>();

        gate.Release();
        var value = Assets
            .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        _factoryCalls.Should().Be(1, "共有している読み込み自体は止まらないはず");
        value.IsDisposed.Should().BeFalse("キャンセルした呼び出し元のスコープの分も残るはず");
    }

    [Fact]
    public void LoadAsync_AlreadyCanceledToken_ShouldNotStartLoading()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () =>
            Assets
                .LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global, cts.Token)
                .GetAwaiter()
                .GetResult();

        act.Should().Throw<OperationCanceledException>();
        _source.OpenCount("data/a.txt").Should().Be(0);
        _factoryCalls.Should().Be(0);
    }

    [Fact]
    public void LoadAsync_SceneReleasedBeforeCompletion_ShouldDisposeResultOnMainThread()
    {
        TrackedDisposable? result = null;
        var disposedBeforePump = true;

        _app.RunWith<ScriptedSceneA>(() =>
        {
            var gate = _source.BlockNextOpen("data/a.txt");
            var task = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Scene);
            gate.WaitEntered();

            LoadEmptyScene();
            gate.Release();
            result = task.GetAwaiter().GetResult();
            disposedBeforePump = result.IsDisposed;
            _app.PumpFrame();
        });

        disposedBeforePump
            .Should()
            .BeFalse("完了はスレッドプールで起きるので、解放は次のフレームに回るはず");
        result!
            .DisposeCount.Should()
            .Be(1, "所有するスコープが無くなっていたので、完了した時点で解放するはず");
        Assets
            .Load("data/a.txt", "k", Read, AssetScope.Global)
            .Should()
            .NotBeSameAs(result, "解放したものはキャッシュに残らないはず");
    }

    [Fact]
    public void LoadAsync_SceneReleasedButAnotherScopeJoined_ShouldKeepResult()
    {
        TrackedDisposable? result = null;

        _app.RunWith<ScriptedSceneA>(() =>
        {
            var gate = _source.BlockNextOpen("data/a.txt");
            var sceneTask = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Scene);
            var globalTask = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);
            gate.WaitEntered();

            LoadEmptyScene();
            gate.Release();
            result = sceneTask.GetAwaiter().GetResult();
            globalTask.GetAwaiter().GetResult().Should().BeSameAs(result);
            _app.PumpFrame();
        });

        result!.IsDisposed.Should().BeFalse("合流した Global が持ち続けるはず");
        Assets.Load("data/a.txt", "k", Read, AssetScope.Global).Should().BeSameAs(result);
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void LoadAsync_UnloadBeforeCompletion_ShouldDisposeResultOnCompletion()
    {
        var gate = _source.BlockNextOpen("data/a.txt");
        var task = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);
        gate.WaitEntered();

        Assets.Unload("data/a.txt");
        gate.Release();
        var result = task.GetAwaiter().GetResult();
        _app.PumpFrame();

        result.DisposeCount.Should().Be(1);
        Assets.Load("data/a.txt", "k", Read, AssetScope.Global).Should().NotBeSameAs(result);
    }

    [Fact]
    public void SyncLoad_WhileAsyncIsPending_ShouldNotWaitAndShouldWin()
    {
        var gate = _source.BlockNextOpen("data/a.txt");
        var asyncTask = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);
        gate.WaitEntered();

        // 実行中の非同期の読み込みを待たずに (デッドロックせずに) 読み込む
        var syncValue = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        gate.Release();
        var asyncValue = asyncTask.GetAwaiter().GetResult();
        _app.PumpFrame();

        asyncValue.Should().BeSameAs(syncValue, "先にエントリに入った同期の結果を返すはず");
        syncValue.IsDisposed.Should().BeFalse();
        _created.Should().HaveCount(2);
        _created
            .Single(v => !ReferenceEquals(v, syncValue))
            .DisposeCount.Should()
            .Be(1, "非同期の方の結果は捨てられるはず");
    }

    [Fact]
    public void Dispose_WhileAsyncIsPending_ShouldDisposeResultOnCompletion()
    {
        var gate = _source.BlockNextOpen("data/a.txt");
        var task = Assets.LoadAsync("data/a.txt", "k", ReadAsync, AssetScope.Global);
        gate.WaitEntered();

        Assets.Dispose();
        gate.Release();
        var result = task.GetAwaiter().GetResult();

        result.DisposeCount.Should().Be(1, "破棄した後に完了したものは、その場で解放するはず");
    }

    private void LoadEmptyScene() => _app.LoadScene<ScriptedEmptyScene>();

    private TrackedDisposable Read(Stream stream)
    {
        Interlocked.Increment(ref _factoryCalls);
        var value = new TrackedDisposable(stream.ReadAllText());
        lock (_created)
            _created.Add(value);
        return value;
    }

    private Task<TrackedDisposable> ReadAsync(Stream stream) => Task.FromResult(Read(stream));
}
