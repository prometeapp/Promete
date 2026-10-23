using System.Reflection;
using FluentAssertions;
using Promete.Assets;
using Promete.Assets.Infrastructure;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <c>UseAssets</c> が、アセンブリの <see cref="PrometeAssetSourceAttribute"/> を <see cref="IAssetManifestLoader"/> で読んでマウントすることのテスト。
/// </summary>
public sealed class UseAssetsAttributeMountTests
{
    private static readonly Assembly TestAssembly = typeof(UseAssetsAttributeMountTests).Assembly;

    [Fact]
    public void Attribute_ShouldBeMountedThroughRegisteredLoader()
    {
        var attribute = Manifest("Game.promete-assets.json");
        var assembly = new FakeAttributeAssembly("Game", attribute);
        var loader = new RecordingLoader(new InMemoryAssetSource(("assets/a.txt", "from loader")));

        using var app = PrometeApp
            .Create()
            .UseAssets(assembly)
            .Use<IAssetManifestLoader>(loader)
            .BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        loader.Calls.Should().ContainSingle();
        loader.Calls[0].Assembly.Should().BeSameAs(assembly);
        loader.Calls[0].Attribute.Should().BeSameAs(attribute);
        fs.Sources.Should().ContainSingle().Which.Should().BeSameAs(loader.Sources[0]);
        new StreamReader(fs.OpenRead("assets/a.txt")).ReadToEnd().Should().Be("from loader");
    }

    [Fact]
    public void Attribute_ShouldBeMountedBelowExplicitMounts()
    {
        var assembly = new FakeAttributeAssembly("Game", Manifest("Game.promete-assets.json"));
        var loader = new RecordingLoader(
            new InMemoryAssetSource(("a.txt", "attribute"), ("only-attribute.txt", "attribute"))
        );
        var explicitSource = new InMemoryAssetSource(("a.txt", "explicit"));

        // 明示的なマウントは、優先度が int.MinValue でも、属性によるマウントより上に重なる
        using var app = PrometeApp
            .Create()
            .UseAssets(assembly, o => o.Mount(explicitSource, int.MinValue))
            .Use<IAssetManifestLoader>(loader)
            .BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().Equal(explicitSource, loader.Sources[0]);
        new StreamReader(fs.OpenRead("a.txt")).ReadToEnd().Should().Be("explicit");
        new StreamReader(fs.OpenRead("only-attribute.txt")).ReadToEnd().Should().Be("attribute");
    }

    [Fact]
    public void MultipleAttributes_ShouldAllBeMounted()
    {
        var main = Package("Game.main.prmtpak", "main");
        var extra = Package("Game.extra.prmtpak", "extra");
        var assembly = new FakeAttributeAssembly("Game", main, extra);
        var loader = new RecordingLoader(
            new InMemoryAssetSource(("main.txt", "main")),
            new InMemoryAssetSource(("extra.txt", "extra"))
        );

        using var app = PrometeApp
            .Create()
            .UseAssets(assembly)
            .Use<IAssetManifestLoader>(loader)
            .BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        loader.Calls.Select(c => c.Attribute).Should().BeEquivalentTo([main, extra]);
        fs.Exists("main.txt").Should().BeTrue();
        fs.Exists("extra.txt").Should().BeTrue();
    }

    [Fact]
    public void LoaderRegisteredBeforeUseAssets_ShouldAlsoReplaceTheDefault()
    {
        var assembly = new FakeAttributeAssembly("Game", Manifest("Game.promete-assets.json"));
        var loader = new RecordingLoader(new InMemoryAssetSource(("a.txt", "x")));

        using var app = PrometeApp
            .Create()
            .Use<IAssetManifestLoader>(loader)
            .UseAssets(assembly)
            .BuildWithHeadless();

        app.GetPlugin<AssetManager>().FileSystem.Exists("a.txt").Should().BeTrue();
        loader.Calls.Should().ContainSingle();
    }

