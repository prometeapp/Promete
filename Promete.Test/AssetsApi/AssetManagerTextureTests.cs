using FluentAssertions;
using Promete.Assets;
using Promete.Graphics;

namespace Promete.Test.AssetsApi;

/// <summary>
/// テクスチャ系の同期の読み込み (<see cref="AssetManager.LoadTexture(string, AssetScope)"/>、
/// <see cref="AssetManager.LoadSpriteSheet(string, int, int, VectorInt, AssetScope)"/>、
/// <see cref="AssetManager.Load9Sliced(string, int, int, int, int, AssetScope)"/>) のテスト。
/// </summary>
public sealed class AssetManagerTextureTests : IDisposable
{
    private static readonly TextureOptions Linear = new(TextureFilterMode.Linear);

    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("img/a.png", TestAssetData.Png(4, 4, seed: 1))
        .Add("img/b.png", TestAssetData.Png(4, 4, seed: 2))
        .Add("img/tiles.png", TestAssetData.Png(8, 4, seed: 3))
        .Add("img/frame.png", TestAssetData.Png(9, 9, seed: 4))
        .Add("img/broken.png", TestAssetData.Text("not a png"));

    private readonly PrometeApp _app;

    public AssetManagerTextureTests()
    {
        _app = AssetsTestApp.Build(_source);
    }

    private AssetManager Assets => _app.Assets();

    private AssetsTestTextureFactory Factory => _app.Textures();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void LoadTexture_ShouldDecodeImage()
    {
        var texture = Assets.LoadTexture("img/a.png", AssetScope.Global);

        texture.Size.Should().Be(new VectorInt(4, 4));
        Factory.CreatedCount.Should().Be(1);
        Factory.GetOptions(texture.Handle).Should().Be(TextureOptions.Default);
    }

    [Fact]
    public void LoadTexture_WithOptions_ShouldPassOptions()
    {
        var texture = Assets.LoadTexture("img/a.png", Linear, AssetScope.Global);

        Factory.GetOptions(texture.Handle).Should().Be(Linear);
    }

    [Fact]
    public void LoadTexture_SameKey_ShouldHitCache()
    {
        var first = Assets.LoadTexture("img/a.png", AssetScope.Global);
        var second = Assets.LoadTexture("img/a.png", TextureOptions.Default, AssetScope.Global);

        second.Handle.Should().Be(first.Handle, "オプション省略は既定のオプションと同じキーのはず");
        Factory.CreatedCount.Should().Be(1);
        _source.OpenCount("img/a.png").Should().Be(1);
    }

    [Fact]
    public void LoadTexture_EquivalentPaths_ShouldShareEntry()
    {
        var first = Assets.LoadTexture("img/a.png", AssetScope.Global);

        Assets.LoadTexture("./img/a.png", AssetScope.Global).Handle.Should().Be(first.Handle);
        Assets.LoadTexture("/img/../img/a.png", AssetScope.Global).Handle.Should().Be(first.Handle);
        Factory.CreatedCount.Should().Be(1);
    }

    [Fact]
    public void LoadTexture_DifferentOptions_ShouldMissCache()
    {
        var nearest = Assets.LoadTexture("img/a.png", AssetScope.Global);
        var linear = Assets.LoadTexture("img/a.png", Linear, AssetScope.Global);

        linear.Handle.Should().NotBe(nearest.Handle);
        Factory.CreatedCount.Should().Be(2);
    }

