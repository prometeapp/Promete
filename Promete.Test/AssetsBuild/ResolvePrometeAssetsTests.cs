using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Promete.AssetPacker.Tasks;
using Promete.Test.AssetsApi;

namespace Promete.Test.AssetsBuild;

/// <summary>
/// <c>PrometeAsset</c> の検証と正規化をする MSBuild Task (<see cref="ResolvePrometeAssets"/>) の単体テスト。
/// </summary>
public sealed class ResolvePrometeAssetsTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly string _project;

    public ResolvePrometeAssetsTests()
    {
        _project = Path.Combine(_dir.Path, "Game");
        Directory.CreateDirectory(_project);
    }

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void DefaultTargetPath_ShouldBeIncludePathWithSlashes()
    {
        CreateFile("assets/sub/a.png");

        var (task, engine) = Run(Item("assets\\sub\\a.png"));

        engine.Errors.Should().BeEmpty();
        var resolved = task.ResolvedAssets.Should().ContainSingle().Which;
        resolved.GetMetadata("LogicalPath").Should().Be("assets/sub/a.png");
        resolved.ItemSpec.Should().Be(Path.Combine(_project, "assets", "sub", "a.png"));
        resolved.GetMetadata("SourceItemSpec").Should().Be("assets\\sub\\a.png");
    }

    [Theory]
    [InlineData("data/a.png", "data/a.png")]
    [InlineData("data\\a.png", "data/a.png")]
    [InlineData("/data/a.png", "data/a.png")]
    [InlineData("./data/./a.png", "data/a.png")]
    public void ExplicitTargetPath_ShouldBeNormalized(string targetPath, string expected)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("TargetPath", targetPath)));

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Single().GetMetadata("LogicalPath").Should().Be(expected);
    }

    [Theory]
    [InlineData("../a.png")]
    [InlineData("data/../../a.png")]
    [InlineData("data/../a.png")]
    [InlineData("C:/a.png")]
    [InlineData("C:\\a.png")]
    [InlineData("//server/a.png")]
    [InlineData("\\\\server\\a.png")]
    [InlineData(".")]
    public void ExplicitTargetPath_Invalid_ShouldReportPROMETE1002(string targetPath)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("TargetPath", targetPath)));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1002");
    }

    [Fact]
    public void IncludeOutsideProject_WithoutTargetPath_ShouldReportPROMETE1001()
    {
        File.WriteAllText(Path.Combine(_dir.Path, "shared.png"), "x");

        var (task, engine) = Run(Item("../shared.png"));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1001");
        engine.Errors[0].Message.Should().Contain("TargetPath");
    }

    [Fact]
    public void AbsoluteInclude_WithoutTargetPath_ShouldReportPROMETE1001()
    {
        var file = CreateFile("assets/a.png");

        var (task, engine) = Run(Item(file));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1001");
    }

    [Fact]
    public void IncludeOutsideProject_WithTargetPath_ShouldResolve()
    {
        File.WriteAllText(Path.Combine(_dir.Path, "shared.png"), "x");

        var (task, engine) = Run(Item("../shared.png", ("TargetPath", "assets/shared.png")));

        engine.Errors.Should().BeEmpty();
        var resolved = task.ResolvedAssets.Single();
        resolved.GetMetadata("LogicalPath").Should().Be("assets/shared.png");
        resolved.ItemSpec.Should().Be(Path.Combine(_dir.Path, "shared.png"));
    }

    [Fact]
    public void NfdFileName_ShouldBeNormalizedToNfc()
    {
        CreateFile("assets/\u304B\u3099.png");

        var (task, engine) = Run(Item("assets/\u304B\u3099.png"));

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Single().GetMetadata("LogicalPath").Should().Be("assets/\u304C.png");
    }

    [Fact]
    public void DuplicateLogicalPath_ShouldReportPROMETE1003()
    {
        CreateFile("a/x.png");
        CreateFile("b/x.png");

        var (task, engine) = Run(
            Item("a/x.png", ("TargetPath", "assets/x.png")),
            Item("b/x.png", ("TargetPath", "./assets/x.png"))
        );

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1003");
        engine.Errors[0].Message.Should().Contain("assets/x.png");
    }

    [Fact]
    public void LogicalPathsDifferingOnlyInCase_ShouldBothBeResolved()
    {
        CreateFile("a/x.png");
        CreateFile("b/x.png");

        var (task, engine) = Run(
            Item("a/x.png", ("TargetPath", "assets/x.png")),
            Item("b/x.png", ("TargetPath", "assets/X.png"))
        );

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Should().HaveCount(2);
    }

    [Fact]
    public void Metadata_ShouldHaveDefaults()
    {
        CreateFile("assets/a.png");

        var (task, _) = Run(Item("assets/a.png"));

        var resolved = task.ResolvedAssets.Single();
        resolved.GetMetadata("Package").Should().Be("main");
        resolved.GetMetadata("Compress").Should().Be("auto");
        resolved.GetMetadata("Obfuscate").Should().Be("false");
    }

    [Theory]
    [InlineData("auto", "auto")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("True", "true")]
    [InlineData(" FALSE ", "false")]
    public void Compress_ShouldBeNormalizedToLowerCase(string value, string expected)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Compress", value)));

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Single().GetMetadata("Compress").Should().Be(expected);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("deflate")]
    [InlineData("1")]
    public void Compress_Invalid_ShouldReportPROMETE1004(string value)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Compress", value)));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1004");
    }

    [Theory]
    [InlineData("main")]
    [InlineData("Stage_1")]
    [InlineData("dlc-2")]
    public void Package_Valid_ShouldBeKept(string package)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Package", package)));

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Single().GetMetadata("Package").Should().Be(package);
    }

    [Theory]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a/b")]
    [InlineData("\u65E5\u672C")]
    public void Package_Invalid_ShouldReportPROMETE1005(string package)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Package", package)));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1005");
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("False", "false")]
    public void Obfuscate_ShouldBeNormalized(string value, string expected)
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Obfuscate", value)));

        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Single().GetMetadata("Obfuscate").Should().Be(expected);
    }

    [Fact]
    public void Obfuscate_Invalid_ShouldReportPROMETE1006()
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(Item("assets/a.png", ("Obfuscate", "maybe")));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1006");
    }

    [Fact]
    public void MissingFile_ShouldReportPROMETE1007()
    {
        var (task, engine) = Run(Item("assets/missing.png"));

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().Equal("PROMETE1007");
        engine.Errors[0].Message.Should().Contain("missing.png");
    }

    [Fact]
    public void MultipleErrors_ShouldAllBeReported()
    {
        CreateFile("assets/a.png");

        var (task, engine) = Run(
            Item("assets/missing.png"),
            Item("assets/a.png", ("Compress", "yes"))
        );

        task.Result.Should().BeFalse();
        engine.ErrorCodes.Should().BeEquivalentTo(["PROMETE1007", "PROMETE1004"]);
    }

    [Fact]
    public void Output_ShouldBeSortedByLogicalPathOrdinally()
    {
        CreateFile("assets/b.png");
        CreateFile("assets/a.png");
        CreateFile("assets/B.png");

        var (task, _) = Run(Item("assets/b.png"), Item("assets/a.png"), Item("assets/B.png"));

        task.ResolvedAssets.Select(i => i.GetMetadata("LogicalPath"))
            .Should()
            .Equal("assets/B.png", "assets/a.png", "assets/b.png");
    }

    [Fact]
    public void Empty_ShouldSucceedWithNoOutput()
    {
        var (task, engine) = Run();

        task.Result.Should().BeTrue();
        engine.Errors.Should().BeEmpty();
        task.ResolvedAssets.Should().BeEmpty();
    }

    [Fact]
    public void PackerVersion_ShouldStartWithFormatVersion()
    {
        var (task, _) = Run();

        task.PackerVersion.Should().StartWith("1.0+");
        task.PackerVersion.Length.Should().BeGreaterThan("1.0+".Length);
    }

    private (TaskResult Task, FakeBuildEngine Engine) Run(params ITaskItem[] items)
    {
        var engine = new FakeBuildEngine();
        var task = new ResolvePrometeAssets
        {
            BuildEngine = engine,
            Assets = items,
            ProjectDirectory = _project,
        };
        var result = task.Execute();
        return (new TaskResult(result, task.ResolvedAssets, task.PackerVersion), engine);
    }

    private string CreateFile(string relativePath)
    {
        var path = Path.Combine(_project, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
        return path;
    }

    private static TaskItem Item(string itemSpec, params (string Name, string Value)[] metadata)
    {
        var item = new TaskItem(itemSpec);
        foreach (var (name, value) in metadata)
            item.SetMetadata(name, value);
        return item;
    }

    private sealed record TaskResult(bool Result, ITaskItem[] ResolvedAssets, string PackerVersion);
}
