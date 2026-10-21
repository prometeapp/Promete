using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Promete.AssetPacker.Tasks;
using Promete.Assets.Infrastructure;
using Promete.Test.AssetsApi;

namespace Promete.Test.AssetsBuild;

/// <summary>
/// デスクトップ形式のマニフェストを書く MSBuild Task (<see cref="WritePrometeAssetManifest"/>) の単体テスト。
/// 書いたものを実行時の <see cref="DesktopAssetManifest"/> で読めることも確かめる。
/// </summary>
public sealed class WritePrometeAssetManifestTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Output_ShouldBeReadableByRuntime()
    {
        var a = _dir.CreateFile("src/a.png", "a");
        var b = _dir.CreateFile("src/sub/b.txt", "b");
        var output = Path.Combine(_dir.Path, "obj", "Game.promete-assets.json");

        var (result, engine, _) = Run(
            output,
            Resolved(a, "assets/a.png"),
            Resolved(b, "assets/sub/b.txt")
        );

        result.Should().BeTrue();
        engine.Errors.Should().BeEmpty();
        var files = DesktopAssetManifest.Read(output);
        files.Should().HaveCount(2);
        files["assets/a.png"].Should().Be(a);
        files["assets/sub/b.txt"].Should().Be(b);
    }

    [Fact]
    public void Output_ShouldBeUtf8WithoutBom()
    {
        var output = Path.Combine(_dir.Path, "m.json");

        Run(output, Resolved(_dir.CreateFile("a.txt"), "a.txt"));

        File.ReadAllBytes(output)[0].Should().Be((byte)'{');
    }

    [Fact]
    public void Output_ShouldEscapeSpecialCharacters()
    {
        var file = _dir.CreateFile("a.txt", "a");
        var output = Path.Combine(_dir.Path, "m.json");
        var logical = "quote\"and\ttab/日本.txt";

        Run(output, Resolved(file, logical));

        DesktopAssetManifest.Read(output).Should().ContainKey(logical);
    }

    [Fact]
    public void Output_ShouldNotDependOnInputOrder()
    {
        var a = _dir.CreateFile("a.txt");
        var b = _dir.CreateFile("b.txt");
        var first = Path.Combine(_dir.Path, "first.json");
        var second = Path.Combine(_dir.Path, "second.json");

        Run(first, Resolved(a, "x/a.txt"), Resolved(b, "x/B.txt"));
        Run(second, Resolved(b, "x/B.txt"), Resolved(a, "x/a.txt"));

        File.ReadAllBytes(first).Should().Equal(File.ReadAllBytes(second));
    }

    [Fact]
    public void SameContent_ShouldNotRewriteFile()
    {
        var a = _dir.CreateFile("a.txt");
        var output = Path.Combine(_dir.Path, "m.json");
        Run(output, Resolved(a, "a.txt"));
        var past = DateTime.UtcNow.AddHours(-1);
        File.SetLastWriteTimeUtc(output, past);

        var (result, _, written) = Run(output, Resolved(a, "a.txt"));

        result.Should().BeTrue();
        written.Should().BeFalse();
        File.GetLastWriteTimeUtc(output).Should().Be(past);
    }

    [Fact]
    public void ChangedContent_ShouldRewriteFile()
    {
        var a = _dir.CreateFile("a.txt");
        var b = _dir.CreateFile("b.txt");
        var output = Path.Combine(_dir.Path, "m.json");
        var (_, _, firstWritten) = Run(output, Resolved(a, "a.txt"));

        var (_, _, written) = Run(output, Resolved(a, "a.txt"), Resolved(b, "b.txt"));

        firstWritten.Should().BeTrue();
        written.Should().BeTrue();
        DesktopAssetManifest.Read(output).Should().HaveCount(2);
    }

    [Fact]
    public void RemovedItem_ShouldDisappearFromManifest()
    {
        var a = _dir.CreateFile("a.txt");
        var b = _dir.CreateFile("b.txt");
        var output = Path.Combine(_dir.Path, "m.json");
        Run(output, Resolved(a, "a.txt"), Resolved(b, "b.txt"));

        Run(output, Resolved(a, "a.txt"));

        DesktopAssetManifest.Read(output).Keys.Should().Equal("a.txt");
    }

    [Fact]
    public void Empty_ShouldWriteManifestWithNoFiles()
    {
        var output = Path.Combine(_dir.Path, "deep", "dir", "m.json");

        var (result, _, _) = Run(output);

        result.Should().BeTrue();
        DesktopAssetManifest.Read(output).Should().BeEmpty();
    }

    private static (bool Result, FakeBuildEngine Engine, bool Written) Run(
        string output,
        params ITaskItem[] items
    )
    {
        var engine = new FakeBuildEngine();
        var task = new WritePrometeAssetManifest
        {
            BuildEngine = engine,
            Assets = items,
            OutputFile = output,
        };
        var result = task.Execute();
        return (result, engine, task.Written);
    }

    private static TaskItem Resolved(string fullPath, string logicalPath)
    {
        var item = new TaskItem(fullPath);
        item.SetMetadata("LogicalPath", logicalPath);
        item.SetMetadata("Package", "main");
        item.SetMetadata("Compress", "auto");
        item.SetMetadata("Obfuscate", "false");
        return item;
    }
}
