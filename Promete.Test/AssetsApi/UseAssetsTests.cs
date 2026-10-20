using FluentAssertions;
using Promete.Assets;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <c>UseAssets</c> による DI 登録と、<see cref="AssetManager"/> のマウントのテスト。
/// </summary>
public sealed class UseAssetsTests : IDisposable
{
    private static readonly System.Reflection.Assembly TestAssembly =
        typeof(UseAssetsTests).Assembly;

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void UseAssets_InTestHost_ShouldNotThrowAndMountNothing()
    {
        // テストホストではエントリアセンブリが取れる (testhost) が、Assets の属性は無い
        using var app = PrometeApp.Create().UseAssets().BuildWithHeadless();

        var assets = app.GetPlugin<AssetManager>();

        assets.FileSystem.Sources.Should().BeEmpty();
    }

    [Fact]
    public void UseAssets_WithoutEntryAssembly_ShouldThrowAndSuggestAssemblyOverload()
    {
        var act = () => PrometeApp.Create().UseAssetsWithEntryAssembly(() => null, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*UseAssets(Assembly)*");
    }

    [Fact]
    public void UseAssets_WithoutEntryAssembly_ShouldThrowEvenWithConfigure()
    {
        var act = () =>
            PrometeApp
                .Create()
                .UseAssetsWithEntryAssembly(() => null, o => o.MountFolder(_dir.Path));

        act.Should()
            .Throw<InvalidOperationException>("WASM では configure の有無に関係なく例外にするはず");
    }

    [Fact]
    public void UseAssets_NullAssembly_ShouldThrowArgumentNull()
    {
        var act = () => PrometeApp.Create().UseAssets((System.Reflection.Assembly)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseAssets_Twice_ShouldThrow()
    {
        var act = () => PrometeApp.Create().UseAssets(TestAssembly).UseAssets(TestAssembly);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UseAssets_WithAssembly_WithoutAttribute_ShouldMountOnlyExplicitSources()
    {
        _dir.CreateFile("x.png", "x");
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountFolder(_dir.Path, "assets"))
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().ContainSingle().Which.Should().BeOfType<FolderAssetSource>();
        fs.Exists("assets/x.png").Should().BeTrue();
    }

    [Fact]
    public void MountFolder_RelativePath_ShouldResolveFromAppContextBaseDirectory()
    {
        // Promete.Test/assets は出力先にコピーされている。カレントディレクトリに依存しないこと
        var originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _dir.Path;
            using var app = PrometeApp
                .Create()
                .UseAssets(TestAssembly, o => o.MountFolder("assets", "assets"))
                .BuildWithHeadless();

            var fs = app.GetPlugin<AssetManager>().FileSystem;

            fs.Exists("assets/Koruri.ttf").Should().BeTrue();
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void MountFolder_Nonexistent_ShouldThrowDirectoryNotFoundWhenMounted()
    {
        var missing = Path.Combine(_dir.Path, "missing");
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountFolder(missing))
            .BuildWithHeadless();

        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void AssetManager_ShouldBeCreatedOnStart()
    {
        // 誰も AssetManager を要求しなくても、OnStart でインスタンスが作られてマウントされる
        var missing = Path.Combine(_dir.Path, "missing");
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountFolder(missing))
            .BuildWithHeadless();

        var act = () => app.Run();

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void ExplicitMounts_ShouldBeAppliedInOrderWithPriority()
    {
        var first = new InMemoryAssetSource(("a.txt", "first"), ("b.txt", "first"));
        var second = new InMemoryAssetSource(("a.txt", "second"));
        var low = new InMemoryAssetSource(("b.txt", "low"));
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.Mount(first).Mount(second).Mount(low, -1))
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().Equal(second, first, low);
        using (var stream = fs.OpenRead("a.txt"))
            stream.ReadAllText().Should().Be("second", "同じ優先度では後に指定したものが勝つはず");
        using (var stream = fs.OpenRead("b.txt"))
            stream.ReadAllText().Should().Be("first");
    }

    [Fact]
    public void MountEmbeddedResources_ShouldMountEmbeddedResourceSource()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(
                TestAssembly,
                o => o.MountEmbeddedResources(TestAssembly, "embedded/", "res")
            )
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().ContainSingle().Which.Should().BeOfType<EmbeddedResourceAssetSource>();
        using var stream = fs.OpenRead("res/hello.txt");
        stream.ReadAllText().Should().Be("hello");
    }

    [Fact]
    public void Options_NullArguments_ShouldThrowArgumentNull()
    {
        var actions = new Action<AssetsOptions>[]
        {
            o => o.MountFolder(null!),
            o => o.MountFolder(_dir.Path, null!),
            o => o.MountEmbeddedResources(null!),
            o => o.Mount(null!),
        };

        foreach (var configure in actions)
        {
            // 構成時 (UseAssets の呼び出し時) にすぐ検証するはず。Build や GetPlugin は呼ばない
            var act = () => PrometeApp.Create().UseAssets(TestAssembly, configure);
            act.Should().Throw<ArgumentNullException>();
        }
    }

    [Fact]
    public void PluginRegisteredEarlier_ShouldReadAssetsInItsOnStart()
    {
        // マウントは AssetManager のコンストラクタで行うので、先に登録したプラグインの OnStart でも読める
        var source = new InMemoryAssetSource(("a.txt", "ready"));
        using var app = PrometeApp
            .Create()
            .Use<EarlyReaderPlugin>()
            .UseAssets(TestAssembly, o => o.Mount(source))
            .BuildWithHeadless();
        app.Start += () => app.Exit();

        app.Run();

        app.GetPlugin<EarlyReaderPlugin>().ReadContent.Should().Be("ready");
    }

    [Fact]
    public void AppDispose_ShouldDisposeMountedSourcesOnce()
    {
        var source = new InMemoryAssetSource();
        var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.Mount(source))
            .BuildWithHeadless();
        app.Start += () => app.Exit();
        app.Run();

        app.Dispose();

        source
            .DisposeCount.Should()
            .Be(1, "プラグインとしての破棄と DI コンテナの破棄の両方で呼ばれても、1 回だけのはず");
    }

    [Fact]
    public void AssetManagerDispose_ShouldBeIdempotent()
    {
        var source = new InMemoryAssetSource();
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.Mount(source))
            .BuildWithHeadless();
        var assets = app.GetPlugin<AssetManager>();

        assets.Dispose();
        assets.Dispose();

        source.DisposeCount.Should().Be(1);
        var act = () => assets.FileSystem.Sources;
        act.Should()
            .Throw<ObjectDisposedException>(
                "AssetManager の Dispose で FileSystem も破棄されるはず"
            );
    }

