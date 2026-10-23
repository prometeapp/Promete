using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// <c>dotnet publish</c> で、Package モードのパッケージが載り、Folder モードのマニフェスト (絶対パスを含む) が載らないことのテスト。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildPublishTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    [Fact]
    public void FolderMode_Publish_ShouldWarnAndExcludeManifest()
    {
        var project = CreateGame("Folder");
        var publish = _ws.FullPath("publish");

        var result = _ws.Publish(project, publish).EnsureSucceeded();

        result.HasDiagnostic("PROMETE1011").Should().BeTrue(result.ToString());
        File.Exists(Path.Combine(publish, "Game.dll")).Should().BeTrue();
        File.Exists(Path.Combine(publish, "Game.promete-assets.json")).Should().BeFalse();
        Directory
            .GetFiles(publish, "*.json", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".deps.json") && !f.EndsWith(".runtimeconfig.json"))
            .Should()
            .AllSatisfy(f =>
                File.ReadAllText(f)
                    .Should()
                    .NotContain(
                        _ws.Root.Replace("\\", "\\\\"),
                        "開発機の絶対パスを publish に載せない"
                    )
            );
    }

    [Fact]
    public void PackageMode_Publish_ShouldIncludePackageAndRun()
    {
        var project = CreateGame("Package");
        var publish = _ws.FullPath("publish");

        var result = _ws.Publish(project, publish).EnsureSucceeded();

        result.HasAnyAssetsDiagnostic().Should().BeFalse(result.ToString());
        File.Exists(Path.Combine(publish, "Game.main.prmtpak")).Should().BeTrue();
        Directory.Exists(Path.Combine(publish, "assets")).Should().BeFalse();

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(publish, "Game.dll"),
            "entry",
            "assets/a.txt"
        );
        run.Lines("READ").Should().Equal("READ 1 alpha");
    }

    private string CreateGame(string mode)
    {
        _ws.Write("Game/assets/a.txt", "alpha");
        return _ws.CreateDesktopProject(
            "Game",
            $"<PrometeAssetMode>{mode}</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );
    }
}
