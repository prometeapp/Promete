using FluentAssertions;
using Promete.Assets.Packaging;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 無圧縮のエントリを直接読むストリーム <see cref="PackageEntryStream"/> のテスト。
/// </summary>
public sealed class PackageEntryStreamTests
{
    private static readonly byte[] Data = RawPackageLayout.RandomBytes(1000, 3);

    [Fact]
    public void Read_ShouldReadOnlyTheEntryRange()
    {
        using var stream = Open(100, 50, out _);

        var buffer = new byte[200];
        var total = ReadAll(stream, buffer);

        total.Should().Be(50);
        buffer[..50].Should().Equal(Data[100..150]);
        stream.Read(buffer, 0, buffer.Length).Should().Be(0, "末尾では 0 を返す");
    }

    [Fact]
    public void Capabilities_ShouldBeReadOnlyAndSeekable()
    {
        using var stream = Open(0, 10, out _);

        stream.CanRead.Should().BeTrue();
        stream.CanSeek.Should().BeTrue();
        stream.CanWrite.Should().BeFalse();
        stream.Length.Should().Be(10);
        stream.Position.Should().Be(0);
    }

    [Theory]
    [InlineData(5, SeekOrigin.Begin, 5)]
    [InlineData(-3, SeekOrigin.End, 47)]
    [InlineData(0, SeekOrigin.End, 50)]
    [InlineData(10, SeekOrigin.End, 60)]
    public void Seek_ShouldMovePosition(long offset, SeekOrigin origin, long expected)
    {
        using var stream = Open(100, 50, out _);

        stream.Seek(offset, origin).Should().Be(expected);
        stream.Position.Should().Be(expected);
    }

    [Fact]
    public void Seek_FromCurrent_ShouldBeRelative()
    {
        using var stream = Open(100, 50, out _);
        stream.Position = 10;

        stream.Seek(5, SeekOrigin.Current).Should().Be(15);
        stream.Seek(-15, SeekOrigin.Current).Should().Be(0);
    }

    [Fact]
    public void Read_AfterSeek_ShouldReadFromNewPosition()
    {
        using var stream = Open(100, 50, out _);

        stream.Seek(-10, SeekOrigin.End);
        var buffer = new byte[10];
        ReadAll(stream, buffer).Should().Be(10);

        buffer.Should().Equal(Data[140..150]);
        stream.Position.Should().Be(50);
    }

    [Fact]
    public void Read_SpanOverload_ShouldWork()
    {
        using var stream = Open(10, 20, out _);
        stream.Position = 5;
        Span<byte> buffer = stackalloc byte[8];

        var read = stream.Read(buffer);

        read.Should().BeGreaterThan(0);
        buffer[..read].ToArray().Should().Equal(Data[15..(15 + read)]);
    }

    [Fact]
    public void Read_BeyondEnd_ShouldReturnZero()
    {
        using var stream = Open(100, 50, out _);
        stream.Position = 80;

        stream.Read(new byte[10], 0, 10).Should().Be(0);
        stream.ReadByte().Should().Be(-1);
    }

    [Fact]
    public void Seek_BeforeBeginning_ShouldThrowIOException()
    {
        using var stream = Open(100, 50, out _);

        var act = () => stream.Seek(-1, SeekOrigin.Begin);

        act.Should().Throw<IOException>();
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Position_Negative_ShouldThrowArgumentOutOfRange()
    {
        using var stream = Open(100, 50, out _);

        var act = () => stream.Position = -1;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WriteAndSetLength_ShouldThrowNotSupported()
    {
        using var stream = Open(100, 50, out _);

        ((Action)(() => stream.Write([1], 0, 1))).Should().Throw<NotSupportedException>();
        ((Action)(() => stream.SetLength(1))).Should().Throw<NotSupportedException>();
        ((Action)(() => stream.Flush())).Should().NotThrow();
    }

    [Fact]
    public void TwoStreams_ShouldHaveIndependentPositions()
    {
        var storage = new StreamPackageStorage(new MemoryStream(Data, false), false);
        storage.TryAddRef();
        storage.TryAddRef();
        using var first = new PackageEntryStream(storage, 0, 100);
        using var second = new PackageEntryStream(storage, 0, 100);

        first.Position = 50;
        var a = new byte[10];
        var b = new byte[10];
        ReadAll(first, a);
        ReadAll(second, b);

        a.Should().Equal(Data[50..60]);
        b.Should().Equal(Data[..10]);
        storage.Release();
    }

    [Fact]
    public void Dispose_ShouldReleaseStorageReferenceOnce()
    {
        var inner = new ProbeStream(Data);
        var storage = new StreamPackageStorage(inner, false);
        storage.TryAddRef().Should().BeTrue();
        var stream = new PackageEntryStream(storage, 0, 10);
        storage.Release();
        inner.IsDisposed.Should().BeFalse("ストリームが参照を持っている");

        stream.Dispose();
        stream.Dispose();

        inner.DisposeCount.Should().Be(1);
        storage.TryAddRef().Should().BeFalse();
    }

    [Fact]
    public void Members_AfterDispose_ShouldThrowObjectDisposed()
    {
        var stream = Open(0, 10, out _);
        stream.Dispose();

        stream.CanRead.Should().BeFalse();
        stream.CanSeek.Should().BeFalse();
        ((Action)(() => stream.Read(new byte[1], 0, 1))).Should().Throw<ObjectDisposedException>();
        ((Action)(() => stream.Seek(0, SeekOrigin.Begin)))
            .Should()
            .Throw<ObjectDisposedException>();
        ((Action)(() => _ = stream.Length)).Should().Throw<ObjectDisposedException>();
        ((Action)(() => _ = stream.Position)).Should().Throw<ObjectDisposedException>();
    }

    private static PackageEntryStream Open(long start, long length, out PackageStorage storage)
    {
        storage = new StreamPackageStorage(new MemoryStream(Data, false), false);

        // 作った側の参照をそのままストリームに渡す
        return new PackageEntryStream(storage, start, length);
    }

    private static int ReadAll(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
                break;
            total += read;
        }

        return total;
    }
}
