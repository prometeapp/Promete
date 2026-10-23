// Promete.Web は browser 専用のアセンブリだが、ここではマニフェストの解析 (ブラウザに依存しない部分) だけを使う
#pragma warning disable CA1416

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using FluentAssertions;
using Promete.Web.Assets;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// Web (<c>Microsoft.NET.Sdk.WebAssembly</c>) のプロジェクトでの <c>PrometeAsset</c> の扱いのテスト。
/// 契約に対応した Promete.Web (<c>PrometeWebAssetsContract</c> を立てる) と、古い Promete.Web (立てない) の両方を見る。
/// </summary>
/// <remarks>
/// ネイティブのリンクと AOT は止めてビルドする (<see cref="MSBuildWorkspace.CreateWebProject"/>)。
/// ブラウザでの実行は確かめない (手動の確認に残す)。
/// </remarks>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildWebTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    [Fact]
    public void FolderMode_ShouldWriteWebManifestAndWebAttribute()
    {
        var project = CreateWebGame(withPrometeWeb: true, string.Empty);

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        _ws.ReadAssemblyInfo("Web")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"Manifest\", \"promete-assets.json\", Environment=\"Web\")"
            );
        var entries = ReadWebManifest();
        entries
            .Should()
            .BeEquivalentTo([
                new WebAssetManifestEntry(
                    "file",
                    "/assets/a.txt",
                    "assets/a.txt",
                    null,
                    null,
                    null
                ),
                new WebAssetManifestEntry(
                    "font",
                    "/assets/fonts/ui.ttf",
                    "assets/fonts/ui.ttf",
                    null,
                    null,
                    null
                ),
                new WebAssetManifestEntry(
                    "file",
                    "/data/shared.txt",
                    "data/shared.txt",
                    null,
                    null,
                    null
                ),
            ]);
        Directory
            .GetFiles(_ws.FullPath("Web"), "*.promete-assets.json", SearchOption.AllDirectories)
            .Should()
            .BeEmpty("Web ではデスクトップ形式のマニフェストを書かない");
        Directory
            .GetFiles(_ws.FullPath("Web"), "*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void FolderMode_ShouldBeTheDefaultEvenInRelease()
    {
        var project = CreateWebGame(withPrometeWeb: true, string.Empty);

        MSBuildWorkspace
            .GetProperty(project, "PrometeAssetMode", "-p:Configuration=Release")
            .Should()
            .Be("Folder");
    }

    [Fact]
    public void PackageMode_ShouldPackAndListOnlyThePackage()
    {
        var project = CreateWebGame(
            withPrometeWeb: true,
            "<PrometeAssetMode>Package</PrometeAssetMode>"
        );

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        _ws.ReadAssemblyInfo("Web")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"Package\", \"Web.main.prmtpak\", Environment=\"Web\", Package=\"main\")"
            );
        ReadWebManifest()
            .Should()
            .Equal(
                new WebAssetManifestEntry("package", "/Web.main.prmtpak", null, "main", null, null)
            );
        File.Exists(Path.Combine(_ws.IntermediateDirectory("Web"), "Web.main.prmtpak"))
            .Should()
            .BeTrue();
        StaticWebAssetsManifest().Should().Contain("Web.main.prmtpak").And.NotContain("ui.ttf");
    }

    [Fact]
    public void PackageMode_Embed_ShouldListEmbeddedPackageForFonts()
    {
        var project = CreateWebGame(
            withPrometeWeb: true,
            "<PrometeAssetMode>Package</PrometeAssetMode><PrometeAssetEmbed>true</PrometeAssetEmbed>"
        );

        _ws.Build(project).EnsureSucceeded();

        _ws.ReadAssemblyInfo("Web")
            .Should()
            .Contain(
                "PrometeAssetSourceAttribute(\"EmbeddedPackage\", \"Web.main.prmtpak\", Environment=\"Web\", Package=\"main\")"
            );
        ReadWebManifest()
            .Should()
            .Equal(
                new WebAssetManifestEntry(
                    "embedded-package",
                    null,
                    null,
                    "main",
                    "Web",
                    "Web.main.prmtpak"
                )
            );
        EmbeddedResourceNames(Path.Combine(_ws.IntermediateDirectory("Web"), "Web.dll"))
            .Should()
            .Contain(
                "Web.main.prmtpak",
                "属性とマニフェストが指すパッケージが、実際にアセンブリに埋め込まれているはず"
            );
        Directory
            .GetFiles(_ws.OutputDirectory("Web"), "*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty("埋め込んだパッケージは出力先に置かない");
    }

    [Fact]
    public void WithoutPrometeAsset_ShouldBeQuietAndWriteEmptyManifest()
    {
        var project = _ws.CreateWebProject("Web", withPrometeWeb: true);

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        _ws.ReadAssemblyInfo("Web").Should().NotContain("PrometeAssetSource");
        ReadWebManifest().Should().BeEmpty();
    }

    [Fact]
    public void OldPrometeWeb_WithPrometeAsset_ShouldWarnAndLeaveItemsAlone()
    {
        var project = CreateWebGame(withPrometeWeb: false, string.Empty);

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasDiagnostic("PROMETE1013").Should().BeTrue(build.ToString());
        build.Output.Should().Contain("2.4");
        _ws.ReadAssemblyInfo("Web").Should().NotContain("PrometeAssetSource");
        Directory
            .GetFiles(_ws.FullPath("Web"), "*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
        Directory
            .GetFiles(_ws.FullPath("Web"), "*.promete-assets.json", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
        MSBuildWorkspace
            .GetItems(project, "PrometeAsset")
            .Should()
            .AllSatisfy(i =>
                i.GetValueOrDefault("Package", string.Empty)
                    .Should()
                    .BeEmpty("古い Promete.Web の挙動のまま、メタデータを足さない")
            );
    }

    [Fact]
    public void OldPrometeWeb_ExplicitPackageMode_ShouldFail()
    {
        var project = CreateWebGame(
            withPrometeWeb: false,
            "<PrometeAssetMode>Package</PrometeAssetMode>"
        );

        var build = _ws.Build(project);

        build.Succeeded.Should().BeFalse();
        build.HasDiagnostic("PROMETE1014").Should().BeTrue(build.ToString());
    }

    [Fact]
    public void OldPrometeWeb_WithoutPrometeAsset_ShouldBeQuiet()
    {
        var project = _ws.CreateWebProject("Web", withPrometeWeb: false);

        var build = _ws.Build(project).EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
    }

    [Fact]
    public void DesktopExecutable_BuiltWithBrowserRuntimeIdentifier_ShouldStayDesktop()
    {
        // dotnet build -r browser-wasm で Web のプロジェクトをビルドすると、参照先のデスクトップの実行プロジェクトにも RID が伝わる。
        // そのときも、古い Promete.Web と誤認して止めたりせず、デスクトップとして扱うこと
        _ws.Write("Game/assets/a.txt", "a");
        var game = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Folder</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );

        var build = _ws.Build(game, "-p:RuntimeIdentifier=browser-wasm").EnsureSucceeded();

        build.HasAnyAssetsDiagnostic().Should().BeFalse(build.ToString());
        _ws.ReadAssemblyInfo("Game", "browser-wasm").Should().Contain("Environment=\"Desktop\"");
    }

    [Theory]
    [InlineData("Folder")]
    [InlineData("Package")]
    public void WebReferencingDesktopExecutable_ShouldNotCopyItsAssetOutputs(string mode)
    {
        _ws.Write("Game/assets/game.txt", "game");
        var game = _ws.CreateDesktopProject(
            "Game",
            $"<PrometeAssetMode>{mode}</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );
        _ws.Write("Web/assets/web.txt", "web");
        var web = _ws.CreateWebProject(
            "Web",
            withPrometeWeb: true,
            items: """
            <ProjectReference Include="../Game/Game.csproj" />
            <PrometeAsset Include="assets/**" />
            """
        );
        _ws.Build(game).EnsureSucceeded();

        _ws.Build(web).EnsureSucceeded();

        var output = _ws.OutputDirectory("Web");
        File.Exists(Path.Combine(output, "Game.dll")).Should().BeTrue("参照先の出力は届いている");
        Directory
            .GetFiles(output, "Game.*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
        Directory
            .GetFiles(output, "Game.promete-assets.json", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
        ReadWebManifest().Select(e => e.LogicalPath).Should().Equal("assets/web.txt");
    }

    [Fact]
    public void WebReferencingDesktopExecutable_ShouldNotPublishItsPackage()
    {
        // 参照先 (デスクトップの実行プロジェクト) が Package モードだと、そのパッケージは CopyToPublishDirectory 付きで伝播してくる。
        // Web の publish の出力に、デスクトップ向けのパッケージやマニフェストを残さないこと (wwwroot の外も含む)
        _ws.Write("Game/assets/game.txt", "game");
        var game = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );
        _ws.Write("Web/assets/web.txt", "web");
        var web = _ws.CreateWebProject(
            "Web",
            withPrometeWeb: true,
            items: """
            <ProjectReference Include="../Game/Game.csproj" />
            <PrometeAsset Include="assets/**" />
            """
        );
        _ws.Build(game).EnsureSucceeded();
        File.Exists(Path.Combine(_ws.OutputDirectory("Game"), "Game.main.prmtpak"))
            .Should()
            .BeTrue("前提: 参照先は Package モードでパッケージを出力している");
        var publish = _ws.FullPath("publish");

        _ws.Publish(web, publish).EnsureSucceeded();

        File.Exists(Path.Combine(publish, "wwwroot", "promete-assets.json"))
            .Should()
            .BeTrue("前提: Web 自身の publish はできている");
        Directory
            .GetFiles(publish, "Game.*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
        Directory
            .GetFiles(publish, "Game.promete-assets.json", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void OwnPackageFileInProject_ShouldStayInOutputAndPublish()
    {
        // 参照先から伝播したものだけを外し、利用者自身がプロジェクトのフォルダに置いた .prmtpak は残す。
        // 「プロジェクトのフォルダの中か」の判定は、パスの区切りに依存してはいけない (Linux / macOS では / になる)
        _ws.Write("Game/assets/game.txt", "game");
        var game = _ws.CreateDesktopProject(
            "Game",
            "<PrometeAssetMode>Package</PrometeAssetMode>",
            """<PrometeAsset Include="assets/**" />"""
        );
        _ws.Write("Web/extra/mine.prmtpak", "not a real package");
        var web = _ws.CreateWebProject(
            "Web",
            withPrometeWeb: true,
            items: """
            <ProjectReference Include="../Game/Game.csproj" />
            <None Include="extra/mine.prmtpak" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
            """
        );
        _ws.Build(game).EnsureSucceeded();
        var publish = _ws.FullPath("publish");

        _ws.Build(web).EnsureSucceeded();
        _ws.Publish(web, publish).EnsureSucceeded();

        File.Exists(Path.Combine(_ws.OutputDirectory("Web"), "extra", "mine.prmtpak"))
            .Should()
            .BeTrue("利用者自身のファイルは出力に残るはず");
        File.Exists(Path.Combine(publish, "extra", "mine.prmtpak"))
            .Should()
            .BeTrue("利用者自身のファイルは publish にも残るはず");
        Directory
            .GetFiles(publish, "Game.*.prmtpak", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
    }

    /// <summary>
    /// assets/a.txt、assets/fonts/ui.ttf、プロジェクトの外の shared.txt (TargetPath を明示) を持つ Web のプロジェクトを作る。
    /// </summary>
    private string CreateWebGame(bool withPrometeWeb, string properties)
    {
        _ws.Write("Web/assets/a.txt", "a");
        _ws.Write("Web/assets/fonts/ui.ttf", "ttf");
        _ws.Write("shared/shared.txt", "shared");
        return _ws.CreateWebProject(
            "Web",
            withPrometeWeb,
            properties,
            """
            <PrometeAsset Include="assets/**" />
            <PrometeAsset Include="../shared/shared.txt" TargetPath="data/shared.txt" />
            """
        );
    }

    /// <summary>
    /// アセンブリのマニフェストリソースの名前を読む (アセンブリを読み込まず、メタデータだけを見る)。
    /// </summary>
    private static string[] EmbeddedResourceNames(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        return metadata
            .ManifestResources.Select(h => metadata.GetString(metadata.GetManifestResource(h).Name))
            .ToArray();
    }

    private IReadOnlyList<WebAssetManifestEntry> ReadWebManifest() =>
        WebAssetManifest.Parse(
            File.ReadAllBytes(
                Path.Combine(_ws.IntermediateDirectory("Web"), "promete", "promete-assets.json")
            ),
            "'promete-assets.json'"
        );

    private string StaticWebAssetsManifest() =>
        File.ReadAllText(
            Path.Combine(_ws.IntermediateDirectory("Web"), "staticwebassets.build.json")
        );
}
