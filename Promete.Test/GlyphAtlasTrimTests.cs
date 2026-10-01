using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Test.Fakes;

namespace Promete.Test;

/// <summary>
/// グリフアトラスが際限なく肥大化しないことを検証します。
/// </summary>
public class GlyphAtlasTrimTests
{
    [Fact]
    public void フォントサイズは一定の単位に丸められる()
    {
        new GlyphRenderOptions(16.2f).Size.Should().Be(16);
        new GlyphRenderOptions(16.3f).Size.Should().Be(16.5f);
        new GlyphRenderOptions(16.75f).Size.Should().Be(17);
    }

    [Fact]
    public void with式で変更した場合も丸められる()
    {
        var options = new GlyphRenderOptions(16);

        (options with { Size = 32.3f }).Size.Should().Be(32.5f);
        (options with { Size = 32.1f }).Size.Should().Be(32);
    }

    [Fact]
    public void 丸められた結果が同じサイズは同じキーになる()
    {
        var a = new GlyphRenderOptions(16.1f);
        var b = new GlyphRenderOptions(15.9f);

        a.Should().Be(b);
    }

    [Fact]
    public void 連続的にサイズを変化させてもキャッシュの種類は抑えられる()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 256);
        using var source = new BitmapGlyphSource(16);
        source.Register('A', new byte[16 * 16 * 4], (16, 16));

        // 16px から 17px までを 0.05px 刻みで変化させる
        for (var size = 16f; size <= 17f; size += 0.05f)
        {
            var options = new GlyphRenderOptions(size);
            source.TryGetGlyph('A', options, out var glyph).Should().BeTrue();
            atlas.GetOrAdd(glyph, options);
        }

        // 丸めがなければ 21 種類になるが、0.5px 単位に丸められる
        atlas.GlyphCount.Should().Be(3);
    }

    [Fact]
    public void ページ数が上限に達するとトリムが予約される()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 32) { MaxPages = 3 };
        using var source = new BitmapGlyphSource(32);

        // 1 ページに 1 つしか入らない大きさのグリフを積む
        for (var i = 0; i < 10; i++)
            source.Register(0xE000 + i, new byte[32 * 32 * 4], (32, 32));

        for (var i = 0; i < 10; i++)
        {
            var options = new GlyphRenderOptions(32);
            source.TryGetGlyph(0xE000 + i, options, out var glyph).Should().BeTrue();
            atlas.GetOrAdd(glyph, options);
            atlas.TrimIfNeeded();
        }

        atlas.PageCount.Should().BeLessThan(3, "上限に達したらアトラスが作り直される");
    }

    [Fact]
    public void トリムはフレームの境界まで遅延される()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 32) { MaxPages = 2 };
        using var source = new BitmapGlyphSource(32);
        source.Register(0xE000, new byte[32 * 32 * 4], (32, 32));
        source.Register(0xE001, new byte[32 * 32 * 4], (32, 32));

        var options = new GlyphRenderOptions(32);
        source.TryGetGlyph(0xE000, options, out var first).Should().BeTrue();
        source.TryGetGlyph(0xE001, options, out var second).Should().BeTrue();

        var entryA = atlas.GetOrAdd(first, options);
        var entryB = atlas.GetOrAdd(second, options);

        // 上限に達しても、描画中に解放されてはならない
        factory.DisposedHandles.Should().BeEmpty();
        entryA.Texture.Handle.Should().NotBe(0);
        entryB.Texture.Handle.Should().NotBe(0);

        atlas.TrimIfNeeded();

        atlas.PageCount.Should().Be(0);
        atlas.GlyphCount.Should().Be(0);
        factory.DisposedHandles.Should().NotBeEmpty();
    }

    [Fact]
    public void 上限に達していなければトリムは何もしない()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 256) { MaxPages = 8 };
        using var source = new BitmapGlyphSource(16);
        source.Register('A', new byte[16 * 16 * 4], (16, 16));

        var options = new GlyphRenderOptions(16);
        source.TryGetGlyph('A', options, out var glyph).Should().BeTrue();
        atlas.GetOrAdd(glyph, options);

        atlas.TrimIfNeeded();

        atlas.GlyphCount.Should().Be(1);
        factory.DisposedHandles.Should().BeEmpty();
    }
}
