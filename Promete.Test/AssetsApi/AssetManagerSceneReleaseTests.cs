using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// シーンのスコープが、遷移の各経路でいつ解放されるか (所有するスコープの集合を含む) のテスト。
/// </summary>
public sealed class AssetManagerSceneReleaseTests
{
    private readonly TrackingAssetSource _source = new();

    public AssetManagerSceneReleaseTests()
    {
        ScriptedScene.Script.Reset();
        byte seed = 0;
        foreach (var name in new[] { "a.png", "b.png", "c.png", "d.png", "n.png" })
            _source.Add(name, TestAssetData.Png(seed: seed++));
    }

    private static Dictionary<string, int> Handles => ScriptedScene.Script.Handles;

    private static List<string> UsableAtDestroy => ScriptedScene.Script.UsableAtDestroy;

    [Fact]
    public void LoadScene_ShouldReleasePreviousSceneAfterOnDestroy()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.LoadScene<ScriptedSceneB>();

            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["a.png"]);
        });

        UsableAtDestroy.Should().Equal("ScriptedSceneA:a.png=True");
    }

    [Fact]
    public void LoadScene_NextSceneLoadingSameAsset_ShouldNotReload()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedSceneB)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneC)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var first = Handles["a.png"];

            app.LoadScene<ScriptedSceneB>();
            app.LoadScene<ScriptedSceneC>();

            Handles["a.png"].Should().Be(first);
            app.Textures()
                .CreatedCount.Should()
                .Be(1, "解放は新しいシーンの OnStart の後なので、読み直さないはず");
            app.Textures().DisposedHandlesSnapshot().Should().BeEmpty();
            _source.OpenCount("a.png").Should().Be(1);
        });
    }

    [Fact]
    public void LoadScene_SameSceneType_ShouldNotReload()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.LoadScene<ScriptedSceneA>();

            app.Textures().CreatedCount.Should().Be(1);
            app.Textures().DisposedHandlesSnapshot().Should().BeEmpty();

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["a.png"]);
        });
    }

    [Fact]
    public void PushScene_ShouldNotReleaseLowerScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();

            app.Textures().DisposedHandlesSnapshot().Should().BeEmpty();
        });
    }

    [Fact]
    public void PopScene_ShouldReleaseOnlyTopScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["b.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();
            app.PopScene();

            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["b.png"]);
        });

        UsableAtDestroy.Should().Equal("ScriptedSceneB:b.png=True");
    }

    [Fact]
    public void PopScene_UpperAndLowerShareKey_ShouldKeepLowerScenesAsset()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var lower = Handles["a.png"];
            app.PushScene<ScriptedSceneB>();
            Handles["a.png"].Should().Be(lower, "同じキーなので共有するはず");

            app.PopScene();

            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEmpty("下のシーンがまだ持っているはず");

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Equal(lower);
        });
    }

    [Fact]
    public void ClearSceneStack_ShouldReleaseStackedScenesButNotCurrent()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["b.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneC)] = ["c.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();
            app.PushScene<ScriptedSceneC>();

            app.ClearSceneStack();

            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEquivalentTo([Handles["a.png"], Handles["b.png"]]);
        });

        UsableAtDestroy.Should().Equal("ScriptedSceneB:b.png=True", "ScriptedSceneA:a.png=True");
    }

    [Fact]
    public void ClearSceneStack_SharedWithCurrent_ShouldKeepAsset()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();

            app.ClearSceneStack();

            app.Textures().DisposedHandlesSnapshot().Should().BeEmpty();
        });
    }

    [Fact]
    public void AppDestroy_ShouldReleaseAllScenesAndGlobalAssets()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["b.png"];
        // アプリの終了 (バックエンドが呼ぶ OnDestroy)。OnDestroy の中で Dispose されるので using しない
        var app = AssetsTestApp.Build(_source);
        var textures = app.Textures();
        var global = 0;

        app.RunWith<ScriptedSceneA>(() =>
        {
            global = app.Assets().LoadTexture("c.png", AssetScope.Global).Handle;
            app.PushScene<ScriptedSceneB>();
        });
        textures.DisposedHandlesSnapshot().Should().BeEmpty();

        app.OnDestroy();

        textures
            .DisposedHandlesSnapshot()
            .Should()
            .BeEquivalentTo([Handles["a.png"], Handles["b.png"], global]);
        textures.DisposedHandlesSnapshot().Should().OnlyHaveUniqueItems();
        UsableAtDestroy.Should().Equal("ScriptedSceneB:b.png=True", "ScriptedSceneA:a.png=True");
    }

    [Fact]
    public void LoadScene_ConstructorThrows_ShouldReleaseConstructingScopeAndPreviousImmediately()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedThrowingCtorScene)] = ["b.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var act = () => app.LoadScene<ScriptedThrowingCtorScene>();

            act.Should().Throw<InvalidOperationException>();
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEquivalentTo([Handles["a.png"], Handles["b.png"]]);
        });

        UsableAtDestroy.Should().Equal("ScriptedSceneA:a.png=True");
    }

    [Fact]
    public void PushScene_ConstructorThrows_ShouldReleaseOnlyConstructingScope()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedThrowingCtorScene)] = ["b.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var act = () => app.PushScene<ScriptedThrowingCtorScene>();

            act.Should().Throw<InvalidOperationException>();
            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["b.png"]);
        });
    }

    [Fact]
    public void ConstructorThrows_AfterLoadingAssetOwnedByCurrentScene_ShouldKeepIt()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedThrowingCtorScene)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var act = () => app.PushScene<ScriptedThrowingCtorScene>();

            act.Should().Throw<InvalidOperationException>();
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEmpty("下のシーンも所有しているので残るはず");
        });
    }

    [Fact]
    public void LoadScene_OnStartThrows_ShouldStillReleasePreviousScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var act = () => app.LoadScene<ScriptedThrowingOnStartScene>();

            act.Should().Throw<InvalidOperationException>();
            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["a.png"]);
        });
    }

    [Fact]
    public void NestedLoadInOnStart_ShouldReleaseEachSceneAtTheEndOfItsTransition()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedNestingScene)] = ["n.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneC)] = ["a.png", "c.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var a = Handles["a.png"];

            // Nesting の OnStart で C へ LoadScene し、その後に d.png を Scene で読む
            app.LoadScene<ScriptedNestingScene>();

            Handles["a.png"].Should().Be(a, "C が引き継ぐので読み直さないはず");
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .Equal([Handles["n.png"]], "Nesting の分だけが解放され、A の分は C が持つはず");

            // d.png は、内側の遷移の後の「今のシーン」である C に帰属する
            app.LoadScene<ScriptedEmptyScene>();

            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEquivalentTo([Handles["n.png"], a, Handles["c.png"], Handles["d.png"]]);
        });
    }

    [Fact]
    public void GlobalAndSceneOwningSameKey_ShouldReleaseWhenBothAreGone()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        app.RunWith<ScriptedSceneA>(() =>
        {
            var handle = assets.LoadTexture("a.png", AssetScope.Global).Handle;
            handle.Should().Be(Handles["a.png"]);

            app.LoadScene<ScriptedEmptyScene>();
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEmpty("Global がまだ持っているはず");

            assets.Unload("a.png", AssetScope.Global);

            app.Textures().DisposedHandlesSnapshot().Should().Equal(handle);
        });
    }

    [Fact]
    public void UnloadSceneScope_ShouldReleaseCurrentScenesAssetsAndKeepScopeUsable()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        app.RunWith<ScriptedSceneA>(() =>
        {
            var a = Handles["a.png"];

            assets.Unload(AssetScope.Scene);

            app.Textures().DisposedHandlesSnapshot().Should().Equal(a);

            var b = assets.LoadTexture("b.png", AssetScope.Scene).Handle;
            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Equal(a, b);
        });
    }

    [Fact]
    public void UnloadPathWithSceneScope_ShouldRemoveOnlyCurrentScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneB)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();

            assets.Unload("a.png", AssetScope.Scene);
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .BeEmpty("下のシーンがまだ持っているはず");

            app.PopScene();
            app.Textures().DisposedHandlesSnapshot().Should().BeEmpty();

            app.LoadScene<ScriptedEmptyScene>();
            app.Textures().DisposedHandlesSnapshot().Should().Equal(Handles["a.png"]);
        });
    }

    [Fact]
    public void Release_ShouldHappenOnMainThread()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() => app.LoadScene<ScriptedEmptyScene>());

        app.Textures().DisposedHandlesSnapshot().Should().ContainSingle();
        app.Textures().OffMainThreadDestroyCount.Should().Be(0);
    }
}
