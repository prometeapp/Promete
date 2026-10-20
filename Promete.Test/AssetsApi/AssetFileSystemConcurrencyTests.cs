using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetFileSystem"/> の非同期マウントと、スレッド安全性のテスト。
/// </summary>
public class AssetFileSystemConcurrencyTests
{
    [Fact]
    public async Task MountAsync_ShouldMountCreatedSource()
    {
        using var fs = new AssetFileSystem();
        var source = new InMemoryAssetSource(("a.txt", "async"));

        var mounted = await fs.MountAsync(
            async ct =>
            {
                await Task.Yield();
                return source;
            },
            5
        );

        mounted.Should().BeSameAs(source);
        fs.Sources.Should().Equal(source);
        using var stream = fs.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("async");
    }

    [Fact]
    public async Task MountAsync_ShouldRespectPriority()
    {
        using var fs = new AssetFileSystem();
        var high = new InMemoryAssetSource(("a.txt", "high"));
        fs.Mount(high, 10);

        await fs.MountAsync(
            _ => Task.FromResult<IAssetSource>(new InMemoryAssetSource(("a.txt", "low"))),
            0
        );

        using var stream = fs.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("high");
    }

    [Fact]
    public async Task MountAsync_ShouldPassCancellationToken()
    {
        using var fs = new AssetFileSystem();
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;

        await fs.MountAsync(
            ct =>
            {
                received = ct;
                return Task.FromResult<IAssetSource>(new InMemoryAssetSource());
            },
            0,
            cts.Token
        );

        received.Should().Be(cts.Token);
    }

    [Fact]
    public async Task MountAsync_FactoryThrows_ShouldPropagateAndMountNothing()
    {
        using var fs = new AssetFileSystem();

        var act = () =>
            fs.MountAsync(_ => Task.FromException<IAssetSource>(new IOException("boom")));

        await act.Should().ThrowAsync<IOException>();
        fs.Sources.Should().BeEmpty();
    }

    [Fact]
    public async Task MountAsync_NullFactory_ShouldThrowArgumentNull()
    {
        using var fs = new AssetFileSystem();

        var act = () => fs.MountAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MountAsync_DisposedWhileCreating_ShouldDisposeCreatedSourceAndThrow()
    {
        var fs = new AssetFileSystem();
        var gate = new TaskCompletionSource();
        var source = new InMemoryAssetSource();

        var mountTask = fs.MountAsync(async _ =>
        {
            await gate.Task;
            return source;
        });
        fs.Dispose();
        gate.SetResult();

        await ((Func<Task>)(() => mountTask)).Should().ThrowAsync<ObjectDisposedException>();
        source.DisposeCount.Should().Be(1, "作成中に破棄されたら、作成したソースを破棄するはず");
    }

    [Fact]
    public async Task MountAndUnmountDuringReads_ShouldBeSafe()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("base.txt", "base")), 0);
        var stop = 0;
        var errors = 0;

        var mounter = Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                var source = new InMemoryAssetSource(("extra.txt", "extra"), ("base.txt", "base"));
                fs.Mount(source, (i % 3) - 1);
                fs.Unmount(source);
            }

            Volatile.Write(ref stop, 1);
        });
        var readers = Enumerable
            .Range(0, 4)
            .Select(r =>
                Task.Run(() =>
                {
                    while (Volatile.Read(ref stop) == 0)
                    {
                        try
                        {
                            using var stream = fs.OpenRead("base.txt");
                            if (stream.ReadAllText() != "base")
                                Interlocked.Increment(ref errors);
                            fs.Exists("extra.txt");
                            _ = fs.Enumerate("", "*", true).ToList();
                            fs.TryGetFilePath("base.txt", out _);
                            _ = fs.Sources.Count;
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref errors);
                        }
                    }
                })
            )
            .ToArray();

        await Task.WhenAll([mounter, .. readers]);

        errors.Should().Be(0, "マウントの一覧はコピーオンライトのスナップショットで読むはず");
        fs.Sources.Should().HaveCount(1);
    }

    [Fact]
    public void ConcurrentMounts_ShouldAllBeRegistered()
    {
        using var fs = new AssetFileSystem();
        var sources = Enumerable.Range(0, 200).Select(_ => new InMemoryAssetSource()).ToArray();

        Parallel.ForEach(sources, (source, _, i) => fs.Mount(source, (int)(i % 7)));

        fs.Sources.Should().HaveCount(200).And.OnlyHaveUniqueItems();
        fs.Sources.Should().Contain(sources);
    }
}
