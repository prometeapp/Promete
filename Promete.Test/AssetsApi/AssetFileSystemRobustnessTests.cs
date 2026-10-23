using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetFileSystem"/> が、ソースの失敗 (Unmount 後の呼び出し、読み込み中に消えたファイル、Dispose の例外) に耐えることのテスト。
/// </summary>
public class AssetFileSystemRobustnessTests
{
    [Fact]
    public void DisposedSource_ShouldBeTreatedAsNotFound_InAllReadMembers()
    {
        // スナップショットを取った後に Unmount されたソースが、ObjectDisposedException を投げた状況
        using var fs = new AssetFileSystem();
        var disposed = new DisposeSensitiveAssetSource(("a.txt", "disposed"));
        disposed.Dispose();
        fs.Mount(new FileBackedInMemoryAssetSource(("a.txt", "lower")), 0);
        fs.Mount(disposed, 10);

        fs.Exists("a.txt")
            .Should()
            .BeTrue("破棄済みのソースは「無い」扱いで、下のソースを見るはず");
        using (var stream = fs.OpenRead("a.txt"))
            stream.ReadAllText().Should().Be("lower");
        fs.Enumerate("", "*", true).Should().Equal("a.txt");
        fs.TryGetFilePath("a.txt", out var fullPath).Should().BeTrue();
        fullPath.Should().Be("/fake/a.txt");
    }

    [Fact]
    public void DisposedSource_Only_ShouldThrowFileNotFoundInsteadOfObjectDisposed()
    {
        using var fs = new AssetFileSystem();
        var disposed = new DisposeSensitiveAssetSource(("a.txt", "disposed"));
        disposed.Dispose();
        fs.Mount(disposed);

        fs.Exists("a.txt").Should().BeFalse();
        fs.Enumerate("", "*", true).Should().BeEmpty();
        fs.TryGetFilePath("a.txt", out _).Should().BeFalse();
        var act = () => fs.OpenRead("a.txt");
        act.Should()
            .Throw<FileNotFoundException>(
                "ソースの ObjectDisposedException を利用者に見せないはず"
            );
    }

    [Fact]
    public void SourceDisposedBetweenExistsAndOpenRead_ShouldFallThroughToLowerSource()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a.txt", "lower")), 0);
        fs.Mount(
            new DisposeSensitiveAssetSource(("a.txt", "upper")) { DisposeAfterExists = true },
            10
        );

        using var stream = fs.OpenRead("a.txt");

        stream.ReadAllText().Should().Be("lower");
    }

    [Fact]
    public void SourceDisposedBetweenExistsAndTryGetFilePath_ShouldFallThroughToLowerSource()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new FileBackedInMemoryAssetSource(("a.txt", "lower")), 0);
        fs.Mount(
            new DisposeSensitiveAssetSource(("a.txt", "upper")) { DisposeAfterExists = true },
            10
        );

        fs.TryGetFilePath("a.txt", out var fullPath).Should().BeTrue();
        fullPath.Should().Be("/fake/a.txt");
    }

    [Fact]
    public async Task UnmountDuringReads_WithDisposeSensitiveSources_ShouldNeverLeakObjectDisposed()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("base.txt", "base")), 0);
        var stop = 0;
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var mounter = Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                var source = new DisposeSensitiveAssetSource(("base.txt", "base"), ("x.txt", "x"))
                {
                    SpinIterations = 50,
                };
                fs.Mount(source, 10);
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
                            fs.Exists("x.txt");
                            using (var stream = fs.OpenRead("base.txt"))
                            {
                                if (stream.ReadAllText() != "base")
                                    errors.Add(new InvalidDataException("内容が違う"));
                            }

                            _ = fs.Enumerate("", "*", true).ToList();
                            fs.TryGetFilePath("base.txt", out _);
                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                        }
                    }
                })
            )
            .ToArray();

        await Task.WhenAll([mounter, .. readers]);

        errors.Should().BeEmpty("Unmount で破棄されたソースの例外は、読み込み側に届かないはず");
    }

    [Fact]
    public void FileVanishedBetweenExistsAndOpenRead_ShouldThrowFileNotFound()
    {
        // Exists の直後にフォルダごと消えた状況
        using var fs = new AssetFileSystem();
        fs.Mount(
            new DelegateAssetSource
            {
                OnExists = _ => true,
                OnOpenRead = path => throw new DirectoryNotFoundException(path),
            }
        );

        var act = () => fs.OpenRead("a.txt");

        act.Should()
            .Throw<FileNotFoundException>("DirectoryNotFoundException を素通りさせないはず");
    }

    [Fact]
    public void FileVanishedInUpperSource_ShouldFallThroughToLowerSource()
    {
        using var fs = new AssetFileSystem();
        fs.Mount(new InMemoryAssetSource(("a.txt", "lower")), 0);
        fs.Mount(
            new DelegateAssetSource
            {
                OnExists = _ => true,
                OnOpenRead = path => throw new DirectoryNotFoundException(path),
            },
            10
        );

        using var stream = fs.OpenRead("a.txt");

        stream.ReadAllText().Should().Be("lower");
    }

    [Fact]
    public void Dispose_WhenSourceDisposeThrows_ShouldStillDisposeOthersAndThrowAggregate()
    {
        var fs = new AssetFileSystem();
        var first = new DelegateAssetSource { OnDispose = () => throw new IOException("first") };
        var second = new InMemoryAssetSource();
        var third = new DelegateAssetSource { OnDispose = () => throw new IOException("third") };
        fs.Mount(first, 2);
        fs.Mount(second, 1);
        fs.Mount(third, 0);

        var act = () => fs.Dispose();

        act.Should()
            .Throw<AggregateException>()
            .Which.InnerExceptions.Select(e => e.Message)
            .Should()
            .BeEquivalentTo("first", "third");
        first.DisposeCount.Should().Be(1);
        second.DisposeCount.Should().Be(1, "前のソースの Dispose が失敗しても、残りは破棄するはず");
        third.DisposeCount.Should().Be(1);
        fs.Dispose();
        second.DisposeCount.Should().Be(1, "2 回目の Dispose は何もしないはず");
    }
}
