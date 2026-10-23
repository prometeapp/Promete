using FluentAssertions;
using Promete.Assets;
using Promete.Graphics.Fonts;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetManager.LoadFont(string, float, AssetScope)"/> のテスト。グリフソースをパスごとに 1 つだけ読み、サイズ違いで共有すること。
/// </summary>
public sealed class AssetManagerFontTests
{
    private static readonly byte[] UiFont = TestAssetData.Text("ui-font-data");
    private static readonly byte[] TitleFont = TestAssetData.Text("title-font-data");

    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("fonts/ui.ttf", UiFont)
        .Add("fonts/title.ttf", TitleFont);

    [Fact]
    public void LoadFont_ShouldShareGlyphSourceAcrossSizesAndStyles()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var small = assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global);
        var bold = assets.LoadFont("fonts/ui.ttf", 32, FontStyle.Bold, AssetScope.Global);
        var aliased = assets.LoadFont(
            "fonts/ui.ttf",
            12,
            FontStyle.Italic,
            false,
            AssetScope.Global
        );

        bold.Source.Should().BeSameAs(small.Source);
        aliased.Source.Should().BeSameAs(small.Source);
        app.Fonts().FromStreamCount.Should().Be(1, "同じフォントのデータを複製しないはず");
        _source.OpenCount("fonts/ui.ttf").Should().Be(1);

        small.Size.Should().Be(16);
        small.Style.Should().Be(FontStyle.Normal);
        small.IsAntialiased.Should().BeTrue();
        bold.Size.Should().Be(32);
        bold.Style.Should().Be(FontStyle.Bold);
        aliased.Style.Should().Be(FontStyle.Italic);
        aliased.IsAntialiased.Should().BeFalse();
    }

    [Fact]
    public void LoadFont_ShouldReadWholeStreamAndCloseIt()
    {
        using var app = AssetsTestApp.Build(_source);

        var font = app.Assets().LoadFont("fonts/ui.ttf", 16, AssetScope.Global);

        ((FakeGlyphSource)font.Source).Data.Should().Equal(UiFont);
        _source
            .StreamsOf("fonts/ui.ttf")
            .Should()
            .ContainSingle()
            .Which.IsDisposed.Should()
            .BeTrue();
    }

    [Fact]
    public void LoadFont_SameArguments_ShouldBeEqualFonts()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var first = assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global);
        var second = assets.LoadFont(
            "./fonts/ui.ttf",
            16,
            FontStyle.Normal,
            true,
            AssetScope.Global
        );

        second.Should().Be(first);
    }

    [Fact]
    public void LoadFont_DifferentPaths_ShouldHaveDifferentSources()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();

        var ui = assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global);
        var title = assets.LoadFont("fonts/title.ttf", 16, AssetScope.Global);

        title.Source.Should().NotBeSameAs(ui.Source);
        ((FakeGlyphSource)title.Source).Data.Should().Equal(TitleFont);
    }

    [Fact]
    public void LoadFont_Release_ShouldDisposeGlyphSourceOnce()
    {
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        var source = (FakeGlyphSource)assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global).Source;
        assets.LoadFont("fonts/ui.ttf", 32, AssetScope.Global);

        assets.Unload("fonts/ui.ttf", AssetScope.Global);
        assets.Dispose();

        source.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void LoadFont_SceneScope_ShouldReleaseWithScene()
    {
        ScriptedScene.Script.Reset();
        using var app = AssetsTestApp.Build(_source);
        var assets = app.Assets();
        FakeGlyphSource? source = null;

        app.RunWith<ScriptedSceneA>(() =>
        {
            source = (FakeGlyphSource)assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Scene).Source;
            app.LoadScene<ScriptedEmptyScene>();
        });

        source!.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void LoadFont_StreamUnsupported_ShouldFallBackToFilePath()
    {
        // Web (CanvasFontProvider) の再現。ストリームからは読めず、実ファイルのパスから読む
        var fileBacked = new FileBackedInMemoryAssetSource(("fonts/ui.ttf", "ui"));
        using var app = AssetsTestApp.Build(fileBacked);
        app.Fonts().SupportsStream = false;
        var assets = app.Assets();

        var small = assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global);
        var large = assets.LoadFont("fonts/ui.ttf", 32, AssetScope.Global);

        app.Fonts().FromFilePaths.Should().Equal("/fake/fonts/ui.ttf");
        large.Source.Should().BeSameAs(small.Source);
        large.Size.Should().Be(32);
    }

    [Fact]
    public void LoadFont_StreamUnsupported_ShouldNotDisposeProviderCachedSource()
    {
        var fileBacked = new FileBackedInMemoryAssetSource(("fonts/ui.ttf", "ui"));
        using var app = AssetsTestApp.Build(fileBacked);
        app.Fonts().SupportsStream = false;
        var assets = app.Assets();
        var source = (FakeGlyphSource)assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global).Source;

        assets.Unload("fonts/ui.ttf");
        assets.Dispose();

        source
            .IsDisposed.Should()
            .BeFalse(
                "provider がキャッシュしているグリフソースは、スコープの解放の対象にしないはず"
            );
    }

    [Fact]
    public void LoadFont_StreamUnsupportedWithoutFilePath_ShouldThrowNotSupportedAndNotCache()
    {
        using var app = AssetsTestApp.Build(_source);
        app.Fonts().SupportsStream = false;
        var assets = app.Assets();

        var act = () => assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global);

        act.Should().Throw<NotSupportedException>().WithMessage("*fonts/ui.ttf*");
        _source.StreamsOf("fonts/ui.ttf").Should().AllSatisfy(s => s.IsDisposed.Should().BeTrue());

        app.Fonts().SupportsStream = true;
        assets.LoadFont("fonts/ui.ttf", 16, AssetScope.Global).Should().NotBeNull();
    }

    [Fact]
    public void LoadFont_MissingFile_ShouldThrowFileNotFound()
    {
        using var app = AssetsTestApp.Build(_source);

        var act = () => app.Assets().LoadFont("fonts/missing.ttf", 16, AssetScope.Global);

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void LoadFont_OnRealHeadlessBackend_ShouldUseFreeType()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(
                typeof(AssetManagerFontTests).Assembly,
                o => o.MountFolder("assets", "assets")
            )
            .BuildWithHeadless();
        var assets = app.GetPlugin<AssetManager>();

        var small = assets.LoadFont("assets/Koruri.ttf", 16, AssetScope.Global);
        var large = assets.LoadFont("assets/Koruri.ttf", 32, AssetScope.Global);

        large.Source.Should().BeSameAs(small.Source);
        small.Metrics.LineHeight.Should().BeGreaterThan(0);

        assets.Unload("assets/Koruri.ttf");

        var act = () => small.Source.GetMetrics(small.RenderOptions);
        act.Should().Throw<ObjectDisposedException>("解放でグリフソースが破棄されるはず");
    }
}
