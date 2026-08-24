using FluentAssertions;
using Promete.Graphics.Fonts;
using Promete.Markup;

namespace Promete.Test;

public class TextLayoutAdvancedTests
{
    private static readonly Font Font = Font.FromFile("assets/Koruri.ttf", 16);

    [Fact]
    public void 禁則処理により句読点が行頭に来ない()
    {
        // 禁則処理がなければ 4 文字目の「、」が行頭に来る幅を指定する
        var options = new TextRenderingOptions
        {
            Size = (48, 0),
            WrapMode = WrapMode.Mixed,
            KinsokuMode = KinsokuMode.Standard,
        };
        var layout = TextLayoutEngine.Layout("あいう、えお", Font, options);

        foreach (var line in layout.Lines.Skip(1))
            layout.PlainText[line.StartIndex].Should().NotBe('、');
    }

    [Fact]
    public void 禁則処理を無効にすると句読点が行頭に来る()
    {
        var options = new TextRenderingOptions
        {
            Size = (48, 0),
            WrapMode = WrapMode.Mixed,
            KinsokuMode = KinsokuMode.None,
        };
        var layout = TextLayoutEngine.Layout("あいう、えお", Font, options);

        layout.PlainText[layout.Lines[1].StartIndex].Should().Be('、');
    }

    [Fact]
    public void 禁則処理により開き括弧が行末に来ない()
    {
        var options = new TextRenderingOptions
        {
            Size = (48, 0),
            WrapMode = WrapMode.Mixed,
            KinsokuMode = KinsokuMode.Standard,
        };
        var layout = TextLayoutEngine.Layout("あいう「えお」", Font, options);

        foreach (var line in layout.Lines)
        {
            if (line.Length == 0)
                continue;
            var last = layout.PlainText[line.StartIndex + line.Length - 1];
            last.Should().NotBe('「');
        }
    }