    [Fact]
    public void LoadTexture_ShouldCloseStream()
    {
        Assets.LoadTexture("img/a.png", AssetScope.Global);

        _source.StreamsOf("img/a.png").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void LoadTexture_InvalidImage_ShouldThrowCloseStreamAndNotCache()
    {
        var act = () => Assets.LoadTexture("img/broken.png", AssetScope.Global);

        act.Should().Throw<Exception>();
        act.Should().Throw<Exception>();
        _source
            .OpenCount("img/broken.png")
            .Should()
            .Be(2, "失敗した読み込みはキャッシュに残らないはず");
        _source
            .StreamsOf("img/broken.png")
            .Should()
            .AllSatisfy(s => s.IsDisposed.Should().BeTrue());
    }

    [Fact]
    public void LoadTexture_Unload_ShouldDisposeTextureOnce()
    {
        var texture = Assets.LoadTexture("img/a.png", AssetScope.Global);

        Assets.Unload("img/a.png", AssetScope.Global);
        Assets.Dispose();

        Factory.DisposedHandlesSnapshot().Should().Equal(texture.Handle);
    }

    [Fact]
    public void TextureAndSpriteSheet_FromSamePath_ShouldBeSeparateEntries()
    {
        var texture = Assets.LoadTexture("img/tiles.png", AssetScope.Global);
        var cells = Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);

        cells[0].Handle.Should().NotBe(texture.Handle);
        Factory.CreatedCount.Should().Be(2);
    }

    [Fact]
    public void LoadSpriteSheet_Grid_ShouldSliceAndHitCache()
    {
        var first = Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);
        var second = Assets.LoadSpriteSheet(
            "img/tiles.png",
            2,
            1,
            (4, 4),
            TextureOptions.Default,
            AssetScope.Global
        );

