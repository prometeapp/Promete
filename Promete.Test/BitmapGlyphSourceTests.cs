using FluentAssertions;
using Promete.Graphics.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Promete.Test;

public class BitmapGlyphSourceTests
{
    [Fact]
    public void ピクセルデータからグリフを登録できる()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        source.GlyphCount.Should().Be(1);
        source.TryGetGlyph('A', new GlyphRenderOptions(8), out var glyph).Should().BeTrue();
        glyph.Advance.Should().Be(4);
        glyph.Source.Should().BeSameAs(source);
    }

    [Fact]
    public void 登録されていない文字は取得できない()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        source.TryGetGlyph('B', new GlyphRenderOptions(8), out _).Should().BeFalse();
    }

    [Fact]
    public void 送り幅を明示的に指定できる()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8), advance: 6);

        source.TryGetGlyph('A', new GlyphRenderOptions(8), out var glyph).Should().BeTrue();
        glyph.Advance.Should().Be(6);
    }

    [Fact]
    public void 元のサイズと同じ大きさなら等倍で描画される()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        var bitmap = Rasterize(source, 'A', new GlyphRenderOptions(8));

        bitmap.Size.Should().Be(new VectorInt(4, 8));
    }

    [Fact]
    public void フォントサイズは元の大きさに対する整数倍として解決される()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        Rasterize(source, 'A', new GlyphRenderOptions(16)).Size.Should().Be(new VectorInt(8, 16));
        Rasterize(source, 'A', new GlyphRenderOptions(24)).Size.Should().Be(new VectorInt(12, 24));

        // 20px は 8px の 2.5 倍。中間値は大きい方へ丸める
        Rasterize(source, 'A', new GlyphRenderOptions(20)).Size.Should().Be(new VectorInt(12, 24));

        // 偶数寄せの丸めでは 2 倍になってしまう組み合わせも、正しく 3 倍になる
        Rasterize(source, 'A', new GlyphRenderOptions(21)).Size.Should().Be(new VectorInt(12, 24));
    }

    [Fact]
    public void 元のサイズより小さくしても縮小されない()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        Rasterize(source, 'A', new GlyphRenderOptions(4)).Size.Should().Be(new VectorInt(4, 8));
    }

    [Fact]
    public void 拡大しても最近傍でドットが保たれる()
    {
        using var source = new BitmapGlyphSource(2);

        // 左上のみ不透明な 2x2 のグリフ
        var pixels = new byte[2 * 2 * 4];
        pixels[3] = 255;
        source.Register('A', pixels, (2, 2));

        var bitmap = Rasterize(source, 'A', new GlyphRenderOptions(4));

        bitmap.Size.Should().Be(new VectorInt(4, 4));
        AlphaAt(bitmap, 0, 0).Should().Be(255);
        AlphaAt(bitmap, 1, 1).Should().Be(255);
        AlphaAt(bitmap, 2, 0).Should().Be(0);
        AlphaAt(bitmap, 0, 2).Should().Be(0);
    }

    [Fact]
    public void 送り幅も拡大率に追従する()
    {
        using var source = new BitmapGlyphSource(8);
        source.Register('A', CreateSolidPixels((4, 8)), (4, 8));

        source.TryGetGlyph('A', new GlyphRenderOptions(16), out var glyph).Should().BeTrue();
        glyph.Advance.Should().Be(8);
    }

    [Fact]
    public void メトリクスは拡大率に追従する()
    {
        using var source = new BitmapGlyphSource(8);

        var normal = source.GetMetrics(new GlyphRenderOptions(8));
        var doubled = source.GetMetrics(new GlyphRenderOptions(16));

        normal.Ascender.Should().Be(8);
        normal.Descender.Should().Be(0);
        normal.LineHeight.Should().Be(8);
        doubled.LineHeight.Should().Be(16);
    }

    [Fact]
    public void ベースラインを指定するとディセンダーが生じる()
    {
        using var source = new BitmapGlyphSource(10, baseline: 8);

        var metrics = source.GetMetrics(new GlyphRenderOptions(10));

        metrics.Ascender.Should().Be(8);
        metrics.Descender.Should().Be(-2);
    }

    [Fact]
    public void 格子状の画像からフォントを生成できる()
    {
        using var stream = CreateGridImage((4, 8), columns: 3, rows: 2);
        using var source = BitmapGlyphSource.FromGrid(stream, (4, 8), "ABCDEF");

        source.GlyphCount.Should().Be(6);
        source.NativeSize.Should().Be(8);

        foreach (var c in "ABCDEF")
            source.TryGetGlyph(c, new GlyphRenderOptions(8), out _).Should().BeTrue();
    }

    [Fact]
    public void 格子状の画像から切り出したグリフは対応するセルの内容を持つ()
    {
        // 各セルの左上に、セル番号に応じた赤成分を持つ点を描く
        using var stream = CreateGridImage((4, 8), columns: 3, rows: 1, markCells: true);
        using var source = BitmapGlyphSource.FromGrid(stream, (4, 8), "ABC");

        RedAt(Rasterize(source, 'A', new GlyphRenderOptions(8)), 0, 0).Should().Be(0);
        RedAt(Rasterize(source, 'B', new GlyphRenderOptions(8)), 0, 0).Should().Be(10);
        RedAt(Rasterize(source, 'C', new GlyphRenderOptions(8)), 0, 0).Should().Be(20);
    }

    [Fact]
    public void 画像に対して文字数が多すぎると例外をスローする()
    {
        using var stream = CreateGridImage((4, 8), columns: 3, rows: 1);

        var act = () => BitmapGlyphSource.FromGrid(stream, (4, 8), "ABCD");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void カーニングは常に0を返す()
    {
        using var source = new BitmapGlyphSource(8);
        source.GetKerning('A', 'V', new GlyphRenderOptions(8)).Should().Be(0);
    }

    [Fact]
    public void 破棄後に操作すると例外をスローする()
    {
        var source = new BitmapGlyphSource(8);
        source.Dispose();

        var act = () => source.TryGetGlyph('A', new GlyphRenderOptions(8), out _);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ベクターフォントの外字として合成できる()
    {
        using var custom = new BitmapGlyphSource(16);
        custom.Register(0xE000, CreateSolidPixels((16, 16)), (16, 16));

        var font = Font.FromFile("assets/Koruri.ttf", 16).WithOverride(custom);
        var layout = TextLayoutEngine.Layout("あい", font, new TextRenderingOptions());

        layout.Glyphs.Should().HaveCount(3);
        layout.Glyphs[1].Glyph.Source.Should().BeSameAs(custom, "外字が優先して解決される");
        layout.Glyphs[0].Glyph.Source.Should().NotBeSameAs(custom);
    }

    private static byte[] CreateSolidPixels(VectorInt size)
    {
        var pixels = new byte[size.X * size.Y * 4];
        Array.Fill(pixels, (byte)255);
        return pixels;
    }

    private static GlyphBitmap Rasterize(
        IGlyphSource source,
        int codepoint,
        GlyphRenderOptions options
    )
    {
        source.TryGetGlyph(codepoint, options, out var glyph).Should().BeTrue();
        return source.Rasterize(glyph, options);
    }

    private static byte AlphaAt(GlyphBitmap bitmap, int x, int y)
    {
        return bitmap.Pixels[(((y * bitmap.Size.X) + x) * 4) + 3];
    }

    private static byte RedAt(GlyphBitmap bitmap, int x, int y)
    {
        return bitmap.Pixels[((y * bitmap.Size.X) + x) * 4];
    }

    private static MemoryStream CreateGridImage(
        VectorInt cellSize,
        int columns,
        int rows,
        bool markCells = false
    )
    {
        using var image = new Image<Rgba32>(cellSize.X * columns, cellSize.Y * rows);
        if (markCells)
        {
            for (var i = 0; i < columns * rows; i++)
            {
                var x = (i % columns) * cellSize.X;
                var y = (i / columns) * cellSize.Y;
                image[x, y] = new Rgba32((byte)(i * 10), 0, 0, 255);
            }
        }

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }
}
