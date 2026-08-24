using FluentAssertions;
using Promete.Graphics.Fonts;

namespace Promete.Test;

public class SystemFontsTests
{
    [Fact]
    public void システムフォントを列挙できる()
    {
        SystemFonts.Fonts.Should().NotBeEmpty();
        SystemFonts.Families.Should().NotBeEmpty();
    }

    [Fact]
    public void 列挙されたフォントは実在するファイルを指す()
    {
        foreach (var font in SystemFonts.Fonts.Take(20))
        {
            File.Exists(font.Path).Should().BeTrue();
            font.FamilyName.Should().NotBeNullOrEmpty();
            font.FaceIndex.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void 列挙されたファミリー名で検索できる()
    {
        var familyName = SystemFonts.Families.First();

        SystemFonts.TryGet(familyName, out var font).Should().BeTrue();
        font.FamilyName.Should().Be(familyName);
    }

    [Fact]
    public void ファミリー名の大文字小文字は区別されない()
    {
        var familyName = SystemFonts.Families.First(f => f.Any(char.IsLetter));

        SystemFonts.TryGet(familyName.ToUpperInvariant(), out _).Should().BeTrue();
        SystemFonts.TryGet(familyName.ToLowerInvariant(), out _).Should().BeTrue();
    }

    [Fact]
    public void 存在しないファミリー名は見つからない()
    {
        SystemFonts.TryGet("この名前のフォントは存在しません", out _).Should().BeFalse();
        SystemFonts.GetAll("この名前のフォントは存在しません").Should().BeEmpty();
    }

    [Fact]
    public void スタイルが存在しない場合は通常のスタイルで代替される()
    {
        // 太字を持たないファミリーを探す
        var familyName = SystemFonts
            .Families.FirstOrDefault(f =>
                SystemFonts.GetAll(f).All(info => info.Style == FontStyle.Normal)
            );

        if (familyName is null)
            return;

        SystemFonts.TryGet(familyName, FontStyle.Bold, out var font).Should().BeTrue();
        font.Style.Should().Be(FontStyle.Normal);
    }

    [Fact]
    public void 優先順位つきの検索は最初に見つかったものを返す()
    {
        var familyName = SystemFonts.Families.First();
        var candidates = new[] { "存在しないフォントA", "存在しないフォントB", familyName };

        SystemFonts.TryGetFirst(candidates, FontStyle.Normal, out var font).Should().BeTrue();
        font.FamilyName.Should().Be(familyName);
    }

    [Fact]
    public void 既定のフォントを取得できる()
    {
        var font = Font.GetDefault();

        font.Size.Should().Be(16);
        font.Metrics.LineHeight.Should().BePositive();
        font.Source.TryGetGlyph('A', font.RenderOptions, out _).Should().BeTrue();
    }

    [Fact]
    public void ファミリー名を指定してフォントを読み込める()
    {
        var familyName = SystemFonts.Families.First();

        var font = Font.FromSystem(familyName, 20);
        font.Size.Should().Be(20);
    }

    [Fact]
    public void 存在しないファミリー名を指定すると例外をスローする()
    {
        var act = () => Font.FromSystem("この名前のフォントは存在しません");
        act.Should().Throw<FontException>();
    }

    [Fact]
    public void 同じフォントから生成した場合はグリフソースを共有する()
    {
        var familyName = SystemFonts.Families.First();

        var a = Font.FromSystem(familyName, 16);
        var b = Font.FromSystem(familyName, 24);

        a.Source.Should().BeSameAs(b.Source);
    }

    [Fact]
    public void 専用の字形を持つスタイルは合成されない()
    {
        // 太字の字形を持つファミリーを探す
        var familyName = SystemFonts
            .Families.FirstOrDefault(f =>
                SystemFonts.GetAll(f).Any(info => info.Style == FontStyle.Bold)
            );

        if (familyName is null)
            return;

        var font = Font.FromSystem(familyName, 16, FontStyle.Bold);
        font.Style.Should().Be(FontStyle.Normal, "太字の字形そのものを使うため、合成は不要");
    }
}
