using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="FolderAssetSource"/> の基本的な読み出しのテスト。
/// </summary>
public sealed class FolderAssetSourceTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Constructor_NonexistentFolder_ShouldThrowDirectoryNotFound()
    {
        var path = Path.Combine(_dir.Path, "missing");

        var act = () => new FolderAssetSource(path);

        act.Should().Throw<DirectoryNotFoundException>("黙って空のソースにしないはず");
    }

    [Fact]
    public void Constructor_Null_ShouldThrowArgumentNull()
    {
        var act = () => new FolderAssetSource(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_MountPointWithBackslash_ShouldThrow()
    {
        var act = () => new FolderAssetSource(_dir.Path, "a\\b");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Properties_ShouldBeNormalized()
    {
        var source = new FolderAssetSource(_dir.Path, "/assets/");

        source.RootPath.Should().Be(Path.GetFullPath(_dir.Path));
        source.MountPoint.Should().Be("assets");
    }

    [Fact]
    public void RelativeRootPath_ShouldResolveFromAppContextBaseDirectory()
    {
        // Promete.Test/assets は出力先にコピーされている。カレントディレクトリを別の場所にしても読めること
        var originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _dir.Path;

            var source = new FolderAssetSource("assets");

            source.RootPath.Should().Be(Path.Combine(AppContext.BaseDirectory, "assets"));
            source.Exists("Koruri.ttf").Should().BeTrue();
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void ExistsAndOpenRead_ShouldReadFile()
    {
        _dir.CreateFile("a.txt", "hello");
        _dir.CreateFile("sub/b.txt", "world");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists("a.txt").Should().BeTrue();
        source.Exists("sub/b.txt").Should().BeTrue();
        source.Exists("missing.txt").Should().BeFalse();
        using (var stream = source.OpenRead("a.txt"))
            stream.ReadAllText().Should().Be("hello");
        using (var stream = source.OpenRead("sub/b.txt"))
            stream.ReadAllText().Should().Be("world");
    }

    [Fact]
    public void ExistsAndOpenRead_ShouldNormalizePath()
    {
        _dir.CreateFile("sub/b.txt", "world");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists("/sub/./x/../b.txt").Should().BeTrue();
        using var stream = source.OpenRead("./sub/b.txt");
        stream.ReadAllText().Should().Be("world");
    }

    [Fact]
    public void Exists_PathWithBackslash_ShouldThrow()
    {
        _dir.CreateFile("sub/b.txt");
        var source = new FolderAssetSource(_dir.Path);

        var act = () => source.Exists("sub\\b.txt");

        act.Should().Throw<ArgumentException>("Windows でだけ通ってしまうのを防ぐはず");
    }

    [Fact]
    public void Exists_Directory_ShouldBeFalse()
    {
        _dir.CreateFile("sub/b.txt");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists("sub").Should().BeFalse("ディレクトリはファイルとして扱わないはず");
    }

    [Fact]
    public void OpenRead_Missing_ShouldThrowFileNotFound()
    {
        var source = new FolderAssetSource(_dir.Path);

        var act = () => source.OpenRead("missing.txt");

        act.Should()
            .Throw<FileNotFoundException>()
            .Which.Should()
            .NotBeOfType<AssetPathCaseMismatchException>();
    }

    [Fact]
    public void OpenRead_Directory_ShouldThrowFileNotFound()
    {
        _dir.CreateFile("sub/b.txt");
        var source = new FolderAssetSource(_dir.Path);

        var act = () => source.OpenRead("sub");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void OpenRead_ShouldReturnSeekableStream()
    {
        _dir.CreateFile("a.txt", "0123456789");
        var source = new FolderAssetSource(_dir.Path);

        using var stream = source.OpenRead("a.txt");

        stream.CanSeek.Should().BeTrue("NVorbis は seek できないストリームを受け付けないため");
        stream.Seek(5, SeekOrigin.Begin);
        stream.ReadByte().Should().Be('5');
    }

    [Fact]
    public void OpenRead_ShouldAllowConcurrentStreamsOfSameFile()
    {
        _dir.CreateFile("a.txt", "abc");
        var source = new FolderAssetSource(_dir.Path);

        using var first = source.OpenRead("a.txt");
        using var second = source.OpenRead("a.txt");

        first.ReadAllText().Should().Be("abc");
        second.ReadAllText().Should().Be("abc");
    }

    [Fact]
    public void DotDot_ShouldNotEscapeRoot()
    {
        _dir.CreateFile("outside.txt", "secret");
        _dir.CreateFile("root/inside.txt", "ok");
        var source = new FolderAssetSource(Path.Combine(_dir.Path, "root"));

        source.Exists("../outside.txt").Should().BeFalse("ルートより上には出られないはず");
        source.Exists("../inside.txt").Should().BeTrue("ルートで止まるので inside.txt を指すはず");
    }

    [Fact]
    public void MountPoint_ShouldPrefixLogicalPaths()
    {
        _dir.CreateFile("x.png", "x");
        _dir.CreateFile("sub/y.png", "y");
        var source = new FolderAssetSource(_dir.Path, "assets");

        source.Exists("assets/x.png").Should().BeTrue();
        source.Exists("assets/sub/y.png").Should().BeTrue();
        source.Exists("x.png").Should().BeFalse("接頭辞の外には何も無いはず");
        source.Exists("assets").Should().BeFalse();
        using var stream = source.OpenRead("/assets/sub/y.png");
        stream.ReadAllText().Should().Be("y");
    }

    [Fact]
    public void MountPoint_Nested_ShouldWork()
    {
        _dir.CreateFile("x.png", "x");
        var source = new FolderAssetSource(_dir.Path, "a/b");

        source.Exists("a/b/x.png").Should().BeTrue();
        source.Exists("a/x.png").Should().BeFalse();
    }

    [Fact]
    public void TryGetFilePath_ShouldReturnPhysicalFullPath()
    {
        var expected = _dir.CreateFile("sub/a.txt");
        var source = new FolderAssetSource(_dir.Path, "assets");

        source.TryGetFilePath("assets/sub/a.txt", out var fullPath).Should().BeTrue();
        fullPath.Should().Be(Path.GetFullPath(expected));
    }

    [Fact]
    public void TryGetFilePath_Missing_ShouldReturnFalse()
    {
        var source = new FolderAssetSource(_dir.Path);

        source.TryGetFilePath("missing.txt", out var fullPath).Should().BeFalse();
        fullPath.Should().BeNull();
    }

    [Fact]
    public void ShouldImplementIFileBackedAssetSource()
    {
        new FolderAssetSource(_dir.Path).Should().BeAssignableTo<IFileBackedAssetSource>();
    }

    [Fact]
    public void Enumerate_NonRecursive_ShouldReturnFilesDirectlyUnderDirectory()
    {
        _dir.CreateFile("a.png");
        _dir.CreateFile("b.txt");
        _dir.CreateFile("sub/c.png");
        var source = new FolderAssetSource(_dir.Path);

        source.Enumerate("", "*", false).Should().BeEquivalentTo("a.png", "b.txt");
        source.Enumerate("/", "*.png", false).Should().BeEquivalentTo("a.png");
        source.Enumerate("sub", "*", false).Should().BeEquivalentTo("sub/c.png");
    }

    [Fact]
    public void Enumerate_Recursive_ShouldIncludeSubdirectories()
    {
        _dir.CreateFile("a.png");
        _dir.CreateFile("sub/c.png");
        _dir.CreateFile("sub/deep/d.png");
        _dir.CreateFile("sub/deep/e.txt");
        var source = new FolderAssetSource(_dir.Path);

        source
            .Enumerate("", "*.png", true)
            .Should()
            .BeEquivalentTo("a.png", "sub/c.png", "sub/deep/d.png");
        source
            .Enumerate("sub", "*", true)
            .Should()
            .BeEquivalentTo("sub/c.png", "sub/deep/d.png", "sub/deep/e.txt");
    }

    [Fact]
    public void Enumerate_PatternShouldBeCaseSensitive()
    {
        _dir.CreateFile("a.png");
        _dir.CreateFile("b.PNG");
        var source = new FolderAssetSource(_dir.Path);

        source.Enumerate("", "*.png", false).Should().BeEquivalentTo("a.png");
    }

    [Fact]
    public void Enumerate_MissingDirectory_ShouldBeEmpty()
    {
        var source = new FolderAssetSource(_dir.Path);

        source.Enumerate("missing", "*", true).Should().BeEmpty();
    }

    [Fact]
    public void Enumerate_WithMountPoint_ShouldReturnLogicalPaths()
    {
        _dir.CreateFile("x.png");
        _dir.CreateFile("sub/y.png");
        var source = new FolderAssetSource(_dir.Path, "assets");

        source.Enumerate("assets", "*", false).Should().BeEquivalentTo("assets/x.png");
        source.Enumerate("", "*", false).Should().BeEmpty("ルート直下にはファイルが無いはず");
        source.Enumerate("", "*", true).Should().BeEquivalentTo("assets/x.png", "assets/sub/y.png");
        source.Enumerate("other", "*", true).Should().BeEmpty();
    }
}
