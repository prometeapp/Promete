using FluentAssertions;
using Promete.Assets.Packaging;

namespace Promete.Test.AssetsApi;

/// <summary>
/// パッケージの読み出し元 (<see cref="StreamPackageStorage"/> / <see cref="FilePackageStorage"/>) の、位置を指定した読み出しと参照カウントのテスト。
/// </summary>
public sealed class PackageStorageTests : IDisposable
{
    private static readonly byte[] Data = RawPackageLayout.RandomBytes(10000, 7);

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    public static TheoryData<string> Kinds => new() { "stream", "file" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReadAt_ShouldReadFromGivenPosition(string kind)
    {
        var storage = Create(kind);
        var buffer = new byte[100];

        var read = storage.ReadAt(1234, buffer);

        read.Should().BeGreaterThan(0);
        buffer[..read].Should().Equal(Data[1234..(1234 + read)]);
        storage.Release();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Length_ShouldBeWholeLength(string kind)
    {
        var storage = Create(kind);

        storage.Length.Should().Be(Data.Length);
        storage.Release();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReadAt_AtEnd_ShouldReturnZero(string kind)
    {
        var storage = Create(kind);

        storage.ReadAt(Data.Length, new byte[10]).Should().Be(0);
        storage.Release();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReadExactlyAt_ShouldFillBufferOrThrowAtEnd(string kind)
    {
        var storage = Create(kind);
        var buffer = new byte[5000];

        storage.ReadExactlyAt(4000, buffer);
        buffer.Should().Equal(Data[4000..9000]);

        var act = () => storage.ReadExactlyAt(Data.Length - 10, new byte[20]);
        act.Should().Throw<EndOfStreamException>();
        storage.Release();
    }

    [Fact]
    public void StreamStorage_PositionBeyondEnd_ShouldHaveZeroLength()
    {
        var stream = new MemoryStream(Data, false);
        stream.Position = Data.Length + 10;
        var storage = new StreamPackageStorage(stream, false);

        storage.Length.Should().Be(0);
        storage.ReadAt(0, new byte[10]).Should().Be(0);
        storage.Release();
    }

    [Fact]
    public void StreamStorage_ShouldTreatInitialPositionAsOrigin()
    {
        var bytes = new byte[] { 0xEE, 0xEE, 0xEE }
            .Concat(Data)
            .ToArray();
        var stream = new MemoryStream(bytes);
        stream.Position = 3;
        var storage = new StreamPackageStorage(stream, false);

        storage.Length.Should().Be(Data.Length);
        var buffer = new byte[4];
        storage.ReadExactlyAt(0, buffer);
        buffer.Should().Equal(Data[..4]);
        storage.Release();
    }

    [Fact]
    public void StreamStorage_LastRelease_ShouldDisposeStreamUnlessLeaveOpen()
    {
        var owned = new ProbeStream(Data);
        var borrowed = new ProbeStream(Data);
        var ownedStorage = new StreamPackageStorage(owned, false);
        var borrowedStorage = new StreamPackageStorage(borrowed, true);

        ownedStorage.TryAddRef().Should().BeTrue();
        ownedStorage.Release();
        owned.IsDisposed.Should().BeFalse("まだ参照が 1 つ残っている");
        ownedStorage.Release();
        borrowedStorage.Release();

        owned.DisposeCount.Should().Be(1);
        borrowed.IsDisposed.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void TryAddRef_AfterLastRelease_ShouldFail(string kind)
    {
        var storage = Create(kind);
        storage.Release();

        storage.TryAddRef().Should().BeFalse();
    }

    [Fact]
    public void FileStorage_LastRelease_ShouldCloseFile()
    {
        var path = WriteDataFile();
        var storage = new FilePackageStorage(path);
        storage.TryAddRef().Should().BeTrue();

        storage.Release();
        if (OperatingSystem.IsWindows())
        {
            var whileOpen = () =>
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None).Dispose();
            whileOpen.Should().Throw<IOException>("参照が残っている間はファイルを開いたまま");
        }

        storage.Release();
        var afterClose = () =>
            new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None).Dispose();
        afterClose.Should().NotThrow("参照がなくなったらファイルを閉じるはず");
    }

    [Fact]
    public void FileStorage_MissingFile_ShouldThrow()
    {
        var act = () => new FilePackageStorage(Path.Combine(_dir.Path, "missing.pak"));

        act.Should().Throw<FileNotFoundException>();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReadAt_FromManyThreads_ShouldReturnCorrectBytes(string kind)
    {
        var storage = Create(kind);
        using var barrier = new Barrier(8);

        var tasks = Enumerable
            .Range(0, 8)
            .Select(t =>
                Task.Factory.StartNew(
                    () =>
                    {
                        var random = new Random(t);
                        var buffer = new byte[64];
                        barrier.SignalAndWait();
                        for (var i = 0; i < 2000; i++)
                        {
                            var position = random.Next(0, Data.Length - buffer.Length);
                            storage.ReadExactlyAt(position, buffer);
                            if (
                                !buffer.AsSpan().SequenceEqual(Data.AsSpan(position, buffer.Length))
                            )
                                throw new InvalidOperationException(
                                    $"位置 {position} の読み出しが一致しません。"
                                );
                        }
                    },
                    TaskCreationOptions.LongRunning
                )
            )
            .ToArray();

        var act = () => Task.WaitAll(tasks);
        act.Should().NotThrow();
        storage.Release();
    }

    private PackageStorage Create(string kind) =>
        kind == "file"
            ? new FilePackageStorage(WriteDataFile())
            : new StreamPackageStorage(new MemoryStream(Data, false), false);

    private string WriteDataFile()
    {
        var path = Path.Combine(_dir.Path, Guid.NewGuid().ToString("N") + ".bin");
        File.WriteAllBytes(path, Data);
        return path;
    }
}
