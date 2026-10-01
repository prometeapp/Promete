using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Graphics.Fonts.FreeType;
using Promete.Test.Fakes;

namespace Promete.Test;

/// <summary>
/// 種類の異なるグリフソースを混在させたときに、
/// グリフアトラスが取り違えを起こさないことを検証します。
/// </summary>
public class GlyphAtlasMixedSourceTests
{
    private const string FontPath = "assets/Koruri.ttf";

    [Fact]
    public void 種類の異なるグリフソースでも識別子は重複しない()
    {
        using var vector = FreeTypeGlyphSource.FromFile(FontPath);
        using var bitmapA = new BitmapGlyphSource(16);
        using var bitmapB = new BitmapGlyphSource(16);
        using var composite = new CompositeGlyphSource([bitmapA, vector]);

        var ids = new[] { vector.SourceId, bitmapA.SourceId, bitmapB.SourceId, composite.SourceId };

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void 複数の外字を登録しても互いに取り違えない()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 256);
        using var icons = new BitmapGlyphSource(16);

        // 3 つの外字に、それぞれ異なる位置の点を描く
        var a = icons.Register("a", MarkedPixels(0, 0), (16, 16));
        var b = icons.Register("b", MarkedPixels(8, 0), (16, 16));
        var c = icons.Register("c", MarkedPixels(0, 8), (16, 16));

        var options = new GlyphRenderOptions(16);
        AlphaOf(factory, atlas, icons, a, options, 0, 0).Should().Be(255);
        AlphaOf(factory, atlas, icons, b, options, 8, 0).Should().Be(255);
        AlphaOf(factory, atlas, icons, c, options, 0, 8).Should().Be(255);

        // それぞれの特徴点以外は透明でなければならない
        AlphaOf(factory, atlas, icons, a, options, 8, 0).Should().Be(0);
        AlphaOf(factory, atlas, icons, b, options, 0, 0).Should().Be(0);
        AlphaOf(factory, atlas, icons, c, options, 0, 0).Should().Be(0);
    }

    [Fact]
    public void ベクターフォントと外字を混在させても取り違えない()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 512);
        using var icons = new BitmapGlyphSource(16);
        var codepoint = icons.Register("icon", MarkedPixels(0, 0), (16, 16));

        var font = Font.FromFile(FontPath, 16).WithOverride(icons);
        var options = font.RenderOptions;

        // 先に本文のグリフをアトラスへ登録し、そのあとで外字を登録する
        foreach (var c in "あいうえおABCDE")
        {
            font.Source.TryGetGlyph(c, options, out var glyph).Should().BeTrue();
            atlas.GetOrAdd(glyph, options);
        }

        font.Source.TryGetGlyph(codepoint, options, out var iconGlyph).Should().BeTrue();
        iconGlyph.Source.Should().BeSameAs(icons);

        var entry = atlas.GetOrAdd(iconGlyph, options);
        entry.Size.Should().Be(new VectorInt(16, 16), "外字は登録した大きさで取り出せる必要がある");
        ReadAlpha(factory, entry, 0, 0).Should().Be(255);
        ReadAlpha(factory, entry, 8, 8).Should().Be(0);
    }

    [Fact]
    public void 外字を先に登録しても本文のグリフを取り違えない()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 512);
        using var icons = new BitmapGlyphSource(16);
        var codepoint = icons.Register("icon", MarkedPixels(0, 0), (16, 16));

        var font = Font.FromFile(FontPath, 16).WithOverride(icons);
        var options = font.RenderOptions;

        font.Source.TryGetGlyph(codepoint, options, out var iconGlyph).Should().BeTrue();
        atlas.GetOrAdd(iconGlyph, options);

        font.Source.TryGetGlyph('あ', options, out var textGlyph).Should().BeTrue();
        var entry = atlas.GetOrAdd(textGlyph, options);

        // 外字は 16x16 の 1 点のみが不透明。本文のグリフがそれと同じ内容になってはならない
        entry.Size.Should().NotBe(new VectorInt(16, 16));
    }

    [Fact]
    public void 同じ内容でも供給元が異なれば別のエントリになる()
    {
        var factory = new FakeTextureFactory();
        using var atlas = new GlyphAtlas(factory, 256);
        using var first = new BitmapGlyphSource(16);
        using var second = new BitmapGlyphSource(16);

        first.Register('X', MarkedPixels(0, 0), (16, 16));
        second.Register('X', MarkedPixels(8, 8), (16, 16));

        var options = new GlyphRenderOptions(16);
        first.TryGetGlyph('X', options, out var glyphA).Should().BeTrue();
        second.TryGetGlyph('X', options, out var glyphB).Should().BeTrue();

        atlas.GetOrAdd(glyphA, options);
        atlas.GetOrAdd(glyphB, options);

        atlas.GlyphCount.Should().Be(2);
        ReadAlpha(factory, atlas.GetOrAdd(glyphA, options), 0, 0).Should().Be(255);
        ReadAlpha(factory, atlas.GetOrAdd(glyphB, options), 8, 8).Should().Be(255);
    }

    /// <summary>
    /// 指定した位置のみが不透明な 16x16 のピクセルデータを生成します。
    /// </summary>
    private static byte[] MarkedPixels(int x, int y)
    {
        var pixels = new byte[16 * 16 * 4];
        var offset = ((y * 16) + x) * 4;
        pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = 255;
        return pixels;
    }

    private static byte AlphaOf(
        FakeTextureFactory factory,
        GlyphAtlas atlas,
        IGlyphSource source,
        int codepoint,
        GlyphRenderOptions options,
        int x,
        int y
    )
    {
        source.TryGetGlyph(codepoint, options, out var glyph).Should().BeTrue();
        return ReadAlpha(factory, atlas.GetOrAdd(glyph, options), x, y);
    }

    /// <summary>
    /// アトラス上の該当領域から、指定した位置のアルファ値を読み出します。
    /// </summary>
    private static byte ReadAlpha(FakeTextureFactory factory, GlyphEntry entry, int x, int y)
    {
        var pageSize = factory.GetSize(entry.Texture.Handle);
        var originX = (int)MathF.Round(entry.Texture.UvStart.X * pageSize.X);
        var originY = (int)MathF.Round(entry.Texture.UvStart.Y * pageSize.Y);
        return factory.GetAlphaAt(entry.Texture.Handle, pageSize, (originX + x, originY + y));
    }
}
