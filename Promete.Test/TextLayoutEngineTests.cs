using System.Drawing;
using FluentAssertions;
using Promete.Graphics.Fonts;

namespace Promete.Test;

public class TextLayoutEngineTests
{
    private static readonly Font Font = Font.FromFile("assets/Koruri.ttf", 16);

    [Fact]
    public void 空文字列は空のレイアウトになる()
    {
        var layout = TextLayoutEngine.Layout("", Font, new TextRenderingOptions());

        layout.Should().BeSameAs(TextLayout.Empty);
        layout.Glyphs.Should().BeEmpty();
        layout.Size.Should().Be(VectorInt.Zero);
    }

    [Fact]
    public void 単一行のテキストは1行としてレイアウトされる()
    {
        var layout = TextLayoutEngine.Layout("Hello", Font, new TextRenderingOptions());

        layout.Lines.Should().HaveCount(1);
        layout.Glyphs.Should().HaveCount(5);
        layout.PlainText.Should().Be("Hello");
        layout.Size.X.Should().BePositive();
        layout.Size.Y.Should().BePositive();
    }

    [Fact]
    public void 改行文字で行が分割される()
    {
        var layout = TextLayoutEngine.Layout("あい\nうえお", Font, new TextRenderingOptions());

        layout.Lines.Should().HaveCount(2);
        layout.Lines[0].Length.Should().Be(2);
        layout.Lines[1].Length.Should().Be(3);
        layout.Glyphs.Should().HaveCount(5, "改行文字自体はグリフを持たない");
    }

