using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PackageAssetSource"/> がパッケージ全体をメモリに載せず、必要な部分だけを読むことのテスト。
/// </summary>
public sealed class PackageAssetSourceMemoryTests : IDisposable
{
    private const int EntrySize = 256 * 1024;

    /// <summary>
    /// 実装が読み出しをまとめる (バッファする) ことを許す余裕。
    /// </summary>
    private const int Slack = 4096;

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Open_ShouldReadOnlyHeaderAndToc()
    {
        var bytes = LargePackage(out var raw);
        var stream = new ProbeStream(bytes);

        using var source = new PackageAssetSource(stream);

        stream.BytesRead.Should().BeLessThanOrEqualTo((long)(64 + raw.TocSize) + Slack);
    }

    [Fact]
    public void OpenRead_Stored_ShouldReadOnlyRequestedRange()
    {
        var bytes = LargePackage(out _);
        var stream = new ProbeStream(bytes);
        using var source = new PackageAssetSource(stream);
        stream.ResetCounter();

        using var entry = source.OpenRead("big/2.bin");
        entry.Seek(100_000, SeekOrigin.Begin);
        var buffer = new byte[100];
        entry.ReadExactly(buffer);

        stream.BytesRead.Should().BeLessThanOrEqualTo(100 + Slack);
        buffer.Should().Equal(Content(2)[100_000..100_100]);
    }

    [Fact]
    public void OpenRead_Deflated_ShouldReadOnlyThatEntry()
    {
        var text = RawPackageLayout.CompressibleText("memory", 5000);
        var builder = new RawPackageBuilder()
            .Stored("big/0.bin", Content(0))
            .Deflated("text.txt", text)
            .Stored("big/1.bin", Content(1));
        var bytes = builder.Build();
        var stored = RawPackage.Parse(bytes).Entry("text.txt").StoredSize;
        var stream = new ProbeStream(bytes);
        using var source = new PackageAssetSource(stream);
        stream.ResetCounter();

        using var entry = source.OpenRead("text.txt");

        stream.BytesRead.Should().BeLessThanOrEqualTo((long)stored + Slack);
        PackageAssetSourceTests.ReadAllBytes(entry).Should().Equal(text);
    }

    [Fact]
    public void FromFile_ShouldNotLoadWholePackageIntoMemory()
    {
        // 32 MB のパッケージを開いて小さく読んでも、確保するメモリは 1 MB に満たない
        var builder = new RawPackageBuilder();
        for (var i = 0; i < 8; i++)
            builder.Stored($"big/{i}.bin", RawPackageLayout.RandomBytes(4 * 1024 * 1024, i));
        var path = Path.Combine(_dir.Path, "large.pak");
        File.WriteAllBytes(path, builder.Build());
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        using (var source = new PackageAssetSource(path))
        using (var entry = source.OpenRead("big/5.bin"))
        {
            entry.Seek(-4096, SeekOrigin.End);
            entry.ReadExactly(new byte[4096]);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().BeLessThan(1024 * 1024);
    }

    private static byte[] Content(int index) =>
        RawPackageLayout.RandomBytes(EntrySize, 100 + index);

    private static byte[] LargePackage(out RawPackage raw)
    {
        var builder = new RawPackageBuilder();
        for (var i = 0; i < 4; i++)
            builder.Stored($"big/{i}.bin", Content(i));
        var bytes = builder.Build();
        raw = RawPackage.Parse(bytes);
        return bytes;
    }
}
