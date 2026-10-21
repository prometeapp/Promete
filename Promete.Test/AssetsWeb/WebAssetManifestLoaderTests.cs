// Promete.Web は browser 専用のアセンブリだが、ここではブラウザに依存しない部分だけをデスクトップで試す
#pragma warning disable CA1416

using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;
using Promete.Assets.Infrastructure;
using Promete.Test.AssetsApi;
using Promete.Web.Assets;

namespace Promete.Test.AssetsWeb;

/// <summary>
/// Web 向けの属性を読む <see cref="WebAssetManifestLoader"/> のテスト。ブラウザの仮想ファイルシステムのルートの代わりに一時ディレクトリを使う。
/// </summary>
public sealed class WebAssetManifestLoaderTests : IDisposable
{
    private static readonly System.Reflection.Assembly TestAssembly =
        typeof(WebAssetManifestLoaderTests).Assembly;

    private readonly TempDirectory _root = new();

    public void Dispose() => _root.Dispose();

    [Fact]
    public void DefaultConstructor_ShouldUseFileSystemRoot()
    {
        new WebAssetManifestLoader().RootDirectory.Should().Be("/");
    }

    [Fact]
    public void DesktopAttribute_ShouldThrowWithGuidance()
    {
        var loader = new WebAssetManifestLoader(_root.Path);
        var attribute = new PrometeAssetSourceAttribute(
            PrometeAssetSourceAttribute.KindManifest,
            "Game.promete-assets.json"
        );

        var act = () => loader.CreateSource(TestAssembly, attribute);

        act.Should()
            .Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("'Promete.Test'"), "アセンブリ名を含むはず")
            .Where(
                e => e.Message.Contains("PrometeAsset"),
                "Web の実行プロジェクトでの宣言を案内するはず"
            );
    }

    [Fact]
    public void Manifest_ShouldMountFilesAndFontsOnly()
    {
        _root.CreateFile("assets/x.png", "png");
        _root.CreateFile("assets/a.ttf", "ttf");
        _root.CreateFile("assets/undeclared.txt", "secret");
        _root.CreateFile(
            "promete-assets.json",
            """
            {
              "version": 1,
              "entries": [
                { "kind": "file", "path": "/assets/x.png", "url": "assets/x.png", "logicalPath": "assets/x.png" },
                { "kind": "font", "path": "/assets/a.ttf", "url": "assets/a.ttf", "logicalPath": "assets/a.ttf" },
                { "kind": "package", "path": "/Game.main.prmtpak", "url": "Game.main.prmtpak", "package": "main" }
              ]
            }
            """
        );
        var loader = new WebAssetManifestLoader(_root.Path);

        var source = loader.CreateSource(
            TestAssembly,
            Web(PrometeAssetSourceAttribute.KindManifest, "promete-assets.json")
        );

        source.Should().BeOfType<ManifestAssetSource>();
        new StreamReader(source.OpenRead("assets/x.png")).ReadToEnd().Should().Be("png");
        source.Enumerate("", "*", true).Should().BeEquivalentTo("assets/x.png", "assets/a.ttf");
        ((IFileBackedAssetSource)source)
            .TryGetFilePath("assets/a.ttf", out var fontPath)
            .Should()
            .BeTrue();
        fontPath.Should().Be(Path.GetFullPath(Path.Combine(_root.Path, "assets", "a.ttf")));
        var undeclared = () => source.OpenRead("assets/undeclared.txt");
        undeclared.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Manifest_Missing_ShouldThrowFileNotFound()
    {
        var loader = new WebAssetManifestLoader(_root.Path);

        var act = () =>
            loader.CreateSource(
                TestAssembly,
                Web(PrometeAssetSourceAttribute.KindManifest, "promete-assets.json")
            );

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("promete-assets.json"));
    }

    [Fact]
    public void Package_ShouldOpenPackageAndResolveExtractedFonts()
    {
        var packagePath = Path.Combine(_root.Path, "Game.main.prmtpak");
        WritePackage(packagePath, ("assets/ui.ttf", "ttf"), ("assets/x.png", "png"));
        using (var package = new PackageAssetSource(packagePath))
        {
            WebPackageFonts.Extract(
                package,
                WebPackageFonts.GetFontDirectory(_root.Path, "Game.main.prmtpak")
            );
        }

        var loader = new WebAssetManifestLoader(_root.Path);

        using var source = (IDisposable)
            loader.CreateSource(
                TestAssembly,
                Web(PrometeAssetSourceAttribute.KindPackage, "Game.main.prmtpak")
            );

        var fileBacked = (IFileBackedAssetSource)source;
        new StreamReader(fileBacked.OpenRead("assets/x.png")).ReadToEnd().Should().Be("png");
        fileBacked.TryGetFilePath("assets/ui.ttf", out var fontPath).Should().BeTrue();
        File.ReadAllText(fontPath!).Should().Be("ttf");
        fileBacked.TryGetFilePath("assets/x.png", out _).Should().BeFalse();
    }

    [Fact]
    public void Package_Missing_ShouldThrowFileNotFound()
    {
        var loader = new WebAssetManifestLoader(_root.Path);

        var act = () =>
            loader.CreateSource(
                TestAssembly,
                Web(PrometeAssetSourceAttribute.KindPackage, "Game.main.prmtpak")
            );

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("Game.main.prmtpak"));
    }

    [Fact]
    public void EmbeddedPackage_ShouldOpenFromResource()
    {
        var assembly = FakeResourceAssembly.WithBinary(
            ("Game.main.prmtpak", PackageBytes(("assets/a.txt", "embedded")))
        );
        var loader = new WebAssetManifestLoader(_root.Path);

        using var source = (IDisposable)
            loader.CreateSource(
                assembly,
                Web(PrometeAssetSourceAttribute.KindEmbeddedPackage, "Game.main.prmtpak")
            );

        source.Should().BeAssignableTo<IFileBackedAssetSource>();
        new StreamReader(((IAssetSource)source).OpenRead("assets/a.txt"))
            .ReadToEnd()
            .Should()
            .Be("embedded");
    }

    [Fact]
    public void UnknownKind_ShouldThrowNotSupported()
    {
        var loader = new WebAssetManifestLoader(_root.Path);

        var act = () => loader.CreateSource(TestAssembly, Web("Bundle", "x"));

        act.Should().Throw<NotSupportedException>().Where(e => e.Message.Contains("Bundle"));
    }

    [Fact]
    public void NullArguments_ShouldThrow()
    {
        var loader = new WebAssetManifestLoader(_root.Path);

        var nullAssembly = () =>
            loader.CreateSource(null!, Web(PrometeAssetSourceAttribute.KindManifest, "x"));
        var nullAttribute = () => loader.CreateSource(TestAssembly, null!);
        var nullRoot = () => new WebAssetManifestLoader(null!);

        nullAssembly.Should().Throw<ArgumentNullException>();
        nullAttribute.Should().Throw<ArgumentNullException>();
        nullRoot.Should().Throw<ArgumentNullException>();
    }

    private static PrometeAssetSourceAttribute Web(string kind, string location) =>
        new(kind, location) { Environment = PrometeAssetSourceAttribute.EnvironmentWeb };

    private static byte[] PackageBytes(params (string Path, string Content)[] files)
    {
        var writer = new AssetPackageWriter();
        foreach (var (logical, content) in files)
            writer.Add(logical, Encoding.UTF8.GetBytes(content));
        using var stream = new MemoryStream();
        writer.WriteTo(stream);
        return stream.ToArray();
    }

    private static void WritePackage(string path, params (string Path, string Content)[] files) =>
        File.WriteAllBytes(path, PackageBytes(files));
}
