using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Graphics.Fonts.FreeType;

namespace Promete.Test;

public class FreeTypeGlyphSourceTests
{
    private const string VectorFontPath = "assets/Koruri.ttf";
    private const string BitmapFontPath = "assets/MisakiGothic.ttf";

    [Fact]
    public void ファイルからフォントを読み込める()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        source.FamilyName.Should().Be("Koruri");
        source.IsScalable.Should().BeTrue();
    }

    [Fact]
    public void 存在しないファイルを指定すると例外をスローする()
    {
        var act = () => FreeTypeGlyphSource.FromFile("assets/notfound.ttf");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void 不正なデータを指定すると例外をスローする()
    {
        var act = () => FreeTypeGlyphSource.FromMemory("これはフォントではありません"u8);
        act.Should().Throw<FontException>();
    }

    [Fact]
    public void ストリームからフォントを読み込める()
    {
        using var stream = File.OpenRead(VectorFontPath);
        using var source = FreeTypeGlyphSource.FromStream(stream);

        source.FamilyName.Should().Be("Koruri");
    }

    [Fact]
    public void フォントサイズに応じたメトリクスを取得できる()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        var small = source.GetMetrics(new GlyphRenderOptions(16));
        var large = source.GetMetrics(new GlyphRenderOptions(32));

        small.Ascender.Should().BePositive();
        small.Descender.Should().BeNegative();
        small.LineHeight.Should().BePositive();
        large.LineHeight.Should().BeGreaterThan(small.LineHeight);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('あ')]
    [InlineData('漢')]
    public void 収録されているグリフを取得できる(char c)
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        source.TryGetGlyph(c, new GlyphRenderOptions(16), out var glyph).Should().BeTrue();
        glyph.Codepoint.Should().Be(c);
        glyph.GlyphIndex.Should().NotBe(0u);
        glyph.Advance.Should().BePositive();
        glyph.Source.Should().BeSameAs(source);
    }

    [Fact]
    public void 収録されていないグリフは取得できない()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        // 未割り当ての私用領域
        source.TryGetGlyph(0xE000, new GlyphRenderOptions(16), out _).Should().BeFalse();
    }

    [Fact]
    public void グリフをラスタライズできる()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);
        var options = new GlyphRenderOptions(16);

        source.TryGetGlyph('A', options, out var glyph).Should().BeTrue();
        var bitmap = source.Rasterize(glyph, options);

        bitmap.IsEmpty.Should().BeFalse();
        bitmap.Size.X.Should().BePositive();
        bitmap.Size.Y.Should().BePositive();
        bitmap.Pixels.Length.Should().Be(bitmap.Size.X * bitmap.Size.Y * 4);

        // ベースラインより上に描画されるため、Y 方向のベアリングは負になる
        bitmap.Bearing.Y.Should().BeNegative();
    }

    [Fact]
    public void 空白文字は実体を持たない()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);
        var options = new GlyphRenderOptions(16);

        source.TryGetGlyph(' ', options, out var glyph).Should().BeTrue();
        glyph.Advance.Should().BePositive();
        source.Rasterize(glyph, options).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void アンチエイリアスの有無でラスタライズ結果が変わる()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        var antialiased = Rasterize(source, 'A', new GlyphRenderOptions(16));
        var aliased = Rasterize(source, 'A', new GlyphRenderOptions(16, IsAntialiased: false));

        // アンチエイリアス無効時、アルファは 0 か 255 のいずれかになる
        GetAlphaValues(aliased).Should().OnlyContain(a => a == 0 || a == 255);
        GetAlphaValues(antialiased).Should().Contain(a => a > 0 && a < 255);
    }

    [Fact]
    public void フォントサイズを変えるとラスタライズ結果の大きさが変わる()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);

        var small = Rasterize(source, 'A', new GlyphRenderOptions(16));
        var large = Rasterize(source, 'A', new GlyphRenderOptions(48));

        large.Size.X.Should().BeGreaterThan(small.Size.X);
        large.Size.Y.Should().BeGreaterThan(small.Size.Y);
    }

    [Fact]
    public void 埋め込みビットマップフォントはモノクロとしてラスタライズされる()
    {
        using var source = FreeTypeGlyphSource.FromFile(BitmapFontPath);
        source.HasFixedSizes.Should().BeTrue();

        // アンチエイリアスを有効にしていても、埋め込みビットマップが優先される
        var bitmap = Rasterize(source, 'あ', new GlyphRenderOptions(8));

        bitmap.Size.Should().Be(new VectorInt(8, 8));
        GetAlphaValues(bitmap).Should().OnlyContain(a => a == 0 || a == 255);
    }

    [Fact]
    public void カーニングを持たないフォントでは0を返す()
    {
        using var source = FreeTypeGlyphSource.FromFile(VectorFontPath);
        source.HasKerning.Should().BeFalse();
        source.GetKerning('A', 'V', new GlyphRenderOptions(16)).Should().Be(0);
    }

    [Fact]
    public void 破棄後にアクセスすると例外をスローする()
    {
        var source = FreeTypeGlyphSource.FromFile(VectorFontPath);
        source.Dispose();

        var act = () => source.GetMetrics(new GlyphRenderOptions(16));
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void 複数回破棄しても問題ない()
    {
        var source = FreeTypeGlyphSource.FromFile(VectorFontPath);
        source.Dispose();

        var act = source.Dispose;
        act.Should().NotThrow();
    }

    [Fact]
    public void ソースIDは一意である()
    {
        using var a = FreeTypeGlyphSource.FromFile(VectorFontPath);
        using var b = FreeTypeGlyphSource.FromFile(VectorFontPath);

        a.SourceId.Should().NotBe(b.SourceId);
    }

    private static GlyphBitmap Rasterize(IGlyphSource source, int codepoint, GlyphRenderOptions options)
    {
        source.TryGetGlyph(codepoint, options, out var glyph).Should().BeTrue();
        return source.Rasterize(glyph, options);
    }

    private static IEnumerable<byte> GetAlphaValues(GlyphBitmap bitmap)
    {
        for (var i = 3; i < bitmap.Pixels.Length; i += 4)
            yield return bitmap.Pixels[i];
    }
}
