using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;
using Promete.Assets.Infrastructure;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 属性からソースを作る既定の実装 (<see cref="DesktopAssetManifestLoader"/>) のテスト。
/// </summary>
public sealed class DesktopAssetManifestLoaderTests : IDisposable
{
    private static readonly System.Reflection.Assembly TestAssembly =
        typeof(DesktopAssetManifestLoaderTests).Assembly;

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void DefaultConstructor_ShouldUseAppContextBaseDirectory()
    {
        new DesktopAssetManifestLoader().BaseDirectory.Should().Be(AppContext.BaseDirectory);
    }

    [Fact]
    public void Manifest_ShouldCreateManifestSourceRelativeToBaseDirectory()
    {
        var file = _dir.CreateFile("src/a.txt", "hello");
        WriteManifest("Game.promete-assets.json", ("assets/a.txt", file));
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var source = loader.CreateSource(TestAssembly, Manifest("Game.promete-assets.json"));

        source.Should().BeOfType<ManifestAssetSource>();
        using var stream = source.OpenRead("assets/a.txt");
        new StreamReader(stream).ReadToEnd().Should().Be("hello");
        ((IFileBackedAssetSource)source)
            .TryGetFilePath("assets/a.txt", out var fullPath)
            .Should()
            .BeTrue();
        fullPath.Should().Be(file);
    }

    [Fact]
    public void Manifest_Missing_ShouldThrowFileNotFoundWithFileNameAndHint()
    {
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var act = () => loader.CreateSource(TestAssembly, Manifest("Game.promete-assets.json"));

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("Game.promete-assets.json"))
            .Where(e => e.Message.Contains(_dir.Path), "探した場所を含むはず")
            .Where(e => e.Message.Contains("Package"), "Package モードを案内するはず");
    }

    [Fact]
    public void Manifest_Invalid_ShouldThrowInvalidData()
    {
        _dir.CreateFile("Game.promete-assets.json", "{ broken");
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var act = () => loader.CreateSource(TestAssembly, Manifest("Game.promete-assets.json"));

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Package_ShouldOpenPackageRelativeToBaseDirectory()
    {
        WritePackage(
            Path.Combine(_dir.Path, "Game.main.prmtpak"),
            ("assets/a.txt", "from package")
        );
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        using var source = (IDisposable)
            loader.CreateSource(TestAssembly, Package("Game.main.prmtpak"));

        source.Should().BeOfType<PackageAssetSource>();
        using var stream = ((IAssetSource)source).OpenRead("assets/a.txt");
        new StreamReader(stream).ReadToEnd().Should().Be("from package");
    }

    [Fact]
    public void Package_Missing_ShouldThrowFileNotFoundWithFileName()
    {
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var act = () => loader.CreateSource(TestAssembly, Package("Game.main.prmtpak"));

        act.Should()
            .Throw<FileNotFoundException>()
            .Where(e => e.Message.Contains("Game.main.prmtpak"))
            .Where(e => e.Message.Contains(_dir.Path));
    }

    [Fact]
    public void EmbeddedPackage_ShouldOpenFromAssemblyResource()
    {
        var bytes = PackageBytes(("assets/a.txt", "embedded"));
        var assembly = FakeResourceAssembly.WithBinary(("Game.main.prmtpak", bytes));
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        using var source = (IDisposable)
            loader.CreateSource(
                assembly,
                new PrometeAssetSourceAttribute(
                    PrometeAssetSourceAttribute.KindEmbeddedPackage,
                    "Game.main.prmtpak"
                )
                {
                    Package = "main",
                }
            );

        using var stream = ((IAssetSource)source).OpenRead("assets/a.txt");
        new StreamReader(stream).ReadToEnd().Should().Be("embedded");
    }

    [Fact]
    public void EmbeddedPackage_MissingResource_ShouldThrowFileNotFound()
    {
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var act = () =>
            loader.CreateSource(
                new FakeResourceAssembly(),
                new PrometeAssetSourceAttribute(
                    PrometeAssetSourceAttribute.KindEmbeddedPackage,
                    "Game.main.prmtpak"
                )
            );

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void WebAttribute_ShouldThrowInvalidOperation()
    {
        WriteManifest("promete-assets.json");
        var loader = new DesktopAssetManifestLoader(_dir.Path);
        var attribute = Manifest("promete-assets.json");
        attribute.Environment = PrometeAssetSourceAttribute.EnvironmentWeb;

        var act = () => loader.CreateSource(TestAssembly, attribute);

        act.Should()
            .Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("Promete.Test"), "アセンブリ名を含むはず")
            .Where(e => e.Message.Contains("Web"));
    }

    [Fact]
    public void UnknownKind_ShouldThrowNotSupportedWithKind()
    {
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var act = () =>
            loader.CreateSource(TestAssembly, new PrometeAssetSourceAttribute("Bundle", "x"));

        act.Should().Throw<NotSupportedException>().Where(e => e.Message.Contains("Bundle"));
    }

    [Fact]
    public void NullArguments_ShouldThrowArgumentNull()
    {
        var loader = new DesktopAssetManifestLoader(_dir.Path);

        var nullAssembly = () => loader.CreateSource(null!, Manifest("x.json"));
        var nullAttribute = () => loader.CreateSource(TestAssembly, null!);
        var nullBase = () => new DesktopAssetManifestLoader(null!);

        nullAssembly.Should().Throw<ArgumentNullException>();
        nullAttribute.Should().Throw<ArgumentNullException>();
        nullBase.Should().Throw<ArgumentNullException>();
    }

    private static PrometeAssetSourceAttribute Manifest(string location) =>
        new(PrometeAssetSourceAttribute.KindManifest, location);

    private static PrometeAssetSourceAttribute Package(string location) =>
        new(PrometeAssetSourceAttribute.KindPackage, location) { Package = "main" };

    private static byte[] PackageBytes(params (string Path, string Content)[] files)
    {
        var writer = new AssetPackageWriter();
        foreach (var (path, content) in files)
            writer.Add(path, Encoding.UTF8.GetBytes(content));
        using var stream = new MemoryStream();
        writer.WriteTo(stream);
        return stream.ToArray();
    }

    private static void WritePackage(string path, params (string Path, string Content)[] files) =>
        File.WriteAllBytes(path, PackageBytes(files));

    private void WriteManifest(string name, params (string Logical, string Physical)[] files)
    {
        var json = new StringBuilder("{ \"version\": 1, \"files\": {");
        json.Append(
            string.Join(
                ",",
                files.Select(f =>
                    $"\"{f.Logical}\": \"{f.Physical.Replace("\\", "\\\\", StringComparison.Ordinal)}\""
                )
            )
        );
        json.Append("} }");
        _dir.CreateFile(name, json.ToString());
    }
}
