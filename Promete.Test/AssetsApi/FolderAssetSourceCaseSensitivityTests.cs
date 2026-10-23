using System.Text;
using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="FolderAssetSource"/> の大文字・小文字の厳密照合、NFC、ディレクトリ名のキャッシュ、並行アクセスのテスト。
/// </summary>
public sealed class FolderAssetSourceCaseSensitivityTests : IDisposable
{
    private const string NfdPa = "パ";
    private const string NfcPa = "パ";

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Exists_CaseMismatchedFileName_ShouldBeFalse()
    {
        _dir.CreateFile("Player.png");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists("Player.png").Should().BeTrue();
        source
            .Exists("player.png")
            .Should()
            .BeFalse("ファイルシステムが区別しなくても区別するはず");
        source.Exists("PLAYER.PNG").Should().BeFalse();
    }

    [Fact]
    public void OpenRead_CaseMismatchedFileName_ShouldThrowWithActualName()
    {
        _dir.CreateFile("Player.png");
        var source = new FolderAssetSource(_dir.Path);

        var act = () => source.OpenRead("player.png");

        var ex = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        ex.RequestedPath.Should().Be("player.png");
        ex.ActualPath.Should().Be("Player.png");
        ex.Message.Should().Contain("'Player.png'").And.Contain("大文字・小文字を区別");
    }

    [Fact]
    public void OpenRead_CaseMismatchedDirectory_ShouldThrowWithActualPath()
    {
        _dir.CreateFile("Sub/Dir/a.txt");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists("sub/dir/a.txt").Should().BeFalse();
        var act = () => source.OpenRead("sub/Dir/a.txt");

        var ex = act.Should().Throw<AssetPathCaseMismatchException>().Which;
        ex.ActualPath.Should().Be("Sub/Dir/a.txt", "ディレクトリも含めて実際の名前を示すはず");
    }

    [Fact]
    public void OpenRead_CaseMismatchedMountPoint_ShouldThrowWithActualPath()
    {
        _dir.CreateFile("x.png");
        var source = new FolderAssetSource(_dir.Path, "assets");

        source.Exists("Assets/x.png").Should().BeFalse();
        var act = () => source.OpenRead("Assets/x.png");

        act.Should()
            .Throw<AssetPathCaseMismatchException>()
            .Which.ActualPath.Should()
            .Be("assets/x.png");
    }

