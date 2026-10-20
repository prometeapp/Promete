using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetManager.Unload(string, AssetScope)"/> などの、明示的な解放のテスト (<see cref="AssetScope.Global"/> のみ。シーンのスコープは <see cref="AssetManagerSceneReleaseTests"/>)。
/// </summary>
public sealed class AssetManagerUnloadTests : IDisposable
{
    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("data/a.txt", TestAssetData.Text("A"))
        .Add("data/b.txt", TestAssetData.Text("B"));

    private readonly PrometeApp _app;
    private int _factoryCalls;

    public AssetManagerUnloadTests()
    {
        _app = AssetsTestApp.Build(_source);
    }

    private AssetManager Assets => _app.Assets();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void UnloadPathAndScope_ShouldDisposeWhenNoOwnerIsLeft()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        Assets.Unload("data/a.txt", AssetScope.Global);

        value.DisposeCount.Should().Be(1, "メインスレッドでの解放は、その場で行うはず");
        var reloaded = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        reloaded.Should().NotBeSameAs(value, "解放したものは読み直すはず");
        _factoryCalls.Should().Be(2);
    }

    [Fact]
    public void UnloadPathAndScope_ShouldTargetAllEntriesOfThePath()
    {
        var one = Assets.Load("data/a.txt", "one", Read, AssetScope.Global);
        var two = Assets.Load("data/a.txt", "two", Read, AssetScope.Global);
        var other = Assets.Load("data/b.txt", "one", Read, AssetScope.Global);

        Assets.Unload("data/a.txt", AssetScope.Global);

        one.IsDisposed.Should().BeTrue();
        two.IsDisposed.Should().BeTrue();
        other.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void UnloadPath_ShouldReleaseRegardlessOfOwners()
    {
        var one = Assets.Load("data/a.txt", "one", Read, AssetScope.Global);
        var two = Assets.Load("data/a.txt", "two", Read, AssetScope.Global);
        var other = Assets.Load("data/b.txt", "one", Read, AssetScope.Global);

        Assets.Unload("data/a.txt");

        one.DisposeCount.Should().Be(1);
        two.DisposeCount.Should().Be(1);
        other.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Unload_ShouldNormalizePath()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        Assets.Unload("./data/x/../a.txt", AssetScope.Global);

        value.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void UnloadScope_Global_ShouldReleaseAllGlobalAssets()
    {
        var a = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        var b = Assets.Load("data/b.txt", "k", Read, AssetScope.Global);

        Assets.Unload(AssetScope.Global);

        a.DisposeCount.Should().Be(1);
        b.DisposeCount.Should().Be(1);

        var again = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        again.IsDisposed.Should().BeFalse("スコープ自体は残り、続けて読めるはず");
    }

    [Fact]
    public void Unload_Twice_ShouldDisposeOnce()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        Assets.Unload("data/a.txt", AssetScope.Global);
        Assets.Unload("data/a.txt", AssetScope.Global);
        Assets.Unload("data/a.txt");
        Assets.Dispose();

        value.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Unload_UnknownPath_ShouldDoNothing()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        var act = () =>
        {
            Assets.Unload("data/missing.txt", AssetScope.Global);
            Assets.Unload("data/missing.txt");
        };

        act.Should().NotThrow();
        value.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Unload_InvalidArguments_ShouldThrow()
    {
        var backslash = () => Assets.Unload(@"data\a.txt", AssetScope.Global);
        var backslashAll = () => Assets.Unload(@"data\a.txt");
        var nullPath = () => Assets.Unload(null!, AssetScope.Global);
        var nullPathAll = () => Assets.Unload((string)null!);
        var undefinedScope = () => Assets.Unload((AssetScope)42);

        backslash.Should().Throw<ArgumentException>();
        backslashAll.Should().Throw<ArgumentException>();
        nullPath.Should().Throw<ArgumentNullException>();
        nullPathAll.Should().Throw<ArgumentNullException>();
        undefinedScope.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Unload_FromWorkerThread_ShouldDisposeOnNextFrame()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);

        AssetsTestApp.RunOnWorkerThread(() => Assets.Unload("data/a.txt", AssetScope.Global));

        value.IsDisposed.Should().BeFalse("リソースの Dispose はメインスレッドで行うはず");
        Assets
            .Load("data/a.txt", "k", Read, AssetScope.Global)
            .Should()
            .NotBeSameAs(value, "キャッシュからはすぐに外れるはず");

        _app.PumpFrame();

        value.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_ShouldDisposePendingDisposalsFromWorkerThreads()
    {
        var value = Assets.Load("data/a.txt", "k", Read, AssetScope.Global);
        AssetsTestApp.RunOnWorkerThread(() => Assets.Unload("data/a.txt"));

        Assets.Dispose();

        value
            .DisposeCount.Should()
            .Be(1, "次のフレームを待たずに破棄されても、解放の予約は実行されるはず");
    }

    [Fact]
    public void Unload_AfterDispose_ShouldThrowObjectDisposed()
    {
        Assets.Dispose();

        var withScope = () => Assets.Unload("data/a.txt", AssetScope.Global);
        var all = () => Assets.Unload("data/a.txt");
        var scope = () => Assets.Unload(AssetScope.Global);

        withScope.Should().Throw<ObjectDisposedException>();
        all.Should().Throw<ObjectDisposedException>();
        scope.Should().Throw<ObjectDisposedException>();
    }

    private TrackedDisposable Read(Stream stream)
    {
        Interlocked.Increment(ref _factoryCalls);
        return new TrackedDisposable(stream.ReadAllText());
    }
}
