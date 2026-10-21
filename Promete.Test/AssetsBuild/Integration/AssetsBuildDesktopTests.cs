using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// デスクトップの実行プロジェクトで、<c>PrometeAsset</c> が Folder / Package モードで出力され、実行時に読めることのテスト。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildDesktopTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    [Fact]
    public void FolderMode_ShouldWriteManifestAndReadOnlyDeclaredFiles()
    {
        var project = CreateGame("<PrometeAssetMode>Folder</PrometeAssetMode>");

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        var output = _ws.OutputDirectory("Game");
        File.Exists(Path.Combine(output, "Game.promete-assets.json")).Should().BeTrue();
        Directory.GetFiles(output, "*.prmtpak").Should().BeEmpty();
        Directory
            .Exists(Path.Combine(output, "assets"))
            .Should()
            .BeFalse("Folder モードでもファイルはコピーしない");
        _ws.ReadAssemblyInfo("Game")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"Manifest\", \"Game.promete-assets.json\", Environment=\"Desktop\")"
            );

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Game.dll"),
            "entry",
            "assets/a.txt",
            "assets/sub/b.txt",
            "assets/skip.aseprite",
            "secret/c.txt",
            "assets/A.txt"
        );

        run.Lines("SOURCES").Should().Equal("SOURCES ManifestAssetSource");
        run.Lines("READ").Should().Equal("READ 1 alpha", "READ 2 bravo");
        run.Lines("ERROR")
            .Should()
            .Equal(
                "ERROR 3 FileNotFoundException",
                "ERROR 4 FileNotFoundException",
                "ERROR 5 AssetPathCaseMismatchException"
            );
    }

    [Fact]
    public void PackageMode_ShouldWritePackageNextToExecutable()
    {
        var project = CreateGame("<PrometeAssetMode>Package</PrometeAssetMode>");

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        var output = _ws.OutputDirectory("Game");
        File.Exists(Path.Combine(output, "Game.main.prmtpak")).Should().BeTrue();
        File.Exists(Path.Combine(output, "Game.promete-assets.json")).Should().BeFalse();
        Directory
            .Exists(Path.Combine(output, "assets"))
            .Should()
            .BeFalse("Release の出力にアセットのフォルダが残らないこと");
        _ws.ReadAssemblyInfo("Game")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"Package\", \"Game.main.prmtpak\", Environment=\"Desktop\", Package=\"main\")"
            );

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Game.dll"),
            "entry",
            "assets/a.txt",
            "assets/sub/b.txt",
            "assets/skip.aseprite"
        );

        run.Lines("SOURCES").Should().Equal("SOURCES PackageAssetSource");
        run.Lines("READ").Should().Equal("READ 1 alpha", "READ 2 bravo");
        run.Lines("ERROR").Should().Equal("ERROR 3 FileNotFoundException");
    }

    [Fact]
    public void PackageMode_MultiplePackages_ShouldWriteEachAndMountAll()
    {
        var project = CreateGame(
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """<PrometeAsset Update="assets/sub/**" Package="extra" />"""
        );

        _ws.Build(project).EnsureSucceeded();

        var output = _ws.OutputDirectory("Game");
        Directory
            .GetFiles(output, "*.prmtpak")
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo("Game.main.prmtpak", "Game.extra.prmtpak");
        _ws.ReadAssemblyInfo("Game")
            .Should()
            .Contain("Package=\"extra\"")
            .And.Contain("Package=\"main\"");

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Game.dll"),
            "entry",
            "assets/a.txt",
            "assets/sub/b.txt"
        );

        run.Lines("READ").Should().Equal("READ 1 alpha", "READ 2 bravo");
    }

    [Fact]
    public void Embed_ShouldEmbedPackageIntoAssembly()
    {
        var project = CreateGame(
            "<PrometeAssetMode>Package</PrometeAssetMode><PrometeAssetEmbed>true</PrometeAssetEmbed>"
        );

        _ws.Build(project).EnsureSucceeded();

        var output = _ws.OutputDirectory("Game");
        Directory.GetFiles(output, "*.prmtpak").Should().BeEmpty();
        _ws.ReadAssemblyInfo("Game")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"EmbeddedPackage\", \"Game.main.prmtpak\", Environment=\"Desktop\", Package=\"main\")"
            );

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Game.dll"),
            "entry",
            "assets/a.txt"
        );

        run.Lines("SOURCES").Should().Equal("SOURCES PackageAssetSource");
        run.Lines("READ").Should().Equal("READ 1 alpha");
    }

    [Fact]
    public void FolderMode_ManifestMissingAtRuntime_ShouldFailWithClearMessage()
    {
        var project = CreateGame("<PrometeAssetMode>Folder</PrometeAssetMode>");
        _ws.Build(project).EnsureSucceeded();
        var output = _ws.OutputDirectory("Game");
        File.Delete(Path.Combine(output, "Game.promete-assets.json"));

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Game.dll"),
            "entry",
            "assets/a.txt"
        );

        run.Lines("STARTUP")
            .Should()
            .ContainSingle()
            .Which.Should()
            .StartWith("STARTUP FileNotFoundException")
            .And.Contain("Game.promete-assets.json");
    }

    [Fact]
    public void WithoutPrometeAsset_ShouldProduceNothing()
    {
        var project = _ws.CreateDesktopProject("Plain");

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        var output = _ws.OutputDirectory("Plain");
        Directory.GetFiles(output, "*.prmtpak").Should().BeEmpty();
        Directory.GetFiles(output, "*.promete-assets.json").Should().BeEmpty();
        _ws.ReadAssemblyInfo("Plain").Should().NotContain("PrometeAssetSource");

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(output, "Plain.dll"),
            "entry",
            "assets/a.txt"
        );

        run.Lines("SOURCES").Should().Equal("SOURCES ");
        run.Lines("ERROR").Should().Equal("ERROR 1 FileNotFoundException");
    }

    [Fact]
    public void NfdFileName_ShouldBeReadableByNfcPath()
    {
        _ws.Write("Game/assets/が.txt", "ga");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Folder</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );
        _ws.Build(project).EnsureSucceeded();

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(_ws.OutputDirectory("Game"), "Game.dll"),
            "entry",
            "assets/が.txt"
        );

        run.Lines("READ").Should().Equal("READ 1 ga");
    }

    [Theory]
    [InlineData("Debug", "Folder")]
    [InlineData("Release", "Package")]
    [InlineData("Staging", "Package")]
    public void DefaultMode_ShouldDependOnConfiguration(string configuration, string expected)
    {
        var project = CreateGame(string.Empty);

        MSBuildWorkspace
            .GetProperty(project, "PrometeAssetMode", $"-p:Configuration={configuration}")
            .Should()
            .Be(expected);
    }

    [Fact]
    public void ExplicitMode_ShouldWinOverConfiguration()
    {
        var project = CreateGame("<PrometeAssetMode>Package</PrometeAssetMode>");

        MSBuildWorkspace
            .GetProperty(project, "PrometeAssetMode", "-p:Configuration=Debug")
            .Should()
            .Be("Package");
    }

    [Fact]
    public void UpToDateCheckInput_ShouldIncludeAssets()
    {
        var project = CreateGame("<PrometeAssetMode>Package</PrometeAssetMode>");

        var inputs = MSBuildWorkspace
            .GetItems(project, "UpToDateCheckInput")
            .Select(i => i["Identity"].Replace('\\', '/'))
            .ToArray();

        inputs.Should().Contain(["assets/a.txt", "assets/sub/b.txt"]);
        inputs.Should().Contain(i => i.EndsWith("Game.promete-assets.fingerprint"));
    }

    /// <summary>
    /// assets/a.txt (alpha)、assets/sub/b.txt (bravo)、assets/skip.aseprite (宣言から除外)、secret/c.txt (宣言しない) を持つ Game を作る。
    /// </summary>
    private string CreateGame(string properties, string extraItems = "")
    {
        _ws.Write("Game/assets/a.txt", "alpha");
        _ws.Write("Game/assets/sub/b.txt", "bravo");
        _ws.Write("Game/assets/skip.aseprite", "aseprite");
        _ws.Write("Game/secret/c.txt", "secret");
        return _ws.CreateDesktopProject(
            "Game",
            properties,
            $"""
            <PrometeAsset Include="assets/**" Exclude="assets/**/*.aseprite" />
            {extraItems}
            """
        );
    }
}
