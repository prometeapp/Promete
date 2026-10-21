using System.Text;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Promete.AssetPacker.Tasks;
using Promete.Assets;
using Promete.Test.AssetsApi;

namespace Promete.Test.AssetsBuild;

/// <summary>
/// パッケージを書き出す MSBuild Task (<see cref="PackPrometeAssets"/>) の単体テスト。
/// </summary>
public sealed class PackPrometeAssetsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void ShouldWriteOnePackagePerPackageName()
    {
        var a = _dir.CreateFile("src/a.txt", "a");
        var b = _dir.CreateFile("src/b.txt", "b");
        var c = _dir.CreateFile("src/c.txt", "c");
        var output = Path.Combine(_dir.Path, "obj");

        var (task, engine) = Run(
            output,
            Resolved(a, "assets/a.txt", package: "main"),
            Resolved(b, "assets/b.txt", package: "extra"),
            Resolved(c, "assets/c.txt", package: "main")
        );

        engine.Errors.Should().BeEmpty();
        task.Result.Should().BeTrue();
        task.Packages.Select(p => p.GetMetadata("PackageName")).Should().Equal("extra", "main");
        task.Packages.Select(p => p.GetMetadata("Location"))
            .Should()
            .Equal("Game.extra.prmtpak", "Game.main.prmtpak");
        task.Packages.Select(p => p.ItemSpec)
            .Should()
            .Equal(
                Path.Combine(output, "Game.extra.prmtpak"),
                Path.Combine(output, "Game.main.prmtpak")
            );

        using var main = new PackageAssetSource(Path.Combine(output, "Game.main.prmtpak"));
        main.Enumerate("", "*", true).Should().BeEquivalentTo("assets/a.txt", "assets/c.txt");
        ReadText(main, "assets/c.txt").Should().Be("c");
        using var extra = new PackageAssetSource(Path.Combine(output, "Game.extra.prmtpak"));
        extra.Enumerate("", "*", true).Should().Equal("assets/b.txt");
    }

    [Fact]
    public void Compress_ShouldMapToWriterCompression()
    {
        var text = Encoding.UTF8.GetString(RawPackageLayout.CompressibleText());
        var stored = _dir.CreateFile("src/stored.txt", text);
        var forced = _dir.CreateFile("src/forced.png", text);
        var auto = _dir.CreateFile("src/auto.txt", text);
        var autoPng = _dir.CreateFile("src/auto.png", text);
        var output = Path.Combine(_dir.Path, "obj");

        Run(
            output,
            Resolved(stored, "stored.txt", compress: "false"),
            Resolved(forced, "forced.png", compress: "true"),
            Resolved(auto, "auto.txt", compress: "auto"),
            Resolved(autoPng, "auto.png", compress: "auto")
        );

        var raw = RawPackage.Parse(File.ReadAllBytes(Path.Combine(output, "Game.main.prmtpak")));
        Flags(raw, "stored.txt").Should().Be(0u);
        Flags(raw, "forced.png").Should().Be(RawPackageLayout.EntryFlagDeflate);
        Flags(raw, "auto.txt").Should().Be(RawPackageLayout.EntryFlagDeflate);
        Flags(raw, "auto.png").Should().Be(0u);
    }

    [Fact]
    public void Obfuscate_ShouldBeIgnoredInThisVersion()
    {
        var a = _dir.CreateFile("src/a.txt", "a");
        var output = Path.Combine(_dir.Path, "obj");

        var (task, engine) = Run(output, Resolved(a, "a.txt", obfuscate: "true"));

        task.Result.Should().BeTrue();
        engine.Errors.Should().BeEmpty();
        var raw = RawPackage.Parse(File.ReadAllBytes(Path.Combine(output, "Game.main.prmtpak")));
        raw.Flags.Should().Be(0u);
        raw.Entries.Single().Flags.Should().Be(0u);
    }

    [Fact]
    public void ShouldOverwriteExistingPackage()
    {
        var a = _dir.CreateFile("src/a.txt", "a");
        var b = _dir.CreateFile("src/b.txt", "b");
        var output = Path.Combine(_dir.Path, "obj");
        Run(output, Resolved(a, "a.txt"), Resolved(b, "b.txt"));

        Run(output, Resolved(a, "a.txt"));

        using var source = new PackageAssetSource(Path.Combine(output, "Game.main.prmtpak"));
        source.Enumerate("", "*", true).Should().Equal("a.txt");
    }

    [Fact]
    public void MissingInputFile_ShouldReportPROMETE1020()
    {
        var output = Path.Combine(_dir.Path, "obj");

        var (task, engine) = Run(output, Resolved(Path.Combine(_dir.Path, "missing.txt"), "a.txt"));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1020");
    }

    [Fact]
    public void Empty_ShouldWriteNothing()
    {
        var output = Path.Combine(_dir.Path, "obj");

        var (task, engine) = Run(output);

        task.Result.Should().BeTrue();
        engine.Errors.Should().BeEmpty();
        task.Packages.Should().BeEmpty();
    }

    private static uint Flags(RawPackage raw, string path) =>
        raw.Entries.Single(e => e.Path == path).Flags;

    private static string ReadText(IAssetSource source, string path)
    {
        using var reader = new StreamReader(source.OpenRead(path));
        return reader.ReadToEnd();
    }

    private static (TaskResult Task, FakeBuildEngine Engine) Run(
        string outputDirectory,
        params ITaskItem[] items
    )
    {
        var engine = new FakeBuildEngine();
        var task = new PackPrometeAssets
        {
            BuildEngine = engine,
            Assets = items,
            OutputDirectory = outputDirectory,
            FileNamePrefix = "Game.",
            FileExtension = ".prmtpak",
        };
        var result = task.Execute();
        return (new TaskResult(result, task.Packages), engine);
    }

    private static TaskItem Resolved(
        string fullPath,
        string logicalPath,
        string package = "main",
        string compress = "auto",
        string obfuscate = "false"
    )
    {
        var item = new TaskItem(fullPath);
        item.SetMetadata("LogicalPath", logicalPath);
        item.SetMetadata("Package", package);
        item.SetMetadata("Compress", compress);
        item.SetMetadata("Obfuscate", obfuscate);
        return item;
    }

    private sealed record TaskResult(bool Result, ITaskItem[] Packages);
}
