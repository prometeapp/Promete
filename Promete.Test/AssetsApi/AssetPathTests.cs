using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 論理パスの正規化とパターン照合 (<see cref="AssetPath"/>) のテスト。
/// </summary>
public class AssetPathTests
{
    // ハ (U+30CF) + 半濁点 (U+309A) は NFD。NFC では パ (U+30D1) になる
    private const string NfdPa = "パ";
    private const string NfcPa = "パ";

    [Theory]
    [InlineData("a.png", "a.png")]
    [InlineData("a/b.png", "a/b.png")]
    [InlineData("/a/b.png", "a/b.png")]
    [InlineData("a//b.png", "a/b.png")]
    [InlineData("a/b.png/", "a/b.png")]
    [InlineData("./a/./b.png", "a/b.png")]
    [InlineData("a/../b.png", "b.png")]
    [InlineData("a/b/../../c.png", "c.png")]
    [InlineData("assets/x.png", "assets/x.png")]
    [InlineData("./assets/x.png", "assets/x.png")]
    public void Normalize_ShouldResolveSeparatorsAndDots(string input, string expected)
    {
        AssetPath.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("../a.png", "a.png")]
    [InlineData("a/../../b.png", "b.png")]
    [InlineData("/../../a/b.png", "a/b.png")]
    public void Normalize_DotDotAboveRoot_ShouldStopAtRoot(string input, string expected)
    {
        AssetPath
            .Normalize(input)
            .Should()
            .Be(expected, "マウントの外に出られないよう、ルートで止まるはず");
    }

    [Theory]
    [InlineData("a\\b.png")]
    [InlineData("\\a.png")]
    [InlineData("a/b\\c.png")]
    public void Normalize_WithBackslash_ShouldThrow(string input)
    {
        var act = () => AssetPath.Normalize(input);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/..")]
    public void Normalize_WhenResultIsEmpty_ShouldThrow(string input)
    {
        var act = () => AssetPath.Normalize(input);

        act.Should().Throw<ArgumentException>("ファイルのパスとして空は使えないはず");
    }

    [Fact]
    public void Normalize_Null_ShouldThrowArgumentNull()
    {
        var act = () => AssetPath.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Normalize_ShouldBeCaseSensitive()
    {
        AssetPath
            .Normalize("Assets/Player.PNG")
            .Should()
            .Be("Assets/Player.PNG", "大文字・小文字は変えないはず");
    }

    [Fact]
    public void Normalize_NfdPath_ShouldBecomeNfc()
    {
        var normalized = AssetPath.Normalize($"assets/{NfdPa}.png");

        normalized.Should().Be($"assets/{NfcPa}.png");
    }

    [Fact]
    public void Normalize_ShouldBeIdempotent()
    {
        var once = AssetPath.Normalize($"/./a/../{NfdPa}//b.png");

        AssetPath.Normalize(once).Should().Be(once);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("/", "")]
    [InlineData(".", "")]
    [InlineData("..", "")]
    [InlineData("a", "a")]
    [InlineData("/a/b/", "a/b")]
    [InlineData("a/./b/../c", "a/c")]
    public void NormalizeDirectory_ShouldAllowRoot(string input, string expected)
    {
        AssetPath.NormalizeDirectory(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeDirectory_WithBackslash_ShouldThrow()
    {
        var act = () => AssetPath.NormalizeDirectory("a\\b");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizeDirectory_Null_ShouldThrowArgumentNull()
    {
        var act = () => AssetPath.NormalizeDirectory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("a.png", "*.png", true)]
    [InlineData("a.png", "*", true)]
    [InlineData("a.png", "a.png", true)]
    [InlineData("a.png", "?.png", true)]
    [InlineData("ab.png", "?.png", false)]
    [InlineData("a.png", "*.p?g", true)]
    [InlineData("a.png", "*.jpg", false)]
    [InlineData(".png", "*.png", true)]
    [InlineData("a+b(1).png", "a+b(1).png", true)]
    [InlineData("a.png", "a.pn", false)]
    [InlineData("xa.png", "a.png", false)]
    public void IsMatch_ShouldSupportWildcards(string name, string pattern, bool expected)
    {
        AssetPath.IsMatch(name, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.PNG", "*.png")]
    [InlineData("A.png", "a.png")]
    public void IsMatch_ShouldBeCaseSensitive(string name, string pattern)
    {
        AssetPath.IsMatch(name, pattern).Should().BeFalse("パターンは大文字・小文字を区別するはず");
    }

    [Fact]
    public void IsMatch_NfdPattern_ShouldMatchNfcName()
    {
        AssetPath
            .IsMatch($"{NfcPa}.png", $"{NfdPa}.png")
            .Should()
            .BeTrue("パターンも NFC に揃えて比較するはず");
    }

    [Theory]
    [InlineData("a/*.png")]
    [InlineData("a\\*.png")]
    public void IsMatch_PatternWithSeparator_ShouldThrow(string pattern)
    {
        var act = () => AssetPath.IsMatch("a.png", pattern);

        act.Should().Throw<ArgumentException>();
    }
}
