using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// 正しくない設定がビルドエラーになることのテスト (検証の詳細は Task の単体テストで見る。ここでは targets からの配線を確かめる)。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildValidationTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    [Fact]
    public void IncludeOutsideProject_WithoutTargetPath_ShouldFailWithPROMETE1001()
    {
        _ws.Write("shared/x.txt", "x");
        var project = _ws.CreateDesktopProject(
            "Game",
            items: """<PrometeAsset Include="../shared/x.txt" />"""
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1001").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void IncludeOutsideProject_WithTargetPath_ShouldSucceed()
    {
        _ws.Write("shared/x.txt", "x");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Folder</PrometeAssetMode>",
            """<PrometeAsset Include="../shared/x.txt" TargetPath="assets/%(Filename)%(Extension)" />"""
        );

        _ws.Build(project).EnsureSucceeded();

        var run = MSBuildWorkspace.RunApp(
            Path.Combine(_ws.OutputDirectory("Game"), "Game.dll"),
            "entry",
            "assets/x.txt"
        );
        run.Lines("READ").Should().Equal("READ 1 x");
    }

    [Fact]
    public void DuplicateLogicalPath_ShouldFailWithPROMETE1003()
    {
        _ws.Write("Game/a/x.txt", "a");
        _ws.Write("Game/b/x.txt", "b");
        var project = _ws.CreateDesktopProject(
            "Game",
            items: """<PrometeAsset Include="a/x.txt;b/x.txt" TargetPath="assets/x.txt" />"""
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1003").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void InvalidMode_ShouldFailWithPROMETE1010()
    {
        _ws.Write("Game/assets/a.txt", "a");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Zip</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1010").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void InvalidEmbed_ShouldFailWithPROMETE1015()
    {
        // MSBuild は yes / no / on / off も真偽値として比べるので、それらは正しい値として扱われる
        _ws.Write("Game/assets/a.txt", "a");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Package</PrometeAssetMode><PrometeAssetEmbed>maybe</PrometeAssetEmbed>",
            """<PrometeAsset Include="assets/**" />"""
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1015").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void PackageNamesDifferingOnlyInCase_ShouldFailWithPROMETE1008()
    {
        // MSBuild の Distinct() は大文字・小文字を区別しないので、黙って同じパッケージに書かれ、片方のアセットが失われていた
        _ws.Write("Game/assets/a.txt", "a");
        _ws.Write("Game/assets/b.txt", "b");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """
            <PrometeAsset Include="assets/a.txt" />
            <PrometeAsset Include="assets/b.txt" Package="Main" />
            """
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1008").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void InvalidMetadata_ShouldFailWithTaskErrorCode()
    {
        _ws.Write("Game/assets/a.txt", "a");
        var project = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" Compress="sometimes" />"""
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1004").Should().BeTrue(build.ToString());
    }
}