    [Fact]
    public void TryGetFilePath_CaseMismatched_ShouldReturnFalse()
    {
        _dir.CreateFile("Player.png");
        var source = new FolderAssetSource(_dir.Path);

        source.TryGetFilePath("player.png", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetFilePath_ShouldReturnActualCasing()
    {
        _dir.CreateFile("Sub/Player.png");
        var source = new FolderAssetSource(_dir.Path);

        source.TryGetFilePath("Sub/Player.png", out var fullPath).Should().BeTrue();
        Path.GetFileName(fullPath).Should().Be("Player.png");
        Path.GetFileName(Path.GetDirectoryName(fullPath)).Should().Be("Sub");
    }

    [Fact]
    public void Enumerate_CaseMismatchedDirectory_ShouldBeEmpty()
    {
        _dir.CreateFile("Sub/a.txt");
        var source = new FolderAssetSource(_dir.Path);

        source
            .Enumerate("sub", "*", true)
            .Should()
            .BeEmpty("ディレクトリ名も序数比較で照合するはず");
        source.Enumerate("Sub", "*", true).Should().BeEquivalentTo("Sub/a.txt");
    }

    [Fact]
    public void FileAddedAfterLookup_ShouldBeFoundByReenumeration()
    {
        _dir.CreateFile("a.txt");
        var source = new FolderAssetSource(_dir.Path);
        source.Exists("a.txt").Should().BeTrue();
        source.Exists("b.txt").Should().BeFalse();

        _dir.CreateFile("b.txt", "new");

        source.Exists("b.txt").Should().BeTrue("見つからなかったときは列挙し直すはず");
        using var stream = source.OpenRead("b.txt");
        stream.ReadAllText().Should().Be("new");
    }

    [Fact]
    public void DirectoryAddedAfterLookup_ShouldBeFoundByReenumeration()
    {
        _dir.CreateFile("a.txt");
        var source = new FolderAssetSource(_dir.Path);
        source.Exists("sub/b.txt").Should().BeFalse();

        _dir.CreateFile("sub/b.txt");

        source.Exists("sub/b.txt").Should().BeTrue();
    }

    [Fact]
    public void FileDeletedAfterLookup_ShouldNotExist()
    {
        var path = _dir.CreateFile("a.txt");
        var source = new FolderAssetSource(_dir.Path);
        source.Exists("a.txt").Should().BeTrue();

        File.Delete(path);

        source
            .Exists("a.txt")
            .Should()
            .BeFalse("キャッシュに名前が残っていても、実ファイルが無ければ false のはず");
        var act = () => source.OpenRead("a.txt");
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Enumerate_ShouldSeeFilesAddedLater()
    {
        _dir.CreateFile("a.txt");
        var source = new FolderAssetSource(_dir.Path);
        source.Enumerate("", "*", false).Should().BeEquivalentTo("a.txt");

        _dir.CreateFile("b.txt");

        source.Enumerate("", "*", false).Should().BeEquivalentTo("a.txt", "b.txt");
    }

    [Fact]
    public void NfdFileName_ShouldBeFoundByNfcPath()
    {
        // macOS 由来のファイル名は NFD になっていることがある
        _dir.CreateFile($"{NfdPa}/{NfdPa}.txt", "pa");
        var source = new FolderAssetSource(_dir.Path);

        source.Exists($"{NfcPa}/{NfcPa}.txt").Should().BeTrue();
        source
            .Exists($"{NfdPa}/{NfdPa}.txt")
            .Should()
            .BeTrue("利用者が渡したパスも NFC に正規化するはず");
        using var stream = source.OpenRead($"{NfcPa}/{NfcPa}.txt");
        stream.ReadAllText().Should().Be("pa");
    }

    [Fact]
    public void NfdFileName_ShouldBeEnumeratedAsNfc()
    {
        _dir.CreateFile($"{NfdPa}.txt");
        var source = new FolderAssetSource(_dir.Path);

        var names = source.Enumerate("", "*", false).ToList();

        names.Should().ContainSingle().Which.Should().Be($"{NfcPa}.txt");
        names[0].IsNormalized(NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void NfdFileName_TryGetFilePath_ShouldReturnActualPhysicalName()
    {
        _dir.CreateFile($"{NfdPa}.txt");
        var source = new FolderAssetSource(_dir.Path);

        source.TryGetFilePath($"{NfcPa}.txt", out var fullPath).Should().BeTrue();
        File.Exists(fullPath).Should().BeTrue("実際に存在する (NFD の) ファイル名を返すはず");
    }

    [Fact]
    public void ConcurrentFirstLookups_ShouldAllSucceed()
    {
        const int dirCount = 10;
        const int fileCount = 20;
        for (var d = 0; d < dirCount; d++)
        for (var f = 0; f < fileCount; f++)
            _dir.CreateFile($"d{d}/f{f}.txt", $"{d}-{f}");
        var source = new FolderAssetSource(_dir.Path);

        var failures = 0;
        Parallel.For(
            0,
            dirCount * fileCount * 4,
            new ParallelOptions { MaxDegreeOfParallelism = 16 },
            i =>
            {
                var d = i % dirCount;
                var f = (i / dirCount) % fileCount;
                var path = $"d{d}/f{f}.txt";
                if (!source.Exists(path))
                {
                    Interlocked.Increment(ref failures);
                    return;
                }

                using var stream = source.OpenRead(path);
                if (stream.ReadAllText() != $"{d}-{f}")
                    Interlocked.Increment(ref failures);
            }
        );

        failures.Should().Be(0, "ディレクトリ名のキャッシュは並行アクセスに耐えるはず");
    }

    [Fact]
    public async Task ConcurrentLookupsWhileFilesAreAdded_ShouldNotThrow()
    {
        var source = new FolderAssetSource(_dir.Path);

        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
                _dir.CreateFile($"w/f{i}.txt", "x");
        });
        var readers = Enumerable
            .Range(0, 4)
            .Select(r =>
                Task.Run(() =>
                {
                    for (var i = 0; i < 400; i++)
                    {
                        source.Exists($"w/f{i % 100}.txt");
                        _ = source.Enumerate("w", "*", false).ToList();
                    }
                })
            )
            .ToArray();

        var act = () => Task.WhenAll([writer, .. readers]);

        await act.Should().NotThrowAsync();
        source
            .Enumerate("w", "*", false)
            .Should()
            .HaveCount(100, "書き込みの完了後は全部見えるはず");
    }
}
