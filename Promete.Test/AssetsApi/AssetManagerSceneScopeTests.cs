using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetScope.Scene"/> が、どのシーンを指すか (帰属) と、指定できない場面のテスト。
/// </summary>
public sealed class AssetManagerSceneScopeTests
{
    private readonly TrackingAssetSource _source = new();

    public AssetManagerSceneScopeTests()
    {
        ScriptedScene.Script.Reset();
        byte seed = 0;
        foreach (var name in new[] { "a.png", "b.png", "c.png", "field.png", "lazy.png" })
            _source.Add(name, TestAssetData.Png(seed: seed++));
    }

    [Fact]
    public void SceneScope_WithoutScene_ShouldThrow()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var texture = () => assets.LoadTexture("a.png", AssetScope.Scene);
        var generic = () => assets.Load("a.png", "k", s => s.Length, AssetScope.Scene);
        var unloadPath = () => assets.Unload("a.png", AssetScope.Scene);
        var unloadScope = () => assets.Unload(AssetScope.Scene);

        texture.Should().Throw<InvalidOperationException>();
        generic.Should().Throw<InvalidOperationException>();
        unloadPath.Should().Throw<InvalidOperationException>();
        unloadScope.Should().Throw<InvalidOperationException>();
        _source.OpenCount("a.png").Should().Be(0, "スコープの誤りは読み込む前に検出するはず");
    }

    [Fact]
    public void SceneScope_InPluginOnStart_ShouldThrow()
    {
        SceneScopeLoadingPlugin.Thrown = null;
        using var app = AssetsTestApp.Build(_source, b => b.Use<SceneScopeLoadingPlugin>());

        app.RunWith<ScriptedEmptyScene>(() => { });

        SceneScopeLoadingPlugin
            .Thrown.Should()
            .BeOfType<InvalidOperationException>("プラグインの OnStart の時点ではシーンが無いはず");
    }

    [Fact]
    public void SceneScope_FromWorkerThread_ShouldThrow()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        Exception? load = null;
        Exception? generic = null;
        Exception? unloadPath = null;
        Exception? unloadScope = null;

        app.RunWith<ScriptedEmptyScene>(() =>
        {
            AssetsTestApp.RunOnWorkerThread(() =>
            {
                load = Catch(() => assets.LoadTexture("a.png", AssetScope.Scene));
                generic = Catch(() => assets.Load("a.png", "k", s => s.Length, AssetScope.Scene));
                unloadPath = Catch(() => assets.Unload("a.png", AssetScope.Scene));
                unloadScope = Catch(() => assets.Unload(AssetScope.Scene));
            });
        });

        load.Should().BeOfType<InvalidOperationException>();
        generic.Should().BeOfType<InvalidOperationException>();
        unloadPath.Should().BeOfType<InvalidOperationException>();
        unloadScope.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void GlobalScope_FromWorkerThreadDuringScene_ShouldWork()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var handle = 0;

        app.RunWith<ScriptedEmptyScene>(() =>
            handle = AssetsTestApp.RunOnWorkerThread(() =>
                assets.LoadTexture("a.png", AssetScope.Global).Handle
            )
        );

        handle.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SceneScope_InOnStart_ShouldBelongToThatScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var handle = ScriptedScene.Script.Handles["a.png"];
            app.Textures().DisposedHandlesSnapshot().Should().NotContain(handle);

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void SceneScope_InStartEventHandler_ShouldBelongToCurrentScene()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        app.RunWith<ScriptedSceneA>(() =>
        {
            var handle = assets.LoadTexture("a.png", AssetScope.Scene).Handle;

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void SceneScope_InConstructor_ShouldBelongToConstructingScene()
    {
        ScriptedScene.Script.CtorLoads[typeof(ScriptedSceneB)] = ["b.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.LoadScene<ScriptedSceneB>();
            var handle = ScriptedScene.Script.Handles["b.png"];
            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .NotContain(handle, "前のシーンではなく、生成中のシーンに帰属するはず");

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void SceneScope_InFieldInitializer_ShouldBelongToConstructingScene()
    {
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.LoadScene<FieldInitializerScene>();
            var handle = ScriptedScene.Script.Handles["field.png"];
            app.Textures().DisposedHandlesSnapshot().Should().NotContain(handle);

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void SceneScope_InInitialSceneConstructor_ShouldBelongToInitialScene()
    {
        ScriptedScene.Script.CtorLoads[typeof(ScriptedSceneA)] = ["a.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            var handle = ScriptedScene.Script.Handles["a.png"];
            app.Textures().DisposedHandlesSnapshot().Should().NotContain(handle);

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void SceneScope_InPushedSceneConstructor_ShouldBelongToPushedScene()
    {
        ScriptedScene.Script.StartLoads[typeof(ScriptedSceneA)] = ["a.png"];
        ScriptedScene.Script.CtorLoads[typeof(ScriptedSceneB)] = ["b.png"];
        using var app = AssetsTestApp.Build(_source);

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.PushScene<ScriptedSceneB>();
            var lower = ScriptedScene.Script.Handles["a.png"];
            var upper = ScriptedScene.Script.Handles["b.png"];

            app.PopScene();

            app.Textures()
                .DisposedHandlesSnapshot()
                .Should()
                .Contain(upper, "上のシーンの生成中に読んだものは、上のシーンに帰属するはず")
                .And.NotContain(lower);
        });
    }

    [Fact]
    public void SceneScope_InSingletonCreatedWhileResolvingScene_ShouldBelongToConstructingScene()
    {
        using var app = AssetsTestApp.Build(_source, b => b.Use<LazyLoadingService>());

        app.RunWith<ScriptedSceneA>(() =>
        {
            app.LoadScene<SceneWithLazyService>();
            var handle = ScriptedScene.Script.Handles["lazy.png"];
            app.Textures().DisposedHandlesSnapshot().Should().NotContain(handle);

            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().Contain(handle);
        });
    }

    [Fact]
    public void GlobalScope_InSceneConstructor_ShouldSurviveTransitions()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        app.RunWith<ScriptedSceneA>(() =>
        {
            var handle = assets.LoadTexture("c.png", AssetScope.Global).Handle;

            app.LoadScene<ScriptedSceneB>();
            app.PushScene<ScriptedSceneC>();
            app.PopScene();
            app.LoadScene<ScriptedEmptyScene>();

            app.Textures().DisposedHandlesSnapshot().Should().NotContain(handle);
        });
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
    /// OnStart で <see cref="AssetScope.Scene"/> を指定して読み、投げられた例外を記録するプラグイン。
    /// </summary>
    internal sealed class SceneScopeLoadingPlugin(AssetManager assets) : IInitializable
    {
        public static Exception? Thrown { get; set; }

        public void OnStart() =>
            Thrown = Catch(() => assets.LoadTexture("a.png", AssetScope.Scene));
    }

    /// <summary>
    /// シーンの解決の途中で初めて生成されるシングルトン。コンストラクタで <c>lazy.png</c> を <see cref="AssetScope.Scene"/> で読む。
    /// </summary>
    internal sealed class LazyLoadingService
    {
        public LazyLoadingService(AssetManager assets)
        {
            ScriptedScene.Script.Handles["lazy.png"] = assets
                .LoadTexture("lazy.png", AssetScope.Scene)
                .Handle;
        }
    }

    internal sealed class SceneWithLazyService(LazyLoadingService service) : Scene
    {
        public LazyLoadingService Service { get; } = service;
    }
}
