using System.Text;
using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PackageAssetSource"/> の <see cref="IAssetSource"/> としての契約 (存在の確認・開く・列挙・マウントポイント・破棄) のテスト。
/// パッケージはテスト側のビルダー (<see cref="RawPackageBuilder"/>) で作り、ストリームから開く。
/// </summary>
public class PackageAssetSourceTests
{
    private const string NfcGa = "が";
    private const string NfdGa = "が";

    private static readonly byte[] Binary = RawPackageLayout.RandomBytes(300, 5);
    private static readonly byte[] Compressible = RawPackageLayout.CompressibleText();

    private static byte[] SamplePackage() =>
        new RawPackageBuilder()
            .Stored("a.txt", "A")
            .Deflated("compressed.txt", Compressible)
            .Stored("dir/b.txt", "B")
            .Stored("dir/c.png", Binary)
            .Stored("dir/sub/d.txt", "D")
            .Stored("dir/Upper.txt", "U")
            .Stored("empty.bin", [])
            .Stored("日本語/" + NfcGa + ".txt", "ga")
            .Stored(NfcGa + "/x.txt", "x")
            .Build();

    private static PackageAssetSource Open(byte[]? package = null, string mountPoint = "") =>
        new(new MemoryStream(package ?? SamplePackage(), false), false, mountPoint);

    [Fact]
    public void OpenRead_StoredEntry_ShouldReturnContent()
    {
        using var source = Open();

        using var stream = source.OpenRead("dir/c.png");

        ReadAllBytes(stream).Should().Equal(Binary);
    }

    [Fact]
    public void OpenRead_DeflatedEntry_ShouldReturnInflatedContent()
    {
        using var source = Open();

        using var stream = source.OpenRead("compressed.txt");

        ReadAllBytes(stream).Should().Equal(Compressible);
    }

    [Fact]
    public void OpenRead_EmptyEntry_ShouldReturnEmptyStream()
    {
        using var source = Open();

        using var stream = source.OpenRead("empty.bin");

        stream.Length.Should().Be(0);
        stream.ReadByte().Should().Be(-1);
    }

    [Fact]
    public void OpenRead_ShouldAlwaysReturnSeekableReadOnlyStream()
    {
        using var source = Open();

        foreach (var path in new[] { "a.txt", "compressed.txt", "empty.bin" })
        {
            using var stream = source.OpenRead(path);
            stream.CanSeek.Should().BeTrue(path);
            stream.CanRead.Should().BeTrue(path);
            stream.CanWrite.Should().BeFalse(path);
        }
    }

    [Fact]
    public void OpenRead_DeflatedEntry_ShouldBeMemoryStream()
    {
        using var source = Open();

        using var stream = source.OpenRead("compressed.txt");

        stream.Should().BeAssignableTo<MemoryStream>();
        stream.Length.Should().Be(Compressible.Length);
    }

    [Fact]
    public void OpenRead_StoredEntry_ShouldSeek()
    {
        using var source = Open();
        using var stream = source.OpenRead("dir/c.png");

        stream.Length.Should().Be(Binary.Length);
        stream.Seek(-10, SeekOrigin.End);

        ReadAllBytes(stream).Should().Equal(Binary[^10..]);
    }

    [Fact]
    public void OpenRead_SameEntryTwice_ShouldReturnIndependentStreams()
    {
        using var source = Open();
        using var first = source.OpenRead("dir/c.png");
        using var second = source.OpenRead("dir/c.png");

        first.Position = 100;
        var a = new byte[5];
        var b = new byte[5];
        first.ReadExactly(a);
        second.ReadExactly(b);

        a.Should().Equal(Binary[100..105]);
        b.Should().Equal(Binary[..5]);
    }

    [Theory]
    [InlineData("a.txt")]
    [InlineData("/a.txt")]
    [InlineData("./dir/../a.txt")]
    public void OpenRead_ShouldNormalizePath(string path)
    {
        using var source = Open();

        using var stream = source.OpenRead(path);

        stream.ReadAllText().Should().Be("A");
    }

