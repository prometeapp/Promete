using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="EmbeddedResourceAssetSource"/> のリソース名と接頭辞の扱いのテスト。名前を自由に決めるため <see cref="FakeResourceAssembly"/> を使う。
/// </summary>
public class EmbeddedResourceAssetSourceNamingTests
{
    private const string NfdPa = "パ";
    private const string NfcPa = "パ";

    [Theory]
    [InlineData("pre")]
    [InlineData("pre/")]
    public void Prefix_ShouldBeTreatedAsDirectory(string prefix)
    {
        var assembly = new FakeResourceAssembly(
            ("pre/a.txt", "a"),
            ("pre/sub/b.txt", "b"),
            ("prefix2/c.txt", "c"),
            ("other/d.txt", "d")
        );

        var source = new EmbeddedResourceAssetSource(assembly, prefix, "res");

        source
            .Exists("res/a.txt")
            .Should()
            .BeTrue("末尾の / の有無に関係なく、接頭辞はディレクトリとして扱うはず");
        source.Exists("res/sub/b.txt").Should().BeTrue();
        source
            .Enumerate("", "*", true)
            .Should()
            .BeEquivalentTo(["res/a.txt", "res/sub/b.txt"], "prefix2/ は pre の下ではないはず");
    }

    [Fact]
    public void NamesWithLeadingSlashOrDot_ShouldBeNormalizedAndAccepted()
    {
        var assembly = new FakeResourceAssembly(("/lead/a.txt", "a"), ("./dot/b.txt", "b"));

        var source = new EmbeddedResourceAssetSource(assembly);

        source.Exists("lead/a.txt").Should().BeTrue();
        source.Exists("dot/b.txt").Should().BeTrue();
        using var stream = source.OpenRead("dot/b.txt");
        stream.ReadAllText().Should().Be("b");
    }

    [Fact]
    public void NamesAfterPrefix_WithLeadingSlash_ShouldBeAccepted()
    {
        var assembly = new FakeResourceAssembly(("pre//a.txt", "a"));

        var source = new EmbeddedResourceAssetSource(assembly, "pre");

        source.Exists("a.txt").Should().BeTrue();
    }

    [Fact]
    public void NfdName_ShouldBeNormalizedToNfc()
    {
        var assembly = new FakeResourceAssembly(($"nfd/{NfdPa}.txt", "pa"));

        var source = new EmbeddedResourceAssetSource(assembly);

        source
            .Exists($"nfd/{NfcPa}.txt")
            .Should()
            .BeTrue("NFD のリソース名も黙って無視せず、NFC で読めるはず");
        source.Enumerate("nfd", "*", false).Should().Equal($"nfd/{NfcPa}.txt");
        using var stream = source.OpenRead($"nfd/{NfcPa}.txt");
        stream.ReadAllText().Should().Be("pa");
    }

    [Fact]
    public void NameWithBackslash_MatchingPrefix_ShouldThrowAndSuggestSlash()
    {
        // Windows の %(RelativeDir) をそのまま LogicalName に使うと \ が入る
        var assembly = new FakeResourceAssembly(("pre/sub\\a.txt", "a"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "pre");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*/*")
            .And.Message.Should()
            .Contain("sub\\a.txt");
    }

    [Fact]
    public void NameWithBackslash_NotMatchingPrefix_ShouldBeIgnored()
    {
        var assembly = new FakeResourceAssembly(("pre/a.txt", "a"), ("other\\b.txt", "b"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "pre");

        act.Should().NotThrow("接頭辞に一致しないリソースは検査しないはず");
    }

    [Fact]
    public void NamesCollidingAfterNormalization_ShouldThrow()
    {
        var assembly = new FakeResourceAssembly(("c/a.txt", "1"), ("c/./a.txt", "2"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "c");

        act.Should().Throw<ArgumentException>().WithMessage("*a.txt*");
    }

    [Fact]
    public void PrefixWithBackslash_ShouldThrow()
    {
        var assembly = new FakeResourceAssembly(("pre/a.txt", "a"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "pre\\");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(@"pre\a.txt")]
    [InlineData(@"pre\sub\a.txt")]
    [InlineData(@"/pre\a.txt")]
    public void NameWithBackslashRightAfterPrefix_ShouldThrow(string resourceName)
    {
        // %(RelativeDir) を使うと、接頭辞の直後の区切りも \ になる。接頭辞の照合で落とさず、例外にするはず
        var assembly = new FakeResourceAssembly((resourceName, "a"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "pre");

        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain(resourceName);
    }

    [Fact]
    public void NameWithBackslash_SimilarButDifferentPrefix_ShouldBeIgnored()
    {
        var assembly = new FakeResourceAssembly(("pre/a.txt", "a"), (@"prefix\b.txt", "b"));

        var act = () => new EmbeddedResourceAssetSource(assembly, "pre");

        act.Should().NotThrow(@"\ を / に読み替えても接頭辞 pre/ に一致しない名前は対象外のはず");
    }
}
