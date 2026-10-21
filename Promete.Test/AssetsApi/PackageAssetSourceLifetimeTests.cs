using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PackageAssetSource"/> を Dispose / Unmount した後も、開いたストリームが閉じるまで有効なことのテスト。
/// 元のファイルやストリームは、ソースと開いたストリームのすべてが閉じた時点で閉じる。
/// </summary>
public sealed class PackageAssetSourceLifetimeTests : IDisposable
{
    private static readonly byte[] Stored = RawPackageLayout.RandomBytes(5000, 11);
    private static readonly byte[] Text = RawPackageLayout.CompressibleText("lifetime");

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    public static TheoryData<string> Kinds => new() { "stream", "file" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void OpenStreams_ShouldRemainReadableAfterSourceDispose(string kind)
    {
        var source = Create(kind);
        var stored = source.OpenRead("stored.bin");
        var deflated = source.OpenRead("text.txt");
        stored.Position = 1000;

        source.Dispose();

        var buffer = new byte[100];
        stored.ReadExactly(buffer);
        buffer.Should().Equal(Stored[1000..1100]);
        stored.Seek(0, SeekOrigin.Begin);
        PackageAssetSourceTests.ReadAllBytes(stored).Should().Equal(Stored);
        PackageAssetSourceTests.ReadAllBytes(deflated).Should().Equal(Text);
        stored.Dispose();
        deflated.Dispose();
    }

    [Fact]
    public void OwnedStream_ShouldBeClosedOnlyAfterSourceAndAllOpenedStreamsAreClosed()
    {
        var inner = new ProbeStream(Package());
        var source = new PackageAssetSource(inner);
        var first = source.OpenRead("stored.bin");
        var second = source.OpenRead("stored.bin");
        source.OpenRead("text.txt").Dispose();

        source.Dispose();
        inner.IsDisposed.Should().BeFalse("開いたストリームが残っている");
        first.Dispose();
        inner.IsDisposed.Should().BeFalse("開いたストリームが残っている");
        second.Dispose();

        inner.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void DeflatedStreams_ShouldNotKeepPackageOpen()
    {
        var inner = new ProbeStream(Package());
        var source = new PackageAssetSource(inner);
        using var deflated = source.OpenRead("text.txt");

        source.Dispose();

        inner.DisposeCount.Should().Be(1, "展開済みのストリームは元のストリームを使わない");
        PackageAssetSourceTests.ReadAllBytes(deflated).Should().Equal(Text);
    }

    [Fact]
    public void File_ShouldBeReleasedOnlyAfterSourceAndAllOpenedStreamsAreClosed()
    {
        var path = WriteFile();
        var source = new PackageAssetSource(path);
        var stream = source.OpenRead("stored.bin");

        source.Dispose();
        if (OperatingSystem.IsWindows())
        {
            var whileOpen = () =>
                new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose();
            whileOpen
                .Should()
                .Throw<IOException>("開いたストリームが残っている間は、ファイルを開いたまま");
        }

        stream.Dispose();
        PackageAssetSourceOpenTests.AssertFileIsNotOpen(path);
    }

    [Fact]
    public void File_WithoutOpenedStreams_ShouldBeReleasedOnDispose()
    {
        var path = WriteFile();
        var source = new PackageAssetSource(path);
        source.OpenRead("text.txt").Dispose();
        source.OpenRead("stored.bin").Dispose();

        source.Dispose();

        PackageAssetSourceOpenTests.AssertFileIsNotOpen(path);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Unmount_ShouldKeepOpenedStreamsReadable(string kind)
    {
        using var fs = new AssetFileSystem();
        var source = Create(kind);
        fs.Mount(source);
        using var stream = fs.OpenRead("stored.bin");

        fs.Unmount(source).Should().BeTrue();

        PackageAssetSourceTests.ReadAllBytes(stream).Should().Equal(Stored);
        var act = () => fs.OpenRead("stored.bin");
        act.Should().Throw<FileNotFoundException>();
        ((Action)(() => source.OpenRead("stored.bin"))).Should().Throw<ObjectDisposedException>();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void FileSystemDispose_ShouldKeepOpenedStreamsReadable(string kind)
    {
        var fs = new AssetFileSystem();
        fs.Mount(Create(kind));
        using var stream = fs.OpenRead("stored.bin");

        fs.Dispose();

        PackageAssetSourceTests.ReadAllBytes(stream).Should().Equal(Stored);
    }

    [Fact]
    public void ManyOpenAndClose_ShouldNotLeakReferences()
    {
        var inner = new ProbeStream(Package());
        var source = new PackageAssetSource(inner);
        for (var i = 0; i < 100; i++)
        {
            source.OpenRead("stored.bin").Dispose();
            source.OpenRead("text.txt").Dispose();
        }

        source.Dispose();

        inner.DisposeCount.Should().Be(1);
    }

    private static byte[] Package() =>
        new RawPackageBuilder().Stored("stored.bin", Stored).Deflated("text.txt", Text).Build();

    private PackageAssetSource Create(string kind) =>
        kind == "file"
            ? new PackageAssetSource(WriteFile())
            : new PackageAssetSource(new MemoryStream(Package(), false));

    private string WriteFile()
    {
        var path = Path.Combine(_dir.Path, Guid.NewGuid().ToString("N") + ".pak");
        File.WriteAllBytes(path, Package());
        return path;
    }
}