    [Fact]
    public void CRLFは1つの改行として扱われる()
    {
        var layout = TextLayoutEngine.Layout("A\r\nB", Font, new TextRenderingOptions());

        layout.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void 行数に応じて高さが増える()
    {
        var options = new TextRenderingOptions();
        var single = TextLayoutEngine.Layout("A", Font, options);
        var triple = TextLayoutEngine.Layout("A\nA\nA", Font, options);

        triple.Size.Y.Should().Be(single.Size.Y * 3);
    }

    [Fact]
    public void 行間を広げると高さが増える()
    {
        var normal = TextLayoutEngine.Layout("A\nA", Font, new TextRenderingOptions());
        var wide = TextLayoutEngine.Layout(
            "A\nA",
            Font,
            new TextRenderingOptions { LineSpacing = 2 }
        );

        wide.Size.Y.Should().BeGreaterThan(normal.Size.Y);
    }

    [Fact]
    public void 文字間を広げると幅が増える()
    {
        var normal = TextLayoutEngine.Layout("AAA", Font, new TextRenderingOptions());
        var wide = TextLayoutEngine.Layout(
            "AAA",
            Font,
            new TextRenderingOptions { LetterSpacing = 4 }
        );

        wide.Size.X.Should().Be(normal.Size.X + 12);
    }

    [Fact]
    public void 折り返しを行わない場合は幅を超えても改行されない()
    {
        var layout = TextLayoutEngine.Layout(
            "あいうえおかきくけこ",
            Font,
            new TextRenderingOptions { Size = (32, 0), WrapMode = WrapMode.None }
        );

        layout.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void 文字単位の折り返しは単語の途中でも改行する()
    {
        var layout = TextLayoutEngine.Layout(
            "abcdefghij",
            Font,
            new TextRenderingOptions { Size = (32, 0), WrapMode = WrapMode.Character }
        );

        layout.Lines.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void 単語単位の折り返しは空白で改行する()
    {
        var layout = TextLayoutEngine.Layout(
            "hello world promete",
            Font,
            new TextRenderingOptions { Size = (80, 0), WrapMode = WrapMode.Word }
        );

        layout.Lines.Count.Should().BeGreaterThan(1);

        // 行頭が単語の途中になっていないことを確認する
        foreach (var line in layout.Lines.Skip(1))
        {
            var head = layout.PlainText[line.StartIndex];
            head.Should().NotBe('o', "単語の途中で改行されている");
        }
    }

    [Fact]
    public void 単語単位の折り返しでも1単語が幅を超える場合は強制的に改行する()
    {
        var layout = TextLayoutEngine.Layout(
            "abcdefghijklmnop",
            Font,
            new TextRenderingOptions { Size = (32, 0), WrapMode = WrapMode.Word }
        );

        layout.Lines.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void 混在モードでは和文が任意の位置で改行される()
    {
        var layout = TextLayoutEngine.Layout(
            "あいうえおかきくけこ",
            Font,
            new TextRenderingOptions { Size = (48, 0), WrapMode = WrapMode.Mixed }
        );

        layout.Lines.Count.Should().BeGreaterThan(1);
        layout.Lines[0].Length.Should().Be(3, "16px の全角文字が 3 文字で 48px になる");
    }

    [Fact]
    public void 混在モードでは欧文の単語が分割されない()
    {
        // 単語がちょうど 1 行に収まり、和文と同居はできない幅を用意する
        var wordWidth = TextLayoutEngine.Measure("promete", Font, new TextRenderingOptions()).X;
        var layout = TextLayoutEngine.Layout(
            "あいうpromete",
            Font,
            new TextRenderingOptions { Size = (wordWidth + 16, 0), WrapMode = WrapMode.Mixed }
        );

        var secondLine = layout.PlainText.Substring(
            layout.Lines[1].StartIndex,
            layout.Lines[1].Length
        );
        secondLine.Should().Be("promete");
    }

    [Fact]
    public void 中央揃えではグリフが中央に寄る()
    {
        var options = new TextRenderingOptions
        {
            Size = (200, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var layout = TextLayoutEngine.Layout("A", Font, options);

        layout.Glyphs[0].Position.X.Should().BeGreaterThan(50);
    }

    [Fact]
    public void 右揃えではグリフが右に寄る()
    {
        var left = TextLayoutEngine.Layout(
            "A",
            Font,
            new TextRenderingOptions { Size = (200, 0) }
        );
        var right = TextLayoutEngine.Layout(
            "A",
            Font,
            new TextRenderingOptions
            {
                Size = (200, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
            }
        );

        right.Glyphs[0].Position.X.Should().BeGreaterThan(left.Glyphs[0].Position.X);
    }

    [Fact]
    public void 下揃えではグリフが下に寄る()
    {
        var top = TextLayoutEngine.Layout("A", Font, new TextRenderingOptions { Size = (0, 200) });
        var bottom = TextLayoutEngine.Layout(
            "A",
            Font,
            new TextRenderingOptions
            {
                Size = (0, 200),
                VerticalAlignment = VerticalAlignment.Bottom,
            }
        );

        bottom.Glyphs[0].Position.Y.Should().BeGreaterThan(top.Glyphs[0].Position.Y);
    }

    [Fact]
    public void 既定の色がグリフに適用される()
    {
        var layout = TextLayoutEngine.Layout(
            "A",
            Font,
            new TextRenderingOptions { TextColor = Color.Red }
        );

        layout.Glyphs[0].Color.Should().Be(Color.Red);
    }

    [Fact]
    public void リッチテキストが無効ならタグは文字として扱われる()
    {
        var layout = TextLayoutEngine.Layout("<b>A</b>", Font, new TextRenderingOptions());

        layout.PlainText.Should().Be("<b>A</b>");
    }

    [Fact]
    public void リッチテキストのcolorタグで色が変わる()
    {
        var layout = TextLayoutEngine.Layout(
            "A<color=#ff0000>B</color>C",
            Font,
            new TextRenderingOptions { TextColor = Color.White, UseRichText = true }
        );

        layout.PlainText.Should().Be("ABC");
        layout.Glyphs[0].Color.Should().Be(Color.White);
        layout.Glyphs[1].Color.R.Should().Be(255);
        layout.Glyphs[1].Color.G.Should().Be(0);
        layout.Glyphs[2].Color.Should().Be(Color.White);
    }

    [Fact]
    public void リッチテキストのsizeタグでサイズが変わる()
    {
        var layout = TextLayoutEngine.Layout(
            "A<size=32>B</size>",
            Font,
            new TextRenderingOptions { UseRichText = true }
        );

        layout.Glyphs[0].Options.Size.Should().Be(16);
        layout.Glyphs[1].Options.Size.Should().Be(32);
        layout.Glyphs[1].Glyph.Advance.Should().BeGreaterThan(layout.Glyphs[0].Glyph.Advance);
    }

    [Fact]
    public void リッチテキストのbタグとiタグでスタイルが変わる()
    {
        var layout = TextLayoutEngine.Layout(
            "<b>A</b><i>B</i><b><i>C</i></b>",
            Font,
            new TextRenderingOptions { UseRichText = true }
        );

        layout.Glyphs[0].Options.Style.Should().Be(FontStyle.Bold);
        layout.Glyphs[1].Options.Style.Should().Be(FontStyle.Italic);
        layout.Glyphs[2].Options.Style.Should().Be(FontStyle.BoldItalic);
    }

    [Fact]
    public void サイズの異なる文字を含む行は高い方に合わせられる()
    {
        var normal = TextLayoutEngine.Layout(
            "AB",
            Font,
            new TextRenderingOptions { UseRichText = true }
        );
        var mixed = TextLayoutEngine.Layout(
            "A<size=32>B</size>",
            Font,
            new TextRenderingOptions { UseRichText = true }
        );

        mixed.Size.Y.Should().BeGreaterThan(normal.Size.Y);
    }

    [Fact]
    public void 収録されていない文字は配置されない()
    {
        var layout = TextLayoutEngine.Layout("AB", Font, new TextRenderingOptions());

        layout.PlainText.Should().Be("AB");
        layout.Glyphs.Should().HaveCount(2);
    }

    [Fact]
    public void グリフは元の文字位置を保持する()
    {
        var layout = TextLayoutEngine.Layout("あ\nい", Font, new TextRenderingOptions());

        layout.Glyphs[0].CharIndex.Should().Be(0);
        layout.Glyphs[1].CharIndex.Should().Be(2);
        layout.Glyphs[0].LineIndex.Should().Be(0);
        layout.Glyphs[1].LineIndex.Should().Be(1);
    }

    [Fact]
    public void ベースラインは行の上端より下にある()
    {
        var layout = TextLayoutEngine.Layout("A", Font, new TextRenderingOptions());

        layout.Lines[0].Baseline.Should().BePositive();
        layout.Glyphs[0].Position.Y.Should().Be(layout.Lines[0].Baseline);
    }

    [Fact]
    public void Measureはレイアウト結果のサイズと一致する()
    {
        var options = new TextRenderingOptions();
        var expected = TextLayoutEngine.Layout("Promete", Font, options).Size;

        TextLayoutEngine.Measure("Promete", Font, options).Should().Be(expected);
    }

    [Fact]
    public void HitTestは指定位置に最も近い文字を返す()
    {
        var layout = TextLayoutEngine.Layout("ABC", Font, new TextRenderingOptions());
        var second = layout.Glyphs[1];

        layout.HitTest((second.Position.X + 1, second.Position.Y)).Should().Be(1);
    }
}
