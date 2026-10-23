using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="EmbeddedResourceAssetSource"/> のテスト。リソースは Promete.Test.csproj で LogicalName を付けて埋め込んである。
/// </summary>
/// <remarks>
/// 埋め込んであるリソース: <c>embedded/hello.txt</c> ("hello")、<c>embedded/sub/nested.txt</c> ("nested")、
/// <c>embedded/Upper.txt</c> ("upper")、<c>other/ignored.txt</c> ("ignored")。
/// </remarks>
public class EmbeddedResourceAssetSourceTests
{
    private static readonly System.Reflection.Assembly TestAssembly =
        typeof(EmbeddedResourceAssetSourceTests).Assembly;

    [Fact]
    public void Constructor_NullAssembly_ShouldThrowArgumentNull()
    {
        var act = () => new EmbeddedResourceAssetSource(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_MountPointWithBackslash_ShouldThrow()
    {
        var act = () => new EmbeddedResourceAssetSource(TestAssembly, "", "a\\b");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithoutPrefix_ShouldUseResourceNameAsLogicalPath()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly);

        source.Exists("embedded/hello.txt").Should().BeTrue();
        source.Exists("other/ignored.txt").Should().BeTrue();
        source
            .Exists("/embedded/./sub/nested.txt")
            .Should()
            .BeTrue("パスを正規化してから照合するはず");
        using var stream = source.OpenRead("embedded/hello.txt");
        stream.ReadAllText().Should().Be("hello");
    }

    [Fact]
    public void WithPrefixAndMountPoint_ShouldMapLogicalPaths()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly, "embedded/", "res");

        source.Exists("res/hello.txt").Should().BeTrue();
        source.Exists("res/sub/nested.txt").Should().BeTrue();
        source.Exists("embedded/hello.txt").Should().BeFalse();
        source
            .Exists("res/ignored.txt")
            .Should()
            .BeFalse("接頭辞で始まらないリソースは対象外のはず");
        using var stream = source.OpenRead("res/sub/nested.txt");
        stream.ReadAllText().Should().Be("nested");
    }

    [Fact]
    public void OpenRead_ShouldReturnSeekableStream()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly);

        using var stream = source.OpenRead("embedded/hello.txt");

        stream.CanSeek.Should().BeTrue();
        stream.Seek(1, SeekOrigin.Begin);
        stream.ReadByte().Should().Be('e');
    }

    [Fact]
    public void OpenRead_Missing_ShouldThrowFileNotFound()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly);

        var act = () => source.OpenRead("embedded/missing.txt");

        act.Should()
            .Throw<FileNotFoundException>()
            .Which.Should()
            .NotBeOfType<AssetPathCaseMismatchException>();
    }

    [Fact]
    public void CaseMismatch_ShouldNotExistAndThrowWithActualPath()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly, "embedded/", "res");

        source.Exists("res/upper.txt").Should().BeFalse();
        var act = () => source.OpenRead("res/upper.txt");

        var ex = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        ex.ActualPath.Should().Be("res/Upper.txt");
        ex.Message.Should().Contain("大文字・小文字を区別");
    }

    [Fact]
    public void Enumerate_ShouldTreatSlashAsDirectory()
    {
        var source = new EmbeddedResourceAssetSource(TestAssembly, "embedded/", "res");

        source
            .Enumerate("res", "*", false)
            .Should()
            .BeEquivalentTo("res/hello.txt", "res/Upper.txt");
        source
            .Enumerate("res", "*", true)
            .Should()
            .BeEquivalentTo("res/hello.txt", "res/Upper.txt", "res/sub/nested.txt");
        source.Enumerate("res/sub", "*.txt", false).Should().BeEquivalentTo("res/sub/nested.txt");
        source.Enumerate("", "h*", true).Should().BeEquivalentTo("res/hello.txt");
        source.Enumerate("missing", "*", true).Should().BeEmpty();
    }
}
