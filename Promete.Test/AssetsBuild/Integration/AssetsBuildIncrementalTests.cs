using FluentAssertions;
using Promete.Assets;
using Promete.Assets.Infrastructure;
using Promete.Test.AssetsApi;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// インクリメンタルビルドで、日付だけでは分からない変更 (item の削除、メタデータ) でもパッケージやマニフェストが作り直されることのテスト。
/// </summary>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class AssetsBuildIncrementalTests : IDisposable
{
    private readonly MSBuildWorkspace _ws = new();

    public void Dispose() => _ws.Dispose();

    private string Package => Path.Combine(_ws.IntermediateDirectory("Game"), "Game.main.prmtpak");

    private string OutputPackage => Path.Combine(_ws.OutputDirectory("Game"), "Game.main.prmtpak");

    [Fact]
    public void NoChange_ShouldNotRewritePackage()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""");
        _ws.Build(project).EnsureSucceeded();
        File.Exists(Package).Should().BeTrue();
        var first = File.GetLastWriteTimeUtc(Package);

        _ws.Build(project).EnsureSucceeded();

        File.GetLastWriteTimeUtc(Package).Should().Be(first);
    }

    [Fact]
    public void RemovingItem_ShouldRebuildPackageWithoutIt()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""");
        _ws.Build(project).EnsureSucceeded();

        Rewrite("""<PrometeAsset Include="assets/**" Exclude="assets/b.txt" />""");
        _ws.Build(project).EnsureSucceeded();

        EntriesOf(OutputPackage).Should().Equal("assets/a.txt");
    }

    [Fact]
    public void ChangingCompress_ShouldRebuildPackage()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" Compress="false" />""");
        _ws.Build(project).EnsureSucceeded();
        FlagsOf(OutputPackage, "assets/a.txt").Should().Be(0u);

        Rewrite("""<PrometeAsset Include="assets/**" Compress="true" />""");
        _ws.Build(project).EnsureSucceeded();

        FlagsOf(OutputPackage, "assets/a.txt").Should().Be(RawPackageLayout.EntryFlagDeflate);
    }

    [Fact]
    public void ChangingTargetPath_ShouldRebuildPackage()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""");
        _ws.Build(project).EnsureSucceeded();

        Rewrite(
            """<PrometeAsset Include="assets/**" TargetPath="data/%(Filename)%(Extension)" />"""
        );
        _ws.Build(project).EnsureSucceeded();

        EntriesOf(OutputPackage).Should().BeEquivalentTo("data/a.txt", "data/b.txt");
    }

    [Fact]
    public void ChangingObfuscate_ShouldRebuildPackage()
    {
        // Phase 4 では中身は変わらないが、指紋が変わるので書き直される (Phase 5 で暗号化に使う)
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""");
        _ws.Build(project).EnsureSucceeded();
        var first = File.GetLastWriteTimeUtc(Package);

        Rewrite("""<PrometeAsset Include="assets/**" Obfuscate="true" />""");
        _ws.Build(project).EnsureSucceeded();

        File.GetLastWriteTimeUtc(Package).Should().BeAfter(first);
    }

    [Fact]
    public void ChangingAssetContent_ShouldRebuildPackage()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""");
        _ws.Build(project).EnsureSucceeded();

        var file = _ws.Write("Game/assets/a.txt", "changed");
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds(5));
        _ws.Build(project).EnsureSucceeded();

        using var source = new PackageAssetSource(OutputPackage);
        new StreamReader(source.OpenRead("assets/a.txt")).ReadToEnd().Should().Be("changed");
    }

    [Fact]
    public void FingerprintFile_ShouldRecordPackerVersionAndItems()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" Compress="false" />""");
        _ws.Build(project).EnsureSucceeded();

        var lines = File.ReadAllLines(
            Path.Combine(_ws.IntermediateDirectory("Game"), "Game.promete-assets.fingerprint")
        );

        lines.Should().Contain(l => l.StartsWith("packer=1.0+"));
        lines.Should().Contain("mode=Package");
        lines
            .Should()
            .Contain(l => l.StartsWith("assets/a.txt|") && l.EndsWith("|main|false|false"));
    }

    [Fact]
    public void FolderMode_AddingMatchingFile_ShouldUpdateManifest()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""", "Folder");
        _ws.Build(project).EnsureSucceeded();
        var manifest = Path.Combine(_ws.OutputDirectory("Game"), "Game.promete-assets.json");
        DesktopAssetManifest
            .Read(manifest)
            .Keys.Should()
            .BeEquivalentTo("assets/a.txt", "assets/b.txt");

        _ws.Write("Game/assets/new.txt", "new");
        _ws.Build(project).EnsureSucceeded();

        DesktopAssetManifest
            .Read(manifest)
            .Keys.Should()
            .BeEquivalentTo("assets/a.txt", "assets/b.txt", "assets/new.txt");
    }

    [Fact]
    public void FolderMode_NoChange_ShouldNotRewriteManifest()
    {
        var project = CreateGame("""<PrometeAsset Include="assets/**" />""", "Folder");
        _ws.Build(project).EnsureSucceeded();
        var manifest = Path.Combine(_ws.IntermediateDirectory("Game"), "Game.promete-assets.json");
        File.Exists(manifest).Should().BeTrue();
        var first = File.GetLastWriteTimeUtc(manifest);

        _ws.Build(project).EnsureSucceeded();

        File.GetLastWriteTimeUtc(manifest).Should().Be(first);
    }

    private static string[] EntriesOf(string package)
    {
        using var source = new PackageAssetSource(package);
        return source.Enumerate("", "*", true).ToArray();
    }

    private static uint FlagsOf(string package, string path) =>
        RawPackage.Parse(File.ReadAllBytes(package)).Entries.Single(e => e.Path == path).Flags;

    private string CreateGame(string items, string mode = "Package")
    {
        _ws.Write("Game/assets/a.txt", new string('a', 4000));
        _ws.Write("Game/assets/b.txt", "bravo");
        return _ws.CreateDesktopProject(
            "Game",
            $"<PrometeAssetMode>{mode}</PrometeAssetMode>",
            items
        );
    }

    /// <summary>
    /// Game の csproj の PrometeAsset だけを書き換える (他の設定は同じ)。
    /// </summary>
    private void Rewrite(string items) =>
        _ws.CreateDesktopProject("Game", "<PrometeAssetMode>Package</PrometeAssetMode>", items);
}
