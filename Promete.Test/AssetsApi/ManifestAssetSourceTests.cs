using System.Text;
using FluentAssertions;
using Promete.Assets;
using Promete.Assets.Infrastructure;

namespace Promete.Test.AssetsApi;

/// <summary>
/// マニフェストに基づくソース (<see cref="ManifestAssetSource"/>) のテスト。
/// MSBuild 統合の Folder モードで「宣言していないファイルは読めない」ことの土台になる。
/// </summary>
public sealed class ManifestAssetSourceTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void OpenRead_Declared_ShouldReadContentAndBeSeekable()
    {
        var source = Create(("assets/a.txt", _dir.CreateFile("files/a.txt", "hello")));

        using var stream = source.OpenRead("assets/a.txt");

        stream.CanSeek.Should().BeTrue();
        new StreamReader(stream).ReadToEnd().Should().Be("hello");
        stream.Position = 1;
        stream.ReadByte().Should().Be('e');
    }

    [Theory]
    [InlineData("./assets/a.txt")]
    [InlineData("/assets/a.txt")]
    [InlineData("assets/sub/../a.txt")]
    public void OpenRead_ShouldNormalizePath(string path)
    {
        var source = Create(("assets/a.txt", _dir.CreateFile("files/a.txt", "hello")));

        using var stream = source.OpenRead(path);

        new StreamReader(stream).ReadToEnd().Should().Be("hello");
    }

    [Fact]
    public void OpenRead_Backslash_ShouldThrowArgumentException()
    {
        var source = Create(("assets/a.txt", _dir.CreateFile("files/a.txt", "hello")));

        var act = () => source.OpenRead("assets\\a.txt");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OpenRead_Undeclared_ShouldThrowFileNotFound_EvenIfFileExists()
    {
        _dir.CreateFile("files/secret.txt", "secret");
        var source = Create(("assets/a.txt", _dir.CreateFile("files/a.txt", "hello")));

        var act = () => source.OpenRead("files/secret.txt");

        var ex = act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("PrometeAsset"), "宣言を案内するはず")
            .Where(e => e.Message.Contains("'test.json'"), "マニフェストの説明を含むはず")
            .Which;
        ex.Should().NotBeOfType<AssetPathCaseMismatchException>();
    }

    [Fact]
    public void OpenRead_CaseMismatch_ShouldThrowCaseMismatchException()
    {
        var source = Create(("assets/Player.png", _dir.CreateFile("files/Player.png", "p")));

        var act = () => source.OpenRead("assets/player.png");

        var ex = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        ex.RequestedPath.Should().Be("assets/player.png");
        ex.ActualPath.Should().Be("assets/Player.png");
    }

    [Fact]
    public void OpenRead_DeclaredButFileMissing_ShouldThrowFileNotFoundWithPhysicalPath()
    {
        var missing = Path.Combine(_dir.Path, "files", "missing.txt");
        var source = Create(("assets/missing.txt", missing));

        var act = () => source.OpenRead("assets/missing.txt");

        act.Should().Throw<FileNotFoundException>().Where(e => e.Message.Contains(missing));
    }

    [Fact]
    public void OpenRead_ShouldAllowDeletingTheFileWhileOpen()
    {
        var file = _dir.CreateFile("files/a.txt", "hello");
        var source = Create(("assets/a.txt", file));

        using var stream = source.OpenRead("assets/a.txt");
        var act = () => File.Delete(file);

        act.Should().NotThrow("開発中に作り直せるよう FileShare.Delete で開くはず");
    }

    [Fact]
    public void Exists_ShouldRequireDeclarationAndPhysicalFile()
    {
        _dir.CreateFile("files/undeclared.txt", "x");
        var source = Create(
            ("assets/a.txt", _dir.CreateFile("files/a.txt", "hello")),
            ("assets/missing.txt", Path.Combine(_dir.Path, "files", "missing.txt"))
        );

        source.Exists("assets/a.txt").Should().BeTrue();
        source.Exists("./assets/a.txt").Should().BeTrue();
        source.Exists("assets/missing.txt").Should().BeFalse("実ファイルが無い");
        source.Exists("files/undeclared.txt").Should().BeFalse("宣言していない");
        source.Exists("assets/A.txt").Should().BeFalse("大文字・小文字を区別する");
    }

    [Fact]
    public void Enumerate_ShouldListDeclaredPaths()
    {
        var source = Create(
            ("assets/a.txt", _dir.CreateFile("f/a.txt")),
            ("assets/b.png", _dir.CreateFile("f/b.png")),
            ("assets/sub/c.txt", _dir.CreateFile("f/c.txt")),
            ("assets/missing.txt", Path.Combine(_dir.Path, "f", "missing.txt")),
            ("other/d.txt", _dir.CreateFile("f/d.txt"))
        );

        source
            .Enumerate("assets", "*", false)
            .Should()
            .BeEquivalentTo("assets/a.txt", "assets/b.png", "assets/missing.txt");
        source
            .Enumerate("assets", "*.txt", true)
            .Should()
            .BeEquivalentTo("assets/a.txt", "assets/sub/c.txt", "assets/missing.txt");
        source.Enumerate("", "*", true).Should().HaveCount(5);
        source.Enumerate("nothing", "*", true).Should().BeEmpty();
    }

    [Fact]
    public void TryGetFilePath_ShouldReturnPhysicalPathOnlyForDeclaredExactPath()
    {
        var file = _dir.CreateFile("files/a.ttf", "font");
        var source = Create(("assets/a.ttf", file));

        source.TryGetFilePath("assets/a.ttf", out var fullPath).Should().BeTrue();
        fullPath.Should().Be(file);
        source.TryGetFilePath("assets/A.ttf", out _).Should().BeFalse();
        source.TryGetFilePath("assets/b.ttf", out _).Should().BeFalse();
    }

    [Fact]
    public void ShouldBeUsableThroughAssetFileSystem()
    {
        var file = _dir.CreateFile("files/a.ttf", "font");
        using var fs = new AssetFileSystem();
        fs.Mount(Create(("assets/a.ttf", file)));

        fs.TryGetFilePath("assets/a.ttf", out var fullPath).Should().BeTrue();
        fullPath.Should().Be(file);
        fs.Exists("assets/a.ttf").Should().BeTrue();
    }

    [Fact]
    public void Ctor_NfdKey_ShouldMatchNfcPath()
    {
        var nfd = "assets/が.txt";
        var nfc = "assets/が.txt";
        var source = Create((nfd, _dir.CreateFile("files/ga.txt", "ga")));

        using var stream = source.OpenRead(nfc);

        new StreamReader(stream, Encoding.UTF8).ReadToEnd().Should().Be("ga");
    }

    [Fact]
    public void Ctor_DuplicateAfterNormalization_ShouldThrow()
    {
        var file = _dir.CreateFile("files/a.txt");

        var act = () => Create(("assets/a.txt", file), ("./assets/a.txt", file));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_KeysDifferingOnlyInCase_ShouldBeSeparateEntries()
    {
        var source = Create(
            ("assets/a.txt", _dir.CreateFile("files/lower.txt", "lower")),
            ("assets/A.txt", _dir.CreateFile("files/upper.txt", "upper"))
        );

        new StreamReader(source.OpenRead("assets/a.txt")).ReadToEnd().Should().Be("lower");
        new StreamReader(source.OpenRead("assets/A.txt")).ReadToEnd().Should().Be("upper");
    }

    [Fact]
    public void Ctor_InvalidLogicalPath_ShouldThrow()
    {
        var file = _dir.CreateFile("files/a.txt");

        var act = () => Create(("assets\\a.txt", file));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_RelativeOrNullPhysicalPath_ShouldThrow()
    {
        var relative = () => Create(("assets/a.txt", "files/a.txt"));
        var nullPath = () => Create(("assets/a.txt", null!));

        relative.Should().Throw<ArgumentException>();
        nullPath.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_NullArguments_ShouldThrowArgumentNull()
    {
        var nullFiles = () => new ManifestAssetSource(null!, "x");
        var nullDescription = () =>
            new ManifestAssetSource(new Dictionary<string, string>(), null!);

        nullFiles.Should().Throw<ArgumentNullException>();
        nullDescription.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Empty_ShouldFindNothing()
    {
        var source = new ManifestAssetSource(new Dictionary<string, string>(), "'empty'");

        source.Exists("a").Should().BeFalse();
        source.Enumerate("", "*", true).Should().BeEmpty();
        var act = () => source.OpenRead("a");
        act.Should().Throw<FileNotFoundException>();
    }

    private static ManifestAssetSource Create(params (string Logical, string Physical)[] files) =>
        new(
            files.Select(f => new KeyValuePair<string, string>(f.Logical, f.Physical)),
            "'test.json'"
        );
}