    [Fact]
    public void WithoutCustomLoader_ShouldUseDesktopDefault()
    {
        // 既定の実装は AppContext.BaseDirectory からマニフェストを探す (テストの出力先には無い)
        var assembly = new FakeAttributeAssembly(
            "Game",
            Manifest("NoSuchGame.promete-assets.json")
        );

        using var app = PrometeApp.Create().UseAssets(assembly).BuildWithHeadless();
        var act = () => app.GetPlugin<AssetManager>();

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("NoSuchGame.promete-assets.json"));
    }

    [Fact]
    public void EntryAssemblyWithAttribute_ShouldBeMounted()
    {
        var assembly = new FakeAttributeAssembly("Game", Manifest("Game.promete-assets.json"));
        var loader = new RecordingLoader(new InMemoryAssetSource(("a.txt", "x")));

        using var app = PrometeApp
            .Create()
            .UseAssetsWithEntryAssembly(() => assembly, null)
            .Use<IAssetManifestLoader>(loader)
            .BuildWithHeadless();

        app.GetPlugin<AssetManager>().FileSystem.Exists("a.txt").Should().BeTrue();
        loader.Calls.Single().Assembly.Should().BeSameAs(assembly);
    }

    [Fact]
    public void LoaderFailure_ShouldPropagateAndDisposeAlreadyMountedSources()
    {
        var first = new InMemoryAssetSource(("a.txt", "x"));
        var assembly = new FakeAttributeAssembly(
            "Game",
            Package("Game.a.prmtpak", "a"),
            Package("Game.b.prmtpak", "b")
        );
        var loader = new RecordingLoader(first) { ThrowAfter = 1 };

        using var app = PrometeApp
            .Create()
            .UseAssets(assembly)
            .Use<IAssetManifestLoader>(loader)
            .BuildWithHeadless();
        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<FileNotFoundException>().WithMessage("*loader failure*");
        first.DisposeCount.Should().Be(1, "マウント済みのソースは巻き戻しで破棄されるはず");
    }

    [Fact]
    public void NoAttributeAndNoExplicitMount_OpenRead_ShouldExplainMissingAttribute()
    {
        using var app = PrometeApp.Create().UseAssets(TestAssembly).BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        var act = () => fs.OpenRead("assets/a.txt");

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("'Promete.Test'"), "属性を探したアセンブリ名を含むはず")
            .Where(e => e.Message.Contains("PrometeAsset"), "csproj での宣言を案内するはず")
            .Where(
                e => e.Message.Contains("UseAssets(typeof("),
                "別のアセンブリの指定を案内するはず"
            )
            .Where(
                e => e.Message.Contains("MountFolder"),
                "古い Promete.Web での回避策を案内するはず"
            );
    }

    [Fact]
    public void NoAttributeButExplicitMount_ShouldNotTalkAboutAttributes()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.Mount(new InMemoryAssetSource()))
            .BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        var act = () => fs.OpenRead("assets/a.txt");

        act.Should().Throw<FileNotFoundException>().Where(e => !e.Message.Contains("PrometeAsset"));
    }

    [Fact]
    public void Attribute_ShouldBeMountedWhenAssetManagerIsCreated()
    {
        // 属性によるマウントも、明示的なマウントと同じく AssetManager のコンストラクタで行う
        var assembly = new FakeAttributeAssembly("Game", Manifest("Game.promete-assets.json"));
        var loader = new RecordingLoader(new InMemoryAssetSource(("a.txt", "x")));

        using var app = PrometeApp
            .Create()
            .Use<IAssetManifestLoader>(loader)
            .UseAssets(assembly)
            .BuildWithHeadless();

        loader.Calls.Should().BeEmpty("AssetManager が作られるまではマウントしない");
        app.GetPlugin<AssetManager>();
        loader.Calls.Should().ContainSingle();
    }

    private static PrometeAssetSourceAttribute Manifest(string location) =>
        new(PrometeAssetSourceAttribute.KindManifest, location);

    private static PrometeAssetSourceAttribute Package(string location, string package) =>
        new(PrometeAssetSourceAttribute.KindPackage, location) { Package = package };

    /// <summary>
    /// 呼び出しを記録し、用意したソースを順に返す <see cref="IAssetManifestLoader"/>。
    /// </summary>
    private sealed class RecordingLoader(params IAssetSource[] sources) : IAssetManifestLoader
    {
        public List<(Assembly Assembly, PrometeAssetSourceAttribute Attribute)> Calls { get; } = [];

        public IAssetSource[] Sources { get; } = sources;

        /// <summary>この回数だけ成功した後は、例外をスローする。</summary>
        public int ThrowAfter { get; init; } = int.MaxValue;

        public IAssetSource CreateSource(Assembly assembly, PrometeAssetSourceAttribute attribute)
        {
            if (Calls.Count >= ThrowAfter)
                throw new FileNotFoundException("loader failure");

            Calls.Add((assembly, attribute));
            return Sources[Calls.Count - 1];
        }
    }
}
