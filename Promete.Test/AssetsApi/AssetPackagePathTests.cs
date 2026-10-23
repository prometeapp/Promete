using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// ライター側の論理パスの正規化 <see cref="AssetPackagePath.Normalize"/> のテスト。実行時の <see cref="AssetPath.Normalize"/> と結果が揃うこと。
/// </summary>
public class AssetPackagePathTests
{
    private const string NfcGa = "が"; // が (NFC)
    private const string NfdGa = "\u304B\u3099"; // が (NFD)

    public static TheoryData<string> EquivalenceInputs =>
        new()
        {
            "a.png",
            "/a.png",
            "///a.png",
            "./a.png",
            "a/./b.png",
            "a//b.png",
            "a/b/../c.png",
            "../a.png",
            "a/../../b.png",
            "a/b/",
            "Assets/Player.PNG",
            "dir.with.dot/file",
            "日本語/" + NfdGa + ".png",
            "日本語/" + NfcGa + ".png",
            " space /x y.txt",
            "a/.../b",
        };

    [Theory]
    [InlineData("a.png", "a.png")]
    [InlineData("/a.png", "a.png")]
    [InlineData("./a/./b.png", "a/b.png")]
    [InlineData("a//b.png", "a/b.png")]
    [InlineData("a/x/../b.png", "a/b.png")]
    [InlineData("../a.png", "a.png")]
    [InlineData("Assets/Player.PNG", "Assets/Player.PNG")]
    public void Normalize_ShouldResolveSegments(string input, string expected)
    {
        AssetPackagePath.Normalize(input).Should().Be(expected);
    }

    [Fact]
    public void Normalize_ShouldConvertNfdToNfc()
    {
        AssetPackagePath.Normalize("dir/" + NfdGa + ".png").Should().Be("dir/" + NfcGa + ".png");
    }

    [Theory]
    [MemberData(nameof(EquivalenceInputs))]
    public void Normalize_ShouldMatchRuntimeAssetPath(string input)
    {
        AssetPackagePath.Normalize(input).Should().Be(AssetPath.Normalize(input));
    }

    [Theory]
    [InlineData("a\\b.png")]
    [InlineData("\\a.png")]
    public void Normalize_Backslash_ShouldThrow(string input)
    {
        var act = () => AssetPackagePath.Normalize(input);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData(".")]
    [InlineData("a/..")]
    [InlineData("//./")]
    public void Normalize_EmptyResult_ShouldThrow(string input)
    {
        var act = () => AssetPackagePath.Normalize(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Normalize_Null_ShouldThrowArgumentNull()
    {
        var act = () => AssetPackagePath.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsUnicodeNormalizationSupported_ShouldBeTrueInTestEnvironment()
    {
        // テストは InvariantGlobalization を有効にしていない
        AssetPackagePath.IsUnicodeNormalizationSupported.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(EquivalenceInputs))]
    public void Normalize_WhenSupported_ShouldMatchPublicNormalize(string input)
    {
        AssetPackagePath.Normalize(input, true).Should().Be(AssetPackagePath.Normalize(input));
    }

    [Theory]
    [InlineData("/a/./b.png", "a/b.png")]
    [InlineData("Assets/x/../Player.PNG", "Assets/Player.PNG")]
    [InlineData(" space /x y.txt", " space /x y.txt")]
    public void Normalize_WhenUnsupported_AsciiPath_ShouldStillNormalize(
        string input,
        string expected
    )
    {
        AssetPackagePath.Normalize(input, false).Should().Be(expected);
    }

    [Theory]
    [InlineData("が.png")]
    [InlineData("dir/が.png")]
    [InlineData("café/a.txt")]
    public void Normalize_WhenUnsupported_NonAsciiPath_ShouldThrowWithReason(string input)
    {
        // 正規化が効かない環境で黙って書き出すと、NFD のパスがそのまま入り、通常の環境のリーダーがパッケージ全体を開けなくなる
        var act = () => AssetPackagePath.Normalize(input, false);

        act.Should().Throw<ArgumentException>().WithMessage("*InvariantGlobalization*");
    }

    [Fact]
    public void Normalize_WhenUnsupported_InvalidArguments_ShouldThrowAsUsual()
    {
        ((Action)(() => AssetPackagePath.Normalize(null!, false)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => AssetPackagePath.Normalize("a\\b.png", false)))
            .Should()
            .Throw<ArgumentException>()
            .Which.Message.Should()
            .NotContain("InvariantGlobalization");
        ((Action)(() => AssetPackagePath.Normalize("a/..", false)))
            .Should()
            .Throw<ArgumentException>();
    }
}