        first.Should().HaveCount(2);
        first.Should().AllSatisfy(t => t.Size.Should().Be(new VectorInt(4, 4)));
        second.Select(t => t.Handle).Should().Equal(first.Select(t => t.Handle));
        Factory.CreatedCount.Should().Be(1);
    }

    [Fact]
    public void LoadSpriteSheet_GridWithDifferentArguments_ShouldMissCache()
    {
        Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);
        Assets.LoadSpriteSheet("img/tiles.png", 1, 1, (4, 4), AssetScope.Global);
        Assets.LoadSpriteSheet("img/tiles.png", 2, 2, (2, 2), AssetScope.Global);
        Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), Linear, AssetScope.Global);

        Factory.CreatedCount.Should().Be(4);
    }

    [Fact]
    public void LoadSpriteSheet_ShouldReturnNewArrayEachCall()
    {
        var first = Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);
        var original = first[0];
        first[0] = default;

        var second = Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);

        second.Should().NotBeSameAs(first);
        second[0].Should().Be(original, "返した配列を書き換えても、キャッシュには影響しないはず");
    }

    [Fact]
    public void LoadSpriteSheet_Rects_ShouldUseContentsAsKey()
    {
        RectInt[] rects = [new(0, 0, 4, 4), new(4, 0, 4, 2)];

        var first = Assets.LoadSpriteSheet("img/tiles.png", AssetScope.Global, rects);
        rects[0] = new RectInt(0, 0, 1, 1);
        var sameContents = Assets.LoadSpriteSheet(
            "img/tiles.png",
            AssetScope.Global,
            new RectInt(0, 0, 4, 4),
            new RectInt(4, 0, 4, 2)
        );

        first.Select(t => t.Size).Should().Equal(new VectorInt(4, 4), new VectorInt(4, 2));
        sameContents
            .Select(t => t.Handle)
            .Should()
            .Equal(
                first.Select(t => t.Handle),
                "呼び出し後に元の配列を書き換えても、キーは変わらないはず"
            );
        Factory.CreatedCount.Should().Be(1);
    }

    [Fact]
    public void LoadSpriteSheet_RectsWithDifferentContents_ShouldMissCache()
    {
        Assets.LoadSpriteSheet("img/tiles.png", AssetScope.Global, new RectInt(0, 0, 4, 4));
        Assets.LoadSpriteSheet("img/tiles.png", AssetScope.Global, new RectInt(0, 0, 4, 2));
        Assets.LoadSpriteSheet(
            "img/tiles.png",
            AssetScope.Global,
            new RectInt(0, 0, 4, 2),
            new RectInt(0, 0, 4, 4)
        );
        Assets.LoadSpriteSheet("img/tiles.png", Linear, AssetScope.Global, new RectInt(0, 0, 4, 4));

        Factory.CreatedCount.Should().Be(4, "矩形の値・順序・オプションが違えば別のエントリのはず");
    }

    [Fact]
    public void LoadSpriteSheet_GridAndEquivalentRects_ShouldBeSeparateEntries()
    {
        Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);
        Assets.LoadSpriteSheet(
            "img/tiles.png",
            AssetScope.Global,
            new RectInt(0, 0, 4, 4),
            new RectInt(4, 0, 4, 4)
        );

        Factory.CreatedCount.Should().Be(2);
    }

    [Fact]
    public void LoadSpriteSheet_Unload_ShouldDestroySharedTextureOnce()
    {
        var cells = Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);

        Assets.Unload("img/tiles.png");

        Factory.DisposedHandlesSnapshot().Should().Equal(cells[0].Handle);
    }

    [Fact]
    public void LoadSpriteSheet_Empty_ShouldBeCachedAndReleasedWithoutError()
    {
        var cells = Assets.LoadSpriteSheet("img/tiles.png", 0, 0, (4, 4), AssetScope.Global);

        var act = () => Assets.Unload("img/tiles.png");

        cells.Should().BeEmpty();
        act.Should().NotThrow();
    }

    [Fact]
    public void Load9Sliced_ShouldSliceAndHitCache()
    {
        var first = Assets.Load9Sliced("img/frame.png", 3, 3, 3, 3, AssetScope.Global);
        var second = Assets.Load9Sliced(
            "img/frame.png",
            3,
            3,
            3,
            3,
            TextureOptions.Default,
            AssetScope.Global
        );

        first.Size.Should().Be(new VectorInt(9, 9));
        first.MiddleCenter.Size.Should().Be(new VectorInt(3, 3));
        second.TopLeft.Handle.Should().Be(first.TopLeft.Handle);
        Factory.CreatedCount.Should().Be(9);
    }

    [Fact]
    public void Load9Sliced_DifferentArguments_ShouldMissCache()
    {
        Assets.Load9Sliced("img/frame.png", 3, 3, 3, 3, AssetScope.Global);
        Assets.Load9Sliced("img/frame.png", 2, 3, 3, 3, AssetScope.Global);
        Assets.Load9Sliced("img/frame.png", 3, 3, 3, 3, Linear, AssetScope.Global);

        Factory.CreatedCount.Should().Be(27);
    }

    [Fact]
    public void Load9Sliced_Unload_ShouldDestroyAllPieces()
    {
        var texture = Assets.Load9Sliced("img/frame.png", 3, 3, 3, 3, AssetScope.Global);

        Assets.Unload("img/frame.png", AssetScope.Global);

        Factory.DisposedHandlesSnapshot().Should().HaveCount(9).And.Contain(texture.TopLeft.Handle);
    }

    [Fact]
    public void UnloadPath_ShouldReleaseAllTextureKindsOfThePath()
    {
        Assets.LoadTexture("img/tiles.png", AssetScope.Global);
        Assets.LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global);
        var other = Assets.LoadTexture("img/a.png", AssetScope.Global);

        Assets.Unload("img/tiles.png");

        Factory.DisposedHandlesSnapshot().Should().HaveCount(2).And.NotContain(other.Handle);
    }

    [Fact]
    public void GenericLoad_ShouldNotShareCacheWithTypedLoaders()
    {
        Assets.LoadTexture("img/a.png", AssetScope.Global);
        var calls = 0;

        foreach (var key in new[] { "", "Texture", "texture" })
        {
            Assets.Load(
                "img/a.png",
                key,
                _ =>
                {
                    calls++;
                    return new object();
                },
                AssetScope.Global
            );
        }

        calls
            .Should()
            .Be(3, "汎用の読み込みのキーは、型付きのメソッドのキーとは別の名前空間のはず");
    }
}
