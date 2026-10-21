using System.IO.Compression;
using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// NuGet パッケージ経由 (<c>PackageReference</c>) でも <c>PrometeAsset</c> が動くことのテスト。
/// テストの構成でビルド済みの Promete を、テスト用のバージョンで一時的なフィードに pack して使う。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildNuGetTests : IClassFixture<AssetsBuildNuGetTests.PackedPromete>
{
    private readonly PackedPromete _packed;

    public AssetsBuildNuGetTests(PackedPromete packed)
    {
        _packed = packed;
    }

    [Fact]
    public void Pack_ShouldIncludeTasksAndTargetsWithoutWarnings()
    {
        _packed.PackResult.HasDiagnostic("NU5100").Should().BeFalse(_packed.PackResult.ToString());

        using var archive = ZipFile.OpenRead(_packed.PackagePath);
        var entries = archive.Entries.Select(e => e.FullName).ToArray();
        entries.Should().Contain("tasks/netstandard2.0/Promete.AssetPacker.Tasks.dll");
        entries.Should().Contain("tasks/netstandard2.0/Promete.AssetPacker.dll");
        entries.Should().Contain("build/Promete.targets");
        entries.Should().Contain("buildTransitive/Promete.targets");
        entries
            .Should()
            .NotContain(
                e => e.StartsWith("tasks/") && e.Contains("Microsoft.Build"),
                "MSBuild 自身のアセンブリは同梱しない"
            );
        entries
            .Should()
            .NotContain(
                e => e.StartsWith("lib/") && e.Contains("AssetPacker"),
                "Task はライブラリとして参照させない"
            );
    }

    [Theory]
    [InlineData("Folder", "Game.promete-assets.json")]
    [InlineData("Package", "Game.main.prmtpak")]
    public void PackageReference_ShouldBuildAndReadAssets(string mode, string expectedFile)
    {
        using var ws = new MSBuildWorkspace(MSBuildWorkspace.WorkspaceKind.Isolated);
        _packed.WriteNuGetConfig(ws);
        ws.Write("Game/Program.cs", MSBuildWorkspace.ProbeProgram);
        ws.Write("Game/assets/a.txt", "alpha");
        var project = ws.Write(
            "Game/Game.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <PrometeAssetMode>{mode}</PrometeAssetMode>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Promete" Version="{_packed.Version}" />
                <PrometeAsset Include="assets/**" />
              </ItemGroup>
            </Project>
            """
        );

        var build = MSBuildWorkspace
            .Dotnet(ws.Root, ["build", project, "-c", MSBuildWorkspace.Configuration])
            .EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        var output = ws.OutputDirectory("Game");
        File.Exists(Path.Combine(output, expectedFile)).Should().BeTrue();
        MSBuildWorkspace
            .RunApp(Path.Combine(output, "Game.dll"), "entry", "assets/a.txt", "assets/b.txt")
            .Lines("READ")
            .Should()
            .Equal("READ 1 alpha");
    }

    /// <summary>
    /// Promete をテスト用のバージョンで pack した一時的なフィード。テストクラスの中で 1 回だけ作る。
    /// </summary>
    public sealed class PackedPromete : IDisposable
    {
        private readonly MSBuildWorkspace _feed = new(MSBuildWorkspace.WorkspaceKind.Isolated);

        public PackedPromete()
        {
            Version = $"2.99.0-assetstest.{DateTime.UtcNow:yyyyMMddHHmmss}";
            var output = _feed.FullPath("feed");
            PackResult = MSBuildWorkspace.Dotnet(
                _feed.Root,
                [
                    "pack",
                    $"{MSBuildWorkspace.RepoRoot}/Promete/Promete.csproj",
                    "-c",
                    MSBuildWorkspace.Configuration,
                    "--no-build",
                    "-o",
                    output,
                    $"-p:Version={Version}",
                    $"-p:NuspecOutputPath={_feed.FullPath("nuspec")}/",
                ]
            );
            PackResult.EnsureSucceeded();
            PackagePath = Path.Combine(output, $"Promete.{Version}.nupkg");
        }

        /// <summary>テスト用のバージョン (毎回変わるので、グローバルパッケージフォルダの既存のものと衝突しない)。</summary>
        public string Version { get; }

        internal ProcessResult PackResult { get; }

        public string PackagePath { get; }

        /// <summary>
        /// フィードと、テスト用のグローバルパッケージフォルダを指定した NuGet.config を書く。
        /// 依存パッケージは、普段のグローバルパッケージフォルダをフォールバックとして使い、ダウンロードし直さない。
        /// </summary>
        internal void WriteNuGetConfig(MSBuildWorkspace ws) =>
            ws.Write(
                "NuGet.config",
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <config>
                    <add key="globalPackagesFolder" value="{ws.FullPath("packages")}" />
                  </config>
                  <packageSources>
                    <clear />
                    <add key="test-feed" value="{_feed.FullPath("feed")}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                  <fallbackPackageFolders>
                    <clear />
                    <add key="user-cache" value="{MSBuildWorkspace.GlobalPackagesFolder}" />
                  </fallbackPackageFolders>
                </configuration>
                """
            );

        public void Dispose() => _feed.Dispose();
    }
}
