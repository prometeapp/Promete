using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="PackageAssetSource"/> で、複数のストリームを同時に開いて別々のスレッドから読めることのテスト。
/// </summary>
public sealed class PackageAssetSourceConcurrencyTests : IDisposable
{
    private const int EntryCount = 6;
    private const int ThreadCount = 8;

    private static readonly byte[][] Contents = Enumerable
        .Range(0, EntryCount)
        .Select(i =>
            i % 2 == 0
                ? RawPackageLayout.RandomBytes(64 * 1024, 200 + i)
                : RawPackageLayout.CompressibleText("concurrent" + i, 2000)
        )
        .ToArray();

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    public static TheoryData<string> Kinds => new() { "stream", "file" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ManyStreamsReadFromManyThreads_ShouldEachSeeCorrectBytes(string kind)
    {
        using var source = Create(kind);
        using var barrier = new Barrier(ThreadCount);

        var tasks = Enumerable
            .Range(0, ThreadCount)
            .Select(t =>
                Task.Factory.StartNew(
                    () =>
                    {
                        // 各スレッドは自分のストリームを開き、seek と読み出しを繰り返す
                        var index = t % EntryCount;
                        var expected = Contents[index];
                        using var stream = source.OpenRead(PathOf(index));
                        var random = new Random(t);
                        var buffer = new byte[257];
                        barrier.SignalAndWait();
                        for (var i = 0; i < 500; i++)
                        {
                            var position = random.Next(0, expected.Length - buffer.Length);
                            stream.Position = position;
                            stream.ReadExactly(buffer);
                            if (
                                !buffer
                                    .AsSpan()
                                    .SequenceEqual(expected.AsSpan(position, buffer.Length))
                            )
                                throw new InvalidOperationException(
                                    $"スレッド {t} の位置 {position} の読み出しが一致しません。"
                                );
                        }
                    },
                    TaskCreationOptions.LongRunning
                )
            )
            .ToArray();

        var act = () => Task.WaitAll(tasks);
        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void OpenReadFromManyThreads_ShouldReturnWholeContents(string kind)
    {
        using var source = Create(kind);
        using var barrier = new Barrier(ThreadCount);

        var tasks = Enumerable
            .Range(0, ThreadCount)
            .Select(t =>
                Task.Factory.StartNew(
                    () =>
                    {
                        barrier.SignalAndWait();
                        for (var i = 0; i < 30; i++)
                        {
                            var index = (t + i) % EntryCount;
                            using var stream = source.OpenRead(PathOf(index));
                            if (
                                !PackageAssetSourceTests
                                    .ReadAllBytes(stream)
                                    .AsSpan()
                                    .SequenceEqual(Contents[index])
                            )
                                throw new InvalidOperationException(
                                    $"'{PathOf(index)}' の中身が一致しません。"
                                );
                        }
                    },
                    TaskCreationOptions.LongRunning
                )
            )
            .ToArray();

        var act = () => Task.WaitAll(tasks);
        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void DisposeWhileReading_ShouldNotDisturbOpenedStreams(string kind)
    {
        var source = Create(kind);
        var streams = Enumerable
            .Range(0, EntryCount)
            .Select(i => source.OpenRead(PathOf(i)))
            .ToArray();
        using var started = new CountdownEvent(EntryCount);

        var tasks = streams
            .Select(
                (stream, index) =>
                    Task.Factory.StartNew(
                        () =>
                        {
                            using (stream)
                            {
                                var buffer = new byte[1024];
                                var total = new MemoryStream();
                                var first = true;
                                int read;
                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    total.Write(buffer, 0, read);
                                    if (first)
                                    {
                                        first = false;
                                        started.Signal();
                                    }

                                    Thread.SpinWait(200);
                                }

                                if (!total.ToArray().AsSpan().SequenceEqual(Contents[index]))
                                    throw new InvalidOperationException(
                                        $"'{PathOf(index)}' の中身が一致しません。"
                                    );
                            }
                        },
                        TaskCreationOptions.LongRunning
                    )
            )
            .ToArray();

        started.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        source.Dispose();

        var act = () => Task.WaitAll(tasks);
        act.Should().NotThrow();
    }

    [Fact]
    public void OpenReadRacingWithDispose_ShouldSucceedOrThrowObjectDisposed()
    {
        for (var round = 0; round < 20; round++)
        {
            var source = Create("stream");
            using var barrier = new Barrier(5);
            var opened = new System.Collections.Concurrent.ConcurrentBag<Stream>();
            var readers = Enumerable
                .Range(0, 4)
                .Select(t =>
                    Task.Factory.StartNew(
                        () =>
                        {
                            barrier.SignalAndWait();
                            for (var i = 0; i < 50; i++)
                            {
                                try
                                {
                                    opened.Add(source.OpenRead(PathOf(i % EntryCount)));
                                }
                                catch (ObjectDisposedException)
                                {
                                    return;
                                }
                            }
                        },
                        TaskCreationOptions.LongRunning
                    )
                )
                .ToArray();

            barrier.SignalAndWait();
            source.Dispose();
            Task.WaitAll(readers);

            // Dispose より後に開けたストリームも、最後まで正しく読めること
            foreach (var stream in opened)
            {
                using (stream)
                    stream.Length.Should().BeGreaterThan(0);
            }
        }
    }

    private static string PathOf(int index) =>
        index % 2 == 0 ? $"bin/{index}.bin" : $"text/{index}.txt";

    private PackageAssetSource Create(string kind)
    {
        var writer = new AssetPackageWriter();
        for (var i = 0; i < EntryCount; i++)
            writer.Add(PathOf(i), Contents[i]);
        if (kind == "file")
        {
            var path = Path.Combine(_dir.Path, Guid.NewGuid().ToString("N") + ".pak");
            writer.WriteTo(path);
            return new PackageAssetSource(path);
        }

        var output = new MemoryStream();
        writer.WriteTo(output);
        output.Position = 0;
        return new PackageAssetSource(output);
    }
}
