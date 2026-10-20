using FluentAssertions;
using Promete.Assets;
using Promete.Audio;
using Promete.Graphics;
using Promete.Graphics.Fonts;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 型付きの非同期の読み込み (<c>LoadTextureAsync</c> など) のテスト。同期版とキャッシュを共有し、実行中の読み込みを共有すること。
/// </summary>
/// <remarks>
/// <see cref="AssetManagerAsyncTests"/> と同じく、Task をブロックして待つ。
/// </remarks>
public sealed class AssetManagerTypedAsyncTests : IDisposable
{
    private static readonly TextureOptions Linear = new(TextureFilterMode.Linear);

    private readonly TrackingAssetSource _source = new TrackingAssetSource()
        .Add("img/a.png", TestAssetData.Png(4, 4, seed: 1))
        .Add("img/tiles.png", TestAssetData.Png(8, 4, seed: 3))
        .Add("img/frame.png", TestAssetData.Png(9, 9, seed: 4))
        .Add("fonts/ui.ttf", TestAssetData.Text("ui-font-data"))
        .Add("sfx/se.wav", TestAssetData.Wav());

    private readonly PrometeApp _app;

    public AssetManagerTypedAsyncTests()
    {
        ScriptedScene.Script.Reset();
        _app = AssetsTestApp.Build(_source);
    }

    private AssetManager Assets => _app.Assets();

