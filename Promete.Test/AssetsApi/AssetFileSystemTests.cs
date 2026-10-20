using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetFileSystem"/> のマウント・優先度・読み出し・破棄のテスト。
/// </summary>
public class AssetFileSystemTests
{
    [Fact]
    public void Empty_ShouldFindNothing()
    {
        using var fs = new AssetFileSystem();

        fs.Sources.Should().BeEmpty();
        fs.Exists("a.txt").Should().BeFalse();
        fs.Enumerate("", "*", true).Should().BeEmpty();
        fs.TryGetFilePath("a.txt", out _).Should().BeFalse();
    }

    [Fact]
    public void Empty_OpenRead_ShouldExplainThatNothingIsMounted()
    {
        using var fs = new AssetFileSystem();

        var act = () => fs.OpenRead("a.txt");

        act.Should().Throw<FileNotFoundException>().WithMessage("*マウントされていません*");
    }

    [Fact]
    public void OpenRead_FromSingleSource_ShouldReadContent()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a/b.txt", "content")));

        fs.Exists("a/b.txt").Should().BeTrue();
        using var stream = fs.OpenRead("a/b.txt");
        stream.ReadAllText().Should().Be("content");
    }

    [Fact]
    public void Paths_ShouldBeNormalizedBeforeQueryingSources()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a/b.txt", "content")));

        fs.Exists("/a/./c/../b.txt").Should().BeTrue();
        using var stream = fs.OpenRead("./a/b.txt");
        stream.ReadAllText().Should().Be("content");
        fs.Enumerate("/a/", "*", false).Should().Equal("a/b.txt");
    }

    [Fact]
    public void Paths_WithBackslash_ShouldThrow()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a/b.txt", "content")));

        ((Action)(() => fs.Exists("a\\b.txt"))).Should().Throw<ArgumentException>();
        ((Action)(() => fs.OpenRead("a\\b.txt"))).Should().Throw<ArgumentException>();
        ((Action)(() => fs.Enumerate("a\\c", "*", false).ToList()))
            .Should()
            .Throw<ArgumentException>();
        ((Action)(() => fs.TryGetFilePath("a\\b.txt", out _))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Missing_OpenRead_ShouldThrowFileNotFound()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a.txt", "a")));

        var act = () => fs.OpenRead("missing.txt");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void HigherPriority_ShouldWinRegardlessOfMountOrder()
    {
        using var fs = new AssetFileSystem();
        var high = new InMemoryAssetSource(("a.txt", "high"));
        var low = new InMemoryAssetSource(("a.txt", "low"));
        fs.Mount(high, 10);
        fs.Mount(low, 0);

        using var stream = fs.OpenRead("a.txt");

        stream.ReadAllText().Should().Be("high");
        fs.Sources.Should().Equal(high, low);
    }

    [Fact]
    public void SamePriority_LaterMountShouldWin()
    {
        using var fs = new AssetFileSystem();
        var first = new InMemoryAssetSource(("a.txt", "first"));
        var second = new InMemoryAssetSource(("a.txt", "second"));
        fs.Mount(first);
        fs.Mount(second);

        using var stream = fs.OpenRead("a.txt");

        stream
            .ReadAllText()
            .Should()
            .Be("second", "後からマウントしたソースで上書きできるはず (DLC・パッチ)");
        fs.Sources.Should().Equal(second, first);
    }

    [Fact]
    public void Sources_ShouldBeOrderedByPriorityThenMountOrder()
    {
        using var fs = new AssetFileSystem();
        var a = new InMemoryAssetSource();
        var b = new InMemoryAssetSource();
        var c = new InMemoryAssetSource();
        var d = new InMemoryAssetSource();
        fs.Mount(a, 0);
        fs.Mount(b, 5);
        fs.Mount(c, 0);
        fs.Mount(d, -5);

        fs.Sources.Should().Equal(b, c, a, d);
    }

    [Fact]
    public void MissingInHigherPriority_ShouldFallThroughToLower()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("patch.txt", "patch")), 10);
        fs.Mount(new InMemoryAssetSource(("base.txt", "base"), ("patch.txt", "old")), 0);

        using (var stream = fs.OpenRead("base.txt"))
            stream.ReadAllText().Should().Be("base");
        using (var stream = fs.OpenRead("patch.txt"))
            stream.ReadAllText().Should().Be("patch");
    }

    [Fact]
    public void Enumerate_ShouldMergeDistinctAndSortOrdinally()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(
            new InMemoryAssetSource(("d/b.txt", "1"), ("d/a.txt", "1"), ("d/sub/x.txt", "1")),
            1
        );
        fs.Mount(new InMemoryAssetSource(("d/a.txt", "2"), ("d/C.txt", "2"), ("e/z.txt", "2")), 0);

        fs.Enumerate("d", "*", false).Should().Equal("d/C.txt", "d/a.txt", "d/b.txt");
        fs.Enumerate("d", "*", true).Should().Equal("d/C.txt", "d/a.txt", "d/b.txt", "d/sub/x.txt");
        fs.Enumerate("", "*.txt", true)
            .Should()
            .Equal("d/C.txt", "d/a.txt", "d/b.txt", "d/sub/x.txt", "e/z.txt");
    }

    [Fact]
    public void Enumerate_DefaultArguments_ShouldListAllFilesDirectlyUnderDirectory()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("d/a.txt", "1"), ("d/sub/x.txt", "1")));

        fs.Enumerate("d").Should().Equal("d/a.txt");
    }

    [Fact]
    public void OpenRead_NonSeekableSourceStream_ShouldBeMadeSeekable()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(
            new InMemoryAssetSource(("a.txt", "0123456789")) { ReturnNonSeekableStream = true }
        );

        using var stream = fs.OpenRead("a.txt");

        stream.CanSeek.Should().BeTrue("ソースが契約を破っても、seek できるストリームを返すはず");
        stream.Seek(3, SeekOrigin.Begin);
        stream.ReadAllText().Should().Be("3456789");
    }

    [Fact]
    public void Mount_Null_ShouldThrowArgumentNull()
    {
        using var fs = new AssetFileSystem();

        var act = () => fs.Mount(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Mount_SameSourceTwice_ShouldThrow()
    {
        using var fs = new AssetFileSystem();
        var source = new InMemoryAssetSource();
        fs.Mount(source);

        var act = () => fs.Mount(source, 1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unmount_ShouldHideFilesAndRevealLowerSource()
    {
        using var fs = new AssetFileSystem();
        var patch = new InMemoryAssetSource(("a.txt", "patch"), ("only-patch.txt", "p"));
        fs.Mount(new InMemoryAssetSource(("a.txt", "base")), 0);
        fs.Mount(patch, 10);

        fs.Unmount(patch).Should().BeTrue();

        fs.Exists("only-patch.txt").Should().BeFalse();
        using var stream = fs.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("base");
        fs.Sources.Should().NotContain(patch);
    }

    [Fact]
    public void Unmount_ShouldDisposeDisposableSourceOnce()
    {
        using var fs = new AssetFileSystem();
        var source = new InMemoryAssetSource(("a.txt", "a"));
        fs.Mount(source);

        fs.Unmount(source);
        fs.Unmount(source).Should().BeFalse("2 回目はマウントされていないはず");

        source.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Unmount_NotMounted_ShouldReturnFalseAndNotDispose()
    {
        using var fs = new AssetFileSystem();
        var source = new InMemoryAssetSource();

        fs.Unmount(source).Should().BeFalse();
        source.DisposeCount.Should().Be(0, "マウントしていないソースの所有権は持っていないはず");
    }

    [Fact]
    public void Unmount_Null_ShouldThrowArgumentNull()
    {
        using var fs = new AssetFileSystem();

        var act = () => fs.Unmount(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unmount_ShouldKeepOpenedStreamsValid()
    {
        using var dir = new TempDirectory();
        dir.CreateFile("a.txt", "still readable");
        using var fs = new AssetFileSystem();
        var source = new FolderAssetSource(dir.Path);
        fs.Mount(source);
        using var stream = fs.OpenRead("a.txt");

        fs.Unmount(source);

        stream
            .ReadAllText()
            .Should()
            .Be("still readable", "開いたストリームは閉じられるまで有効のはず");
    }

    [Fact]
    public void Dispose_ShouldDisposeAllDisposableSourcesOnce()
    {
        var fs = new AssetFileSystem();
        var a = new InMemoryAssetSource();
        var b = new InMemoryAssetSource();
        fs.Mount(a);
        fs.Mount(b, 3);

        fs.Dispose();
        fs.Dispose();

        a.DisposeCount.Should().Be(1);
        b.DisposeCount.Should().Be(1, "Dispose は冪等のはず");
    }

    [Fact]
    public void Dispose_ShouldKeepOpenedStreamsValid()
    {
        var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a.txt", "alive")));
        using var stream = fs.OpenRead("a.txt");

        fs.Dispose();

        stream.ReadAllText().Should().Be("alive");
    }

    [Fact]
    public void AfterDispose_MembersShouldThrowObjectDisposed()
    {
        var fs = new AssetFileSystem();
        var source = new InMemoryAssetSource(("a.txt", "a"));
        fs.Mount(source);
        fs.Dispose();

        ((Action)(() => fs.Mount(new InMemoryAssetSource())))
            .Should()
            .Throw<ObjectDisposedException>();
        ((Action)(() => fs.Unmount(source))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => fs.Exists("a.txt"))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => fs.OpenRead("a.txt"))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => fs.Enumerate("", "*", true).ToList()))
            .Should()
            .Throw<ObjectDisposedException>();
        ((Action)(() => fs.TryGetFilePath("a.txt", out _)))
            .Should()
            .Throw<ObjectDisposedException>();
    }

    [Fact]
    public void TryGetFilePath_WinnerIsFileBacked_ShouldReturnItsPath()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new FileBackedInMemoryAssetSource(("a.txt", "a")));

        fs.TryGetFilePath("/a.txt", out var fullPath).Should().BeTrue();
        fullPath.Should().Be("/fake/a.txt");
    }

    [Fact]
    public void TryGetFilePath_WinnerIsNotFileBacked_ShouldReturnFalseWithoutFallingThrough()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new FileBackedInMemoryAssetSource(("a.txt", "file")), 0);
        fs.Mount(new InMemoryAssetSource(("a.txt", "memory")), 10);

        fs.TryGetFilePath("a.txt", out var fullPath)
            .Should()
            .BeFalse(
                "優先度で勝ったソースが実ファイルを持たないなら、下のソースのファイルを返してはいけない"
            );
        fullPath.Should().BeNull();
    }

    [Fact]
    public void TryGetFilePath_FileOnlyInLowerFileBackedSource_ShouldReturnItsPath()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new FileBackedInMemoryAssetSource(("a.txt", "file")), 0);
        fs.Mount(new InMemoryAssetSource(("b.txt", "memory")), 10);

        fs.TryGetFilePath("a.txt", out var fullPath).Should().BeTrue();
        fullPath.Should().Be("/fake/a.txt");
    }

    [Fact]
    public void CaseMismatchInFolder_ShouldPropagateHintException()
    {
        using var dir = new TempDirectory();
        dir.CreateFile("Player.png");
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("other.txt", "o")), 10);
        fs.Mount(new FolderAssetSource(dir.Path), 0);

        fs.Exists("player.png").Should().BeFalse();
        var act = () => fs.OpenRead("player.png");

        var ex = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        ex.ActualPath.Should().Be("Player.png");
        ex.Message.Should().Contain("大文字・小文字を区別");
    }

    [Fact]
    public void CaseMismatchInOneSource_ExactMatchInAnother_ShouldSucceed()
    {
        using var dir = new TempDirectory();
        dir.CreateFile("Player.png", "folder");
        using var fs = new AssetFileSystem();
        fs.Mount(new FolderAssetSource(dir.Path), 10);
        fs.Mount(new InMemoryAssetSource(("player.png", "memory")), 0);

        using var stream = fs.OpenRead("player.png");

        stream.ReadAllText().Should().Be("memory", "どこかに完全一致があれば、そちらを読むはず");
    }
}