    [Fact]
    public void 行数を制限すると超過した行が切り捨てられる()
    {
        var layout = TextLayoutEngine.Layout(
            "1行目\n2行目\n3行目\n4行目",
            Font,
            new TextRenderingOptions { MaxLines = 2 }
        );

        layout.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void 行数制限で省略された場合は省略記号が付く()
    {
        var layout = TextLayoutEngine.Layout(
            "あいう\nえおか\nきくけ",
            Font,
            new TextRenderingOptions { MaxLines = 2 }
        );

        layout.Lines.Should().HaveCount(2);
        layout.Glyphs[^1].Glyph.Codepoint.Should().Be('…');
    }

    [Fact]
    public void 省略記号を空にすると付与されない()
    {
        var layout = TextLayoutEngine.Layout(
            "あいう\nえおか\nきくけ",
            Font,
            new TextRenderingOptions { MaxLines = 2, Ellipsis = "" }
        );

        layout.Lines.Should().HaveCount(2);
        layout.Glyphs[^1].Glyph.Codepoint.Should().Be('か');
    }

    [Fact]
    public void 省略記号は指定幅に収まるよう文字を削って挿入される()
    {
        var options = new TextRenderingOptions
        {
            Size = (48, 0),
            WrapMode = WrapMode.Mixed,
            MaxLines = 1,
        };
        var layout = TextLayoutEngine.Layout("あいうえおかきくけこ", Font, options);

        layout.Lines.Should().HaveCount(1);
        layout.Lines[0].Width.Should().BeLessThanOrEqualTo(48);
        layout.Glyphs[^1].Glyph.Codepoint.Should().Be('…');
    }

    [Fact]
    public void 行数制限に満たない場合は省略記号が付かない()
    {
        var layout = TextLayoutEngine.Layout(
            "あいう",
            Font,
            new TextRenderingOptions { MaxLines = 5 }
        );

        layout.Lines.Should().HaveCount(1);
        layout.Glyphs[^1].Glyph.Codepoint.Should().Be('う');
    }

    [Fact]
    public void 単独タグは置換文字1文字として解析される()
    {
        var (plainText, decorations) = PtmlParser.Parse("A<tex=icon>B");

        plainText.Should().Be($"A{PtmlParser.ObjectReplacementCharacter}B");
        decorations.Should().ContainSingle();
        decorations[0].TagName.Should().Be("tex");
        decorations[0].Attribute.Should().Be("icon");
        decorations[0].Start.Should().Be(1);
        decorations[0].End.Should().Be(2);
    }

    [Fact]
    public void 単独タグは終了タグを必要としない()
    {
        var act = () => PtmlParser.Parse("<tex=a><tex=b>", throwsIfError: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void 単独タグ以外は従来どおり終了タグを必要とする()
    {
        var (plainText, decorations) = PtmlParser.Parse("<b>A</b>");

        plainText.Should().Be("A");
        decorations.Should().ContainSingle();
        decorations[0].TagName.Should().Be("b");
    }

    [Fact]
    public void 外字タグで登録済みのグリフが差し込まれる()
    {
        using var custom = new BitmapGlyphSource(16);
        var codepoint = custom.Register("heart", CreateSolidPixels((16, 16)), (16, 16));
        var font = Font.FromFile("assets/Koruri.ttf", 16).WithOverride(custom);

        var layout = TextLayoutEngine.Layout(
            "あ<tex=heart>い",
            font,
            new TextRenderingOptions { UseRichText = true }
        );

        layout.Glyphs.Should().HaveCount(3);
        layout.Glyphs[1].Glyph.Codepoint.Should().Be(codepoint);
        layout.Glyphs[1].Glyph.Source.Should().BeSameAs(custom);
    }

    [Fact]
    public void 登録されていない名前の外字タグは無視される()
    {
        using var custom = new BitmapGlyphSource(16);
        custom.Register("heart", CreateSolidPixels((16, 16)), (16, 16));
        var font = Font.FromFile("assets/Koruri.ttf", 16).WithOverride(custom);

        var layout = TextLayoutEngine.Layout(
            "あ<tex=unknown>い",
            font,
            new TextRenderingOptions { UseRichText = true }
        );

        // 置換文字に対応するグリフが存在しないため、配置されるのは 2 文字のみ
        layout.Glyphs.Should().HaveCount(2);
    }

    [Fact]
    public void 外字は通常の文字と同じように行送りへ寄与する()
    {
        using var custom = new BitmapGlyphSource(16);
        custom.Register("wide", CreateSolidPixels((32, 16)), (32, 16));
        var font = Font.FromFile("assets/Koruri.ttf", 16).WithOverride(custom);

        var options = new TextRenderingOptions { UseRichText = true };
        var withGlyph = TextLayoutEngine.Layout("あ<tex=wide>", font, options);
        var withoutGlyph = TextLayoutEngine.Layout("あ", font, options);

        withGlyph.Size.X.Should().Be(withoutGlyph.Size.X + 32);
    }

    [Fact]
    public void 名前で登録したグリフには私用領域が割り当てられる()
    {
        using var source = new BitmapGlyphSource(16);

        var first = source.Register("a", CreateSolidPixels((8, 16)), (8, 16));
        var second = source.Register("b", CreateSolidPixels((8, 16)), (8, 16));

        first.Should().Be(0xE000);
        second.Should().Be(0xE001);
        source.TryGetCodepointByName("a", out var found).Should().BeTrue();
        found.Should().Be(first);
    }

    [Fact]
    public void 同じ名前で再登録してもコードポイントは変わらない()
    {
        using var source = new BitmapGlyphSource(16);

        var first = source.Register("a", CreateSolidPixels((8, 16)), (8, 16));
        var again = source.Register("a", CreateSolidPixels((16, 16)), (16, 16));

        again.Should().Be(first);
        source.GlyphCount.Should().Be(1);
    }

    private static byte[] CreateSolidPixels(VectorInt size)
    {
        var pixels = new byte[size.X * size.Y * 4];
        Array.Fill(pixels, (byte)255);
        return pixels;
    }
}
