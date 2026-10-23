using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetManager.Load{T}(string, string, Func{Stream, T}, AssetScope)"/> (汎用の同期の読み込み) と、
/// キャッシュ・<see cref="AssetScope.Global"/>・Dispose の基本のテスト。シーンは使わない。
/// </summary>
public sealed class AssetManagerGenericLoadTests : IDisposable
{
    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("data/a.txt", TestAssetData.Text("A"))
        .Add("data/b.txt", TestAssetData.Text("B"));

    private readonly PrometeApp _app;
    private int _factoryCalls;
    private bool _appDisposed;

    public AssetManagerGenericLoadTests()
    {
        _app = AssetsTestApp.Build(_source);
    }

    private AssetManager Assets => _app.Assets();

    public void Dispose()
    {
        if (!_appDisposed)
            _app.Dispose();
    }

    [Fact]
    public void Load_ShouldReturnFactoryResult()
    {
        var value = Assets.Load("data/a.txt", "text", Read, AssetScope.Global);

        value.Content.Should().Be("A");
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void Load_SameKey_ShouldHitCache()
    {
        var first = Assets.Load("data/a.txt", "text", Read, AssetScope.Global);
        var second = Assets.Load("data/a.txt", "text", Read, AssetScope.Global);

        second.Should().BeSameAs(first);
        _factoryCalls.Should().Be(1);
        _source.OpenCount("data/a.txt").Should().Be(1, "キャッシュに当たれば開き直さないはず");
    }

    [Fact]
    public void Load_EquivalentPaths_ShouldShareEntry()
    {
        var first = Assets.Load("data/a.txt", "text", Read, AssetScope.Global);

        Assets.Load("./data/a.txt", "text", Read, AssetScope.Global).Should().BeSameAs(first);
        Assets.Load("/data/a.txt", "text", Read, AssetScope.Global).Should().BeSameAs(first);
        Assets.Load("data/x/../a.txt", "text", Read, AssetScope.Global).Should().BeSameAs(first);
        _factoryCalls.Should().Be(1);
    }

    [Fact]
    public void Load_DifferentPath_ShouldMissCache()
    {
        var a = Assets.Load("data/a.txt", "text", Read, AssetScope.Global);
        var b = Assets.Load("data/b.txt", "text", Read, AssetScope.Global);

        b.Should().NotBeSameAs(a);
        b.Content.Should().Be("B");
    }

    [Fact]
    public void Load_DifferentKey_ShouldMissCache()
    {
        var first = Assets.Load("data/a.txt", "one", Read, AssetScope.Global);
        var second = Assets.Load("data/a.txt", "two", Read, AssetScope.Global);

        second.Should().NotBeSameAs(first);
        _factoryCalls.Should().Be(2);
    }

    [Fact]
    public void Load_SameKeyDifferentType_ShouldMissCache()
    {
        var tracked = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        var text = Assets.Load("data/a.txt", "k", s => s.ReadAllText(), AssetScope.Global);

        tracked.Content.Should().Be("A");
        text.Should().Be("A");
    }

    [Fact]
    public void Load_KeyIsCaseSensitive()
    {
        var lower = Assets.Load("data/a.txt", "key", Read, AssetScope.Global);
        var upper = Assets.Load("data/a.txt", "KEY", Read, AssetScope.Global);

        upper.Should().NotBeSameAs(lower);
    }

    [Fact]
    public void Load_ShouldCloseStreamAfterFactoryReturns()
    {
        var wasOpenInFactory = false;

        Assets.Load(
            "data/a.txt",
            "k",
            s =>
            {
                wasOpenInFactory = s.CanRead;
                return Read(s);
            },
            AssetScope.Global
        );

        wasOpenInFactory.Should().BeTrue();
        _source.StreamsOf("data/a.txt").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Load_LeaveOpen_ShouldHandStreamOverToFactory()
    {
        Stream? kept = null;

        Assets.Load(
            "data/a.txt",
            "k",
            s =>
            {
                kept = s;
                return 0;
            },
            AssetScope.Global,
            leaveOpen: true
        );

        _source
            .StreamsOf("data/a.txt")
            .Should()
            .ContainSingle()
            .Which.IsDisposed.Should()
            .BeFalse();
        kept!.ReadAllText().Should().Be("A");
        kept.Dispose();
    }

    [Fact]
    public void Load_LeaveOpenFalse_ShouldBehaveLikeDefault()
    {
        Assets.Load("data/a.txt", "k", Read, AssetScope.Global, leaveOpen: false);

        _source.StreamsOf("data/a.txt").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Load_FactoryThrows_ShouldCloseStreamAndNotCache(bool leaveOpen)
    {
        var act = () =>
            Assets.Load<TrackedDisposable>(
                "data/a.txt",
                "k",
                _ => throw new InvalidDataException("壊れたデータ"),
                AssetScope.Global,
                leaveOpen
            );

        act.Should().Throw<InvalidDataException>();
        _source.StreamsOf("data/a.txt").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();

        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        value.Content.Should().Be("A", "失敗した読み込みはキャッシュに残らないはず");
    }

    [Fact]
    public void Load_MissingFile_ShouldThrowFileNotFoundAndNotCache()
    {
        var act = () => Assets.Load("data/missing.txt", "k", Read, AssetScope.Global);

        act.Should().Throw<FileNotFoundException>();

        _source.Add("data/missing.txt", TestAssetData.Text("now"));
        Assets.Load("data/missing.txt", "k", Read, AssetScope.Global).Content.Should().Be("now");
    }

    [Fact]
    public void Load_InvalidPath_ShouldThrowArgumentException()
    {
        var act = () => Assets.Load(@"data\a.txt", "k", Read, AssetScope.Global);

        act.Should().Throw<ArgumentException>();
        _factoryCalls.Should().Be(0);
    }

    [Fact]
    public void Load_NullArguments_ShouldThrowArgumentNull()
    {
        var nullPath = () => Assets.Load(null!, "k", Read, AssetScope.Global);
        var nullKey = () => Assets.Load("data/a.txt", null!, Read, AssetScope.Global);
        var nullFactory = () =>
            Assets.Load<TrackedDisposable>("data/a.txt", "k", null!, AssetScope.Global);

        nullPath.Should().Throw<ArgumentNullException>();
        nullKey.Should().Throw<ArgumentNullException>();
        nullFactory.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Load_UndefinedScope_ShouldThrowArgumentOutOfRange()
    {
        var act = () => Assets.Load("data/a.txt", "k", Read, (AssetScope)42);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Load_GlobalFromWorkerThread_ShouldWork()
    {
        var value = AssetsTestApp.RunOnWorkerThread(() =>
            Assets.Load("data/a.txt", "k", Read, AssetScope.Global)
        );

        value.Content.Should().Be("A");
        Assets.Load("data/a.txt", "k", Read, AssetScope.Global).Should().BeSameAs(value);
    }

    [Fact]
    public void Load_SameKeyFromManyThreads_ShouldEndUpWithOneValue()
    {
        const int threads = 8;
        using var barrier = new Barrier(threads);
        var created = new System.Collections.Concurrent.ConcurrentBag<TrackedDisposable>();

        var tasks = Enumerable
            .Range(0, threads)
            .Select(_ =>
                Task.Factory.StartNew(
                    () =>
                    {
                        barrier.SignalAndWait();
                        return Assets.Load(
                            "data/a.txt",
                            "k",
                            s =>
                            {
                                var value = Read(s);
                                created.Add(value);
                                Thread.Sleep(20);
                                return value;
                            },
                            AssetScope.Global
                        );
                    },
                    TaskCreationOptions.LongRunning
                )
            )
            .ToArray();
        var results = tasks.Select(t => t.Result).ToArray();
        _app.PumpFrame();

        results.Should().AllSatisfy(r => r.Should().BeSameAs(results[0]));
        results[0].IsDisposed.Should().BeFalse();
        created
            .Where(v => !ReferenceEquals(v, results[0]))
            .Should()
            .AllSatisfy(v => v.DisposeCount.Should().Be(1, "採用されなかった結果は解放されるはず"));
    }

    [Fact]
    public void Dispose_ShouldDisposeGlobalAssetsOnce()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        Assets.Dispose();
        Assets.Dispose();

        value.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void AppDispose_ShouldDisposeGlobalAssets()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        _app.Dispose();
        _appDisposed = true;

        value.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_NonDisposableValues_ShouldNotThrow()
    {
        Assets.Load("data/a.txt", "k", s => s.ReadAllText(), AssetScope.Global);

        var act = () => Assets.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Load_AfterDispose_ShouldThrowObjectDisposed()
    {
        Assets.Dispose();

        var act = () => Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        act.Should().Throw<ObjectDisposedException>();
    }

    private TrackedDisposable Read(Stream stream)
    {
        Interlocked.Increment(ref _factoryCalls);
        return new TrackedDisposable(stream.ReadAllText());
    }
}