    [Fact]
    public void OpenRead_NfdPath_ShouldFindNfcEntry()
    {
        using var source = Open();

        using var stream = source.OpenRead("日本語/" + NfdGa + ".txt");

        stream.ReadAllText().Should().Be("ga");
        source.Exists("日本語/" + NfdGa + ".txt").Should().BeTrue();
    }

    [Fact]
    public void OpenRead_Missing_ShouldThrowFileNotFound()
    {
        using var source = Open();

        var act = () => source.OpenRead("missing.txt");

        act.Should()
            .Throw<FileNotFoundException>()
            .Which.Should()
            .NotBeOfType<AssetPathCaseMismatchException>();
    }

    [Fact]
    public void OpenRead_Directory_ShouldThrowFileNotFound()
    {
        using var source = Open();

        var act = () => source.OpenRead("dir");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void OpenRead_CaseMismatch_ShouldThrowWithActualPath()
    {
        using var source = Open();

        var act = () => source.OpenRead("DIR/upper.TXT");

        var e = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        e.RequestedPath.Should().Be("DIR/upper.TXT");
        e.ActualPath.Should().Be("dir/Upper.txt");
    }

    [Theory]
    [InlineData("a.txt", true)]
    [InlineData("/dir/sub/d.txt", true)]
    [InlineData("empty.bin", true)]
    [InlineData("A.txt", false)]
    [InlineData("dir", false)]
    [InlineData("dir/sub", false)]
    [InlineData("missing.txt", false)]
    public void Exists_ShouldMatchFilesOrdinally(string path, bool expected)
    {
        using var source = Open();

        source.Exists(path).Should().Be(expected);
    }

    [Fact]
    public void Enumerate_Root_ShouldListTopLevelFilesSorted()
    {
        using var source = Open();

        source.Enumerate("", "*", false).Should().Equal("a.txt", "compressed.txt", "empty.bin");
        source.Enumerate("/", "*", false).Should().Equal("a.txt", "compressed.txt", "empty.bin");
    }

    [Fact]
    public void Enumerate_Recursive_ShouldListAllFilesUnderDirectorySorted()
    {
        using var source = Open();

        source
            .Enumerate("dir", "*", true)
            .Should()
            .Equal("dir/Upper.txt", "dir/b.txt", "dir/c.png", "dir/sub/d.txt");
    }

    [Fact]
    public void Enumerate_NonRecursive_ShouldNotIncludeSubdirectories()
    {
        using var source = Open();

        source
            .Enumerate("dir", "*", false)
            .Should()
            .Equal("dir/Upper.txt", "dir/b.txt", "dir/c.png");
    }

    [Theory]
    [InlineData("*.txt", new[] { "dir/Upper.txt", "dir/b.txt", "dir/sub/d.txt" })]
    [InlineData("?.txt", new[] { "dir/b.txt", "dir/sub/d.txt" })]
    [InlineData("*.TXT", new string[0])]
    [InlineData("u*", new string[0])]
    public void Enumerate_Pattern_ShouldMatchFileNameCaseSensitively(
        string pattern,
        string[] expected
    )
    {
        using var source = Open();

        source.Enumerate("dir", pattern, true).Should().Equal(expected);
    }

    [Fact]
    public void Enumerate_MissingDirectory_ShouldBeEmpty()
    {
        using var source = Open();

        source.Enumerate("nothing", "*", true).Should().BeEmpty();
        source
            .Enumerate("di", "*", true)
            .Should()
            .BeEmpty("'di' は 'dir' の接頭辞だがディレクトリではない");
    }

    [Fact]
    public void Enumerate_NfdDirectory_ShouldMatch()
    {
        using var source = Open();

        source.Enumerate("日本語", "*", false).Should().Equal("日本語/" + NfcGa + ".txt");
        source.Enumerate(NfdGa, "*", false).Should().Equal(NfcGa + "/x.txt");
        source.Enumerate("日本語", NfdGa + ".*", false).Should().Equal("日本語/" + NfcGa + ".txt");
    }

    [Fact]
    public void MountPoint_ShouldPrefixLogicalPaths()
    {
        using var source = Open(mountPoint: "/assets/");

        source.Exists("assets/a.txt").Should().BeTrue();
        source.Exists("a.txt").Should().BeFalse();
        using (var stream = source.OpenRead("assets/dir/b.txt"))
            stream.ReadAllText().Should().Be("B");
        source.Enumerate("", "*", false).Should().BeEmpty();
        source
            .Enumerate("assets", "*", false)
            .Should()
            .Equal("assets/a.txt", "assets/compressed.txt", "assets/empty.bin");
        source.Enumerate("", "*.png", true).Should().Equal("assets/dir/c.png");
    }

    [Fact]
    public void InvalidArguments_ShouldThrow()
    {
        using var source = Open();

        ((Action)(() => source.Exists("a\\b"))).Should().Throw<ArgumentException>();
        ((Action)(() => source.OpenRead("a\\b"))).Should().Throw<ArgumentException>();
        ((Action)(() => source.Exists(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => source.OpenRead(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => source.Enumerate("dir", "a/b", false))).Should().Throw<ArgumentException>();
        ((Action)(() => source.Enumerate("a\\b", "*", false))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidArguments_ShouldThrow()
    {
        ((Action)(() => new PackageAssetSource((Stream)null!)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => new PackageAssetSource(new MemoryStream(SamplePackage()), false, null!)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => new PackageAssetSource(new MemoryStream(SamplePackage()), false, "a\\b")))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WriteOnlyStream_ShouldThrowArgumentException()
    {
        var act = () => new PackageAssetSource(new WriteOnlyStream());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Members_AfterDispose_ShouldThrowObjectDisposed()
    {
        var source = Open();
        source.Dispose();

        ((Action)(() => source.Exists("a.txt"))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => source.OpenRead("a.txt"))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => source.Enumerate("", "*", true))).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_Twice_ShouldNotThrow()
    {
        var source = Open();

        source.Dispose();
        var act = () => source.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldCloseOwnedStream()
    {
        var stream = new ProbeStream(SamplePackage());
        var source = new PackageAssetSource(stream);

        source.Dispose();

        stream.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_LeaveOpen_ShouldNotCloseStream()
    {
        var stream = new ProbeStream(SamplePackage());
        var source = new PackageAssetSource(stream, leaveOpen: true);
        using (var opened = source.OpenRead("a.txt"))
            opened.ReadAllText().Should().Be("A");

        source.Dispose();

        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Stream_WithPrefix_ShouldTreatCurrentPositionAsPackageStart()
    {
        var prefix = Encoding.ASCII.GetBytes("MZ-some-executable-bytes");
        var bytes = prefix.Concat(SamplePackage()).ToArray();
        var stream = new MemoryStream(bytes, false) { Position = prefix.Length };

        using var source = new PackageAssetSource(stream);

        using var opened = source.OpenRead("dir/c.png");
        ReadAllBytes(opened).Should().Equal(Binary);
    }

    [Fact]
    public void Stream_NonSeekable_ShouldBeCopiedAndClosedImmediately()
    {
        var stream = new ProbeStream(SamplePackage()) { CanSeekOverride = false };

        using var source = new PackageAssetSource(stream);

        stream.DisposeCount.Should().Be(1, "seek できないストリームは読み込んだ直後に閉じる");
        using var opened = source.OpenRead("compressed.txt");
        ReadAllBytes(opened).Should().Equal(Compressible);
        using var stored = source.OpenRead("dir/c.png");
        stored.CanSeek.Should().BeTrue();
        ReadAllBytes(stored).Should().Equal(Binary);
    }

    [Fact]
    public void Stream_NonSeekableWithLeaveOpen_ShouldNotBeClosed()
    {
        var stream = new ProbeStream(SamplePackage()) { CanSeekOverride = false };

        using var source = new PackageAssetSource(stream, leaveOpen: true);

        stream.IsDisposed.Should().BeFalse();
        source.Exists("a.txt").Should().BeTrue();
    }

    [Fact]
    public void EmptyPackage_ShouldOpenWithNoEntries()
    {
        using var source = Open(new RawPackageBuilder().Build());

        source.Enumerate("", "*", true).Should().BeEmpty();
        source.Exists("a.txt").Should().BeFalse();
    }

    internal static byte[] ReadAllBytes(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// 読み取りのできないストリーム。
    /// </summary>
    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
