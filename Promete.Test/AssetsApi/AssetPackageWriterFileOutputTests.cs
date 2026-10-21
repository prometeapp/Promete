using FluentAssertions;
using Promete.AssetPacker;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetPackageWriter.WriteTo(string)"/> のテスト。書きかけのファイルを残さないこと。
/// </summary>
public sealed class AssetPackageWriterFileOutputTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void WriteToPath_ShouldCreateSameBytesAsStream()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", RawPackageLayout.CompressibleText());
        writer.Add("b.png", [1, 2, 3]);
        var path = Path.Combine(_dir.Path, "game.main.pak");

        writer.WriteTo(path);

        using var expected = new MemoryStream();
        writer.WriteTo(expected);
        File.ReadAllBytes(path).Should().Equal(expected.ToArray());
        Directory.GetFiles(_dir.Path).Should().ContainSingle("一時ファイルは残らないはず");
    }

    [Fact]
    public void WriteToPath_ShouldOverwriteExistingFile()
    {
        var path = _dir.CreateFile("game.main.pak", new string('x', 10000));
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", [1]);

        writer.WriteTo(path);

        RawPackage.Parse(File.ReadAllBytes(path)).Entry("a.txt").Content.Should().Equal(1);
        Directory.GetFiles(_dir.Path).Should().ContainSingle();
    }

    [Fact]
    public void WriteToPath_Failure_ShouldKeepExistingFileAndLeaveNoTemporaryFile()
    {
        var path = _dir.CreateFile("out/game.main.pak", "old");
        var source = _dir.CreateFile("src/a.txt", "a");
        var writer = new AssetPackageWriter();
        writer.Add("first.txt", [1]);
        writer.AddFile("z.txt", source);
        File.Delete(source);

        var act = () => writer.WriteTo(path);

        act.Should().Throw<FileNotFoundException>();
        File.ReadAllText(path).Should().Be("old");
        Directory.GetFiles(Path.GetDirectoryName(path)!).Should().ContainSingle();
    }

    [Fact]
    public void WriteToPath_FailureWithoutExistingFile_ShouldLeaveNothing()
    {
        var outDir = Path.Combine(_dir.Path, "out");
        Directory.CreateDirectory(outDir);
        var source = _dir.CreateFile("src/a.txt", "a");
        var writer = new AssetPackageWriter();
        writer.AddFile("a.txt", source);
        File.Delete(source);

        var act = () => writer.WriteTo(Path.Combine(outDir, "game.main.pak"));

        act.Should().Throw<FileNotFoundException>();
        Directory.GetFiles(outDir).Should().BeEmpty();
    }

    [Fact]
    public void WriteToPath_Null_ShouldThrowArgumentNull()
    {
        var act = () => new AssetPackageWriter().WriteTo((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