    [Fact]
    public void AssetManager_ShouldBeSingleton()
    {
        using var app = PrometeApp.Create().UseAssets(TestAssembly).BuildWithHeadless();

        app.GetPlugin<AssetManager>().Should().BeSameAs(app.GetPlugin<AssetManager>());
    }

    /// <summary>
    /// AssetManager より先に登録され、自分の OnStart でアセットを読むプラグイン。
    /// </summary>
    private sealed class EarlyReaderPlugin(AssetManager assets) : IInitializable
    {
        public string? ReadContent { get; private set; }

        public void OnStart()
        {
            using var stream = assets.FileSystem.OpenRead("a.txt");
            ReadContent = stream.ReadAllText();
        }
    }

    [Fact]
    public void Options_InvalidMountPoint_ShouldThrowWhenConfigured()
    {
        var actions = new Action<AssetsOptions>[]
        {
            o => o.MountFolder(_dir.Path, "a\\b"),
            o => o.MountEmbeddedResources(TestAssembly, "", "a\\b"),
            o => o.MountEmbeddedResources(TestAssembly, "embedded\\", "res"),
        };

        foreach (var configure in actions)
        {
            // AssetManager を作る時点まで遅らせず、UseAssets の呼び出し時に ArgumentException になるはず
            var act = () => PrometeApp.Create().UseAssets(TestAssembly, configure);
            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void MountFailure_ShouldDisposeAlreadyMountedSources()
    {
        var source = new InMemoryAssetSource(("a.txt", "a"));
        var missing = Path.Combine(_dir.Path, "missing");
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.Mount(source).MountFolder(missing))
            .BuildWithHeadless();

        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<DirectoryNotFoundException>();
        source
            .DisposeCount.Should()
            .Be(1, "途中で失敗したら、先にマウントしたソースを破棄して巻き戻すはず");
    }

    [Fact]
    public void UseAssets_WithEntryAssemblyAndConfigure_ShouldMountExplicitSources()
    {
        var source = new InMemoryAssetSource(("a.txt", "a"));
        using var app = PrometeApp.Create().UseAssets(o => o.Mount(source)).BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().Equal(source);
        fs.Exists("a.txt").Should().BeTrue();
    }
}