    private AssetsTestTextureFactory Factory => _app.Textures();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void LoadTextureAsync_ShouldShareCacheWithSyncLoad()
    {
        var texture = Assets
            .LoadTextureAsync("img/a.png", AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var withOptions = Assets
            .LoadTextureAsync("img/a.png", Linear, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        texture.Size.Should().Be(new VectorInt(4, 4));
        Factory.GetOptions(withOptions.Handle).Should().Be(Linear);
        Assets.LoadTexture("img/a.png", AssetScope.Global).Handle.Should().Be(texture.Handle);
        Assets
            .LoadTexture("img/a.png", Linear, AssetScope.Global)
            .Handle.Should()
            .Be(withOptions.Handle);
        Factory.CreatedCount.Should().Be(2);
    }

    [Fact]
    public void LoadTextureAsync_ConcurrentSameKey_ShouldShareOneLoad()
    {
        var gate = _source.BlockNextOpen("img/a.png");

        var first = Assets.LoadTextureAsync("img/a.png", AssetScope.Global);
        var second = Assets.LoadTextureAsync(
            "img/a.png",
            TextureOptions.Default,
            AssetScope.Global
        );
        gate.WaitEntered();
        gate.Release();

        first.GetAwaiter().GetResult().Handle.Should().Be(second.GetAwaiter().GetResult().Handle);
        Factory.CreatedCount.Should().Be(1);
        _source.OpenCount("img/a.png").Should().Be(1);
    }

    [Fact]
    public void LoadTextureAsync_ShouldCloseStreamAfterCompletion()
    {
        Assets.LoadTextureAsync("img/a.png", AssetScope.Global).GetAwaiter().GetResult();

        _source.StreamsOf("img/a.png").Should().ContainSingle().Which.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void LoadSpriteSheetAsync_Grid_ShouldShareCacheWithSyncLoad()
    {
        var cells = Assets
            .LoadSpriteSheetAsync("img/tiles.png", 2, 1, (4, 4), AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var linear = Assets
            .LoadSpriteSheetAsync("img/tiles.png", 2, 1, (4, 4), Linear, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        cells.Should().HaveCount(2);
        Assets
            .LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), AssetScope.Global)
            .Select(t => t.Handle)
            .Should()
            .Equal(cells.Select(t => t.Handle));
        Assets
            .LoadSpriteSheet("img/tiles.png", 2, 1, (4, 4), Linear, AssetScope.Global)
            .Select(t => t.Handle)
            .Should()
            .Equal(linear.Select(t => t.Handle));
        Factory.CreatedCount.Should().Be(2);
    }

    [Fact]
    public void LoadSpriteSheetAsync_Rects_ShouldCopyRectsAtCallTime()
    {
        var rects = new List<RectInt> { new(0, 0, 4, 4), new(4, 0, 4, 2) };
        var gate = _source.BlockNextOpen("img/tiles.png");

        var task = Assets.LoadSpriteSheetAsync("img/tiles.png", AssetScope.Global, rects);
        gate.WaitEntered();
        rects[0] = new RectInt(0, 0, 1, 1);
        gate.Release();
        var cells = task.GetAwaiter().GetResult();

        cells.Select(t => t.Size).Should().Equal(new VectorInt(4, 4), new VectorInt(4, 2));
        Assets
            .LoadSpriteSheet(
                "img/tiles.png",
                AssetScope.Global,
                new RectInt(0, 0, 4, 4),
                new RectInt(4, 0, 4, 2)
            )
            .Select(t => t.Handle)
            .Should()
            .Equal(cells.Select(t => t.Handle), "同期版と同じキーのはず");
        Factory.CreatedCount.Should().Be(1);
    }

    [Fact]
    public void LoadSpriteSheetAsync_RectsWithOptions_ShouldShareCacheWithSyncLoad()
    {
        var cells = Assets
            .LoadSpriteSheetAsync(
                "img/tiles.png",
                Linear,
                AssetScope.Global,
                [new RectInt(0, 0, 4, 4)]
            )
            .GetAwaiter()
            .GetResult();

        Assets
            .LoadSpriteSheet("img/tiles.png", Linear, AssetScope.Global, new RectInt(0, 0, 4, 4))[0]
            .Handle.Should()
            .Be(cells[0].Handle);
        Factory.GetOptions(cells[0].Handle).Should().Be(Linear);
        Factory.CreatedCount.Should().Be(1);
    }

    [Fact]
    public void Load9SlicedAsync_ShouldShareCacheWithSyncLoad()
    {
        var sliced = Assets
            .Load9SlicedAsync("img/frame.png", 3, 3, 3, 3, AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var linear = Assets
            .Load9SlicedAsync("img/frame.png", 3, 3, 3, 3, Linear, AssetScope.Global)
            .GetAwaiter()
            .GetResult();

        sliced.Size.Should().Be(new VectorInt(9, 9));
        Assets
            .Load9Sliced("img/frame.png", 3, 3, 3, 3, AssetScope.Global)
            .TopLeft.Handle.Should()
            .Be(sliced.TopLeft.Handle);
        Assets
            .Load9Sliced("img/frame.png", 3, 3, 3, 3, Linear, AssetScope.Global)
            .TopLeft.Handle.Should()
            .Be(linear.TopLeft.Handle);
        Factory.CreatedCount.Should().Be(18);
    }

    [Fact]
    public void LoadFontAsync_ShouldShareGlyphSourceWithSyncLoad()
    {
        var small = Assets
            .LoadFontAsync("fonts/ui.ttf", 16, AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var bold = Assets
            .LoadFontAsync("fonts/ui.ttf", 24, FontStyle.Bold, AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var aliased = Assets
            .LoadFontAsync("fonts/ui.ttf", 12, FontStyle.Normal, false, AssetScope.Global)
            .GetAwaiter()
            .GetResult();
        var sync = Assets.LoadFont("fonts/ui.ttf", 32, AssetScope.Global);

        bold.Source.Should().BeSameAs(small.Source);
        aliased.Source.Should().BeSameAs(small.Source);
        sync.Source.Should().BeSameAs(small.Source);
        bold.Size.Should().Be(24);
        bold.Style.Should().Be(FontStyle.Bold);
        aliased.IsAntialiased.Should().BeFalse();
        _app.Fonts().FromStreamCount.Should().Be(1);
        _source
            .StreamsOf("fonts/ui.ttf")
            .Should()
            .ContainSingle()
            .Which.IsDisposed.Should()
            .BeTrue();
    }

    [Fact]
    public void LoadFontAsync_ConcurrentDifferentSizes_ShouldShareOneGlyphSource()
    {
        var gate = _source.BlockNextOpen("fonts/ui.ttf");

        var small = Assets.LoadFontAsync("fonts/ui.ttf", 16, AssetScope.Global);
        var large = Assets.LoadFontAsync("fonts/ui.ttf", 32, AssetScope.Global);
        gate.WaitEntered();
        gate.Release();

        large
            .GetAwaiter()
            .GetResult()
            .Source.Should()
            .BeSameAs(small.GetAwaiter().GetResult().Source);
        large.GetAwaiter().GetResult().Size.Should().Be(32);
        _app.Fonts().FromStreamCount.Should().Be(1);
    }

    [Fact]
    public void LoadAudioAsync_ShouldShareInstanceWithSyncLoad()
    {
        var audio = Assets.LoadAudioAsync("sfx/se.wav", AssetScope.Global).GetAwaiter().GetResult();

        audio.Should().BeOfType<WaveAudioSource>();
        Assets.LoadAudio("sfx/se.wav", AssetScope.Global).Should().BeSameAs(audio);
        _source.OpenCount("sfx/se.wav").Should().Be(1);
    }

    [Fact]
    public void LoadAudioAsync_NoDecoder_ShouldFaultWithNotSupported()
    {
        _source.Add("misc/x.bin", [1, 2, 3]);

        var act = () =>
            Assets.LoadAudioAsync("misc/x.bin", AssetScope.Global).GetAwaiter().GetResult();

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LoadTextureAsync_SceneReleasedBeforeCompletion_ShouldDestroyTextureOnMainThread()
    {
        var handle = 0;

        _app.RunWith<ScriptedSceneA>(() =>
        {
            var gate = _source.BlockNextOpen("img/a.png");
            var task = Assets.LoadTextureAsync("img/a.png", AssetScope.Scene);
            gate.WaitEntered();

            _app.LoadScene<ScriptedEmptyScene>();
            gate.Release();
            handle = task.GetAwaiter().GetResult().Handle;
            _app.PumpFrame();
        });

        Factory.DisposedHandlesSnapshot().Should().Equal(handle);
        Factory
            .OffMainThreadDestroyCount.Should()
            .Be(0, "GL はメインスレッド以外でテクスチャを破棄できないため");
    }

    [Fact]
    public void LoadTextureAsync_InSceneOnStart_ShouldBelongToThatScene()
    {
        var handle = 0;

        _app.RunWith<ScriptedSceneA>(() =>
        {
            handle = Assets
                .LoadTextureAsync("img/a.png", AssetScope.Scene)
                .GetAwaiter()
                .GetResult()
                .Handle;
            Factory.DisposedHandlesSnapshot().Should().BeEmpty();

            _app.LoadScene<ScriptedEmptyScene>();
        });

        Factory.DisposedHandlesSnapshot().Should().Equal(handle);
    }

    [Fact]
    public void TypedAsync_ScopeErrors_ShouldThrowSynchronously()
    {
        Action texture = () => _ = Assets.LoadTextureAsync("img/a.png", AssetScope.Scene);
        Action sheet = () =>
            _ = Assets.LoadSpriteSheetAsync("img/tiles.png", 2, 1, (4, 4), AssetScope.Scene);
        Action rects = () =>
            _ = Assets.LoadSpriteSheetAsync(
                "img/tiles.png",
                AssetScope.Scene,
                [new RectInt(0, 0, 1, 1)]
            );
        Action nullRects = () =>
            _ = Assets.LoadSpriteSheetAsync("img/tiles.png", AssetScope.Global, null!);
        Action sliced = () =>
            _ = Assets.Load9SlicedAsync("img/frame.png", 3, 3, 3, 3, AssetScope.Scene);
        Action font = () => _ = Assets.LoadFontAsync("fonts/ui.ttf", 16, AssetScope.Scene);
        Action audio = () => _ = Assets.LoadAudioAsync("sfx/se.wav", AssetScope.Scene);
        Action invalidPath = () => _ = Assets.LoadTextureAsync(@"img\a.png", AssetScope.Global);

        texture.Should().Throw<InvalidOperationException>();
        sheet.Should().Throw<InvalidOperationException>();
        rects.Should().Throw<InvalidOperationException>();
        nullRects.Should().Throw<ArgumentNullException>();
        sliced.Should().Throw<InvalidOperationException>();
        font.Should().Throw<InvalidOperationException>();
        audio.Should().Throw<InvalidOperationException>();
        invalidPath.Should().Throw<ArgumentException>();
    }
}
