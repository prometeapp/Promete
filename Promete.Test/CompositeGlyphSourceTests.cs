using FluentAssertions;
using Promete.Graphics.Fonts;

namespace Promete.Test;

public class CompositeGlyphSourceTests
{
    private const string FontPath = "assets/Koruri.ttf";

    [Fact]
    public void 空のチェーンは作成できない()
    {
        var act = () => new CompositeGlyphSource([]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void 先頭のソースが優先される()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        using var composite = new CompositeGlyphSource([custom, font.Source]);

        composite.TryGetGlyph(0xE000, new GlyphRenderOptions(16), out var glyph).Should().BeTrue();
        glyph.Source.Should().BeSameAs(custom);
    }

    [Fact]
    public void 先頭が持たない文字は後続のソースで補完される()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        using var composite = new CompositeGlyphSource([custom, font.Source]);

        composite.TryGetGlyph('あ', new GlyphRenderOptions(16), out var glyph).Should().BeTrue();
        glyph.Source.Should().BeSameAs(font.Source);
    }

    [Fact]
    public void どのソースも持たない文字は取得できない()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        using var composite = new CompositeGlyphSource([custom, font.Source]);

        composite.TryGetGlyph(0xE100, new GlyphRenderOptions(16), out _).Should().BeFalse();
    }

    [Fact]
    public void メトリクスは既定で先頭のソースを基準とする()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        using var composite = new CompositeGlyphSource([custom, font.Source]);

        composite
            .GetMetrics(new GlyphRenderOptions(16))
            .Should()
            .Be(custom.GetMetrics(new GlyphRenderOptions(16)));
    }

    [Fact]
    public void メトリクスの基準を明示できる()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        using var composite = new CompositeGlyphSource([custom, font.Source], font.Source);

        composite
            .GetMetrics(new GlyphRenderOptions(16))
            .Should()
            .Be(font.Source.GetMetrics(new GlyphRenderOptions(16)));
    }

    [Fact]
    public void 外字を重ねても行の高さは変わらない()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        var withCustom = font.WithOverride(custom);

        withCustom.Metrics.Should().Be(font.Metrics, "外字は本文の行送りに影響してはならない");
    }

    [Fact]
    public void フォールバックを追加しても行の高さは変わらない()
    {
        using var custom = CreateIconSource(0xE000);
        var font = Font.FromFile(FontPath, 16);

        font.WithFallback(custom).Metrics.Should().Be(font.Metrics);
    }

    [Fact]
    public void 外字を重ねた状態でさらに重ねても基準は保たれる()
    {
        using var first = CreateIconSource(0xE000);
        using var second = CreateIconSource(0xE001);
        var font = Font.FromFile(FontPath, 16);

        var result = font.WithOverride(first).WithOverride(second);

        result.Metrics.Should().Be(font.Metrics);
        result.Source.TryGetGlyph(0xE000, result.RenderOptions, out _).Should().BeTrue();
        result.Source.TryGetGlyph(0xE001, result.RenderOptions, out _).Should().BeTrue();
        result.Source.TryGetGlyph('あ', result.RenderOptions, out _).Should().BeTrue();
    }

    [Fact]
    public void 外字を重ねても行の高さが本文基準であることがレイアウトに反映される()
    {
        // 本文よりはるかに背の高い外字を用意する
        using var tall = new BitmapGlyphSource(64);
        tall.Register(0xE000, new byte[64 * 64 * 4], (64, 64));

        var font = Font.FromFile(FontPath, 16);
        var options = new TextRenderingOptions();

        var plain = TextLayoutEngine.Layout("あ", font, options);
        var withTall = TextLayoutEngine.Layout("あ", font.WithOverride(tall), options);

        withTall.Size.Y.Should().Be(plain.Size.Y);
    }

    [Fact]
    public void 破棄時に既定では各ソースを破棄しない()
    {
        var custom = CreateIconSource(0xE000);
        var composite = new CompositeGlyphSource([custom]);

        composite.Dispose();

        var act = () => custom.TryGetGlyph(0xE000, new GlyphRenderOptions(16), out _);
        act.Should().NotThrow();
        custom.Dispose();
    }

    [Fact]
    public void 所有権を渡した場合は各ソースも破棄される()
    {
        var custom = CreateIconSource(0xE000);
        var composite = new CompositeGlyphSource([custom], leavesOpen: false);

        composite.Dispose();

        var act = () => custom.TryGetGlyph(0xE000, new GlyphRenderOptions(16), out _);
        act.Should().Throw<ObjectDisposedException>();
    }

    private static BitmapGlyphSource CreateIconSource(int codepoint)
    {
        var source = new BitmapGlyphSource(16);
        source.Register(codepoint, new byte[16 * 16 * 4], (16, 16));
        return source;
    }
}
