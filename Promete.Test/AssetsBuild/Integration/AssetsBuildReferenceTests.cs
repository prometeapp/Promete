using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// 実行プロジェクトを別の実行プロジェクトから参照したとき (テスト → ゲーム、Web Example → Promete.Example) の扱いと、
/// ライブラリ・テストプロジェクトの <c>PrometeAsset</c> の扱いのテスト。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildReferenceTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    [Theory]
    [InlineData("Folder", "Game.promete-assets.json", "Host.promete-assets.json")]
    [InlineData("Package", "Game.main.prmtpak", "Host.main.prmtpak")]
    public void ReferencedExecutable_ShouldPropagateWithoutNameCollision(
        string mode,
        string gameFile,
        string hostFile
    )
    {
        var game = CreateExe("Game", mode, "game");
        var host = CreateExe(
            "Host",
            mode,
            "host",
            """<ProjectReference Include="../Game/Game.csproj" />"""
        );
        _ws.Build(game).EnsureSucceeded();

        _ws.Build(host).EnsureSucceeded();

        var output = _ws.OutputDirectory("Host");
        File.Exists(Path.Combine(output, gameFile)).Should().BeTrue();
        File.Exists(Path.Combine(output, hostFile)).Should().BeTrue();
        var dll = Path.Combine(output, "Host.dll");
        MSBuildWorkspace
            .RunApp(dll, "entry", "assets/host.txt", "assets/game.txt")
            .Lines("READ")
            .Should()
            .Equal("READ 1 host");
        MSBuildWorkspace
            .RunApp(dll, "Game", "assets/game.txt", "assets/host.txt")
            .Lines("READ")
            .Should()
            .Equal("READ 1 game");
    }

    [Fact]
    public void ReferencedEmbeddedExecutable_ShouldBeReadableThroughItsAssembly()
    {
        var game = CreateExe(
            "Game",
            "Package",
            "game",
            properties: "<PrometeAssetEmbed>true</PrometeAssetEmbed>"
        );
        var host = _ws.CreateDesktopProject(
            "Host",
            items: """<ProjectReference Include="../Game/Game.csproj" />"""
        );
        _ws.Build(game).EnsureSucceeded();

        _ws.Build(host).EnsureSucceeded();

        var output = _ws.OutputDirectory("Host");
        Directory
            .GetFiles(output, "Game.*.prmtpak")
            .Should()
            .BeEmpty("埋め込んだパッケージは伝播させない");
        MSBuildWorkspace
            .RunApp(Path.Combine(output, "Host.dll"), "Game", "assets/game.txt")
            .Lines("READ")
            .Should()
            .Equal("READ 1 game");
    }

    [Fact]
    public void Library_ShouldWarnAndIgnorePrometeAsset()
    {
        _ws.Write("Lib/assets/a.txt", "a");
        var lib = _ws.CreateDesktopProject(
            "Lib",
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />""",
            outputType: "Library"
        );

        var build = _ws.Build(lib).EnsureSucceeded();

        build.HasDiagnostic("PROMETE1012").Should().BeTrue(build.ToString());
        _ws.ReadAssemblyInfo("Lib").Should().NotContain("PrometeAssetSource");
        Directory.GetFiles(_ws.OutputDirectory("Lib"), "*.prmtpak").Should().BeEmpty();
        Directory.GetFiles(_ws.IntermediateDirectory("Lib"), "*.prmtpak").Should().BeEmpty();
    }

    [Fact]
    public void TestProject_ShouldBeTreatedLikeLibrary()
    {
        // Microsoft.NET.Test.Sdk は OutputType を Exe にし、IsTestProject を立てる。ここでは同じ状態を直接作る
        _ws.Write("Tests/assets/a.txt", "a");
        var tests = _ws.CreateDesktopProject(
            "Tests",
            "<IsTestProject>true</IsTestProject><PrometeAssetMode>Folder</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );

        var build = _ws.Build(tests).EnsureSucceeded();

        build.HasDiagnostic("PROMETE1012").Should().BeTrue(build.ToString());
        _ws.ReadAssemblyInfo("Tests").Should().NotContain("PrometeAssetSource");
        Directory
            .GetFiles(_ws.OutputDirectory("Tests"), "*.promete-assets.json")
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void TestProject_ReferencingGame_ShouldReadGameAssets()
    {
        var game = CreateExe("Game", "Package", "game");
        var tests = _ws.CreateDesktopProject(
            "Tests",
            "<IsTestProject>true</IsTestProject>",
            """<ProjectReference Include="../Game/Game.csproj" />"""
        );
        _ws.Build(game).EnsureSucceeded();

        _ws.Build(tests).EnsureSucceeded();

        MSBuildWorkspace
            .RunApp(
                Path.Combine(_ws.OutputDirectory("Tests"), "Tests.dll"),
                "Game",
                "assets/game.txt"
            )
            .Lines("READ")
            .Should()
            .Equal("READ 1 game");
    }

    private string CreateExe(
        string name,
        string mode,
        string content,
        string extraItems = "",
        string properties = ""
    )
    {
        _ws.Write($"{name}/assets/{content}.txt", content);
        return _ws.CreateDesktopProject(
            name,
            $"<PrometeAssetMode>{mode}</PrometeAssetMode>{properties}",
            $"""
            <PrometeAsset Include="assets/**" />
            {extraItems}
            """
        );
    }
}
