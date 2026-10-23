// Promete.Web は browser 専用のアセンブリだが、ここではブラウザに依存しない部分だけをデスクトップで試す
#pragma warning disable CA1416

using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;
using Promete.Test.AssetsApi;
using Promete.Web.Assets;

namespace Promete.Test.AssetsWeb;

/// <summary>
/// パッケージの中のフォントを取り出す処理 (<see cref="WebPackageFonts"/>) と、
/// それを使うソース (<see cref="WebPackageAssetSource"/>) のテスト。ブラウザの仮想ファイルシステムの代わりに一時ディレクトリを使う。
/// </summary>
public sealed class WebPackageFontsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Theory]
    [InlineData("a.ttf", true)]
    [InlineData("dir/A.TTF", true)]
    [InlineData("a.otf", true)]
    [InlineData("a.woff", true)]
    [InlineData("a.woff2", true)]
    [InlineData("a.png", false)]
    [InlineData("ttf", false)]
    [InlineData("a.ttf.png", false)]
    [InlineData("fonts.ttf/readme.txt", false)]
    public void IsFont_ShouldJudgeByExtension(string path, bool expected)
    {
        WebPackageFonts.IsFont(path).Should().Be(expected);
    }

    [Fact]
    public void GetFontDirectory_ShouldBeUnderDotPrometeFonts()
    {
        WebPackageFonts
            .GetFontDirectory("/", "Game.main.prmtpak")
            .Should()
            .Be(Path.Combine("/", ".promete", "fonts", "Game.main.prmtpak"));
        WebPackageFonts
            .GetFontDirectory(_dir.Path, "Game.main.prmtpak")
            .Should()
            .Be(Path.Combine(_dir.Path, ".promete", "fonts", "Game.main.prmtpak"));
    }

    [Fact]
    public void Extract_ShouldWriteOnlyFontsAndReturnSortedFullPaths()
    {
        var package = new InMemoryAssetSource(
            ("fonts/b.ttf", "font-b"),
            ("a.otf", "font-a"),
            ("image.png", "png"),
            ("fonts/sub/c.woff2", "font-c")
        );
        var directory = WebPackageFonts.GetFontDirectory(_dir.Path, "Game.main.prmtpak");

        var paths = WebPackageFonts.Extract(package, directory);

        paths
            .Should()
            .Equal(
                Path.GetFullPath(Path.Combine(directory, "a.otf")),
                Path.GetFullPath(Path.Combine(directory, "fonts", "b.ttf")),
                Path.GetFullPath(Path.Combine(directory, "fonts", "sub", "c.woff2"))
            );
        File.ReadAllText(paths[1]).Should().Be("font-b");
        File.Exists(Path.Combine(directory, "image.png")).Should().BeFalse();
    }

    [Fact]
    public void Extract_FromRealPackage_ShouldWork()
    {
        var packagePath = Path.Combine(_dir.Path, "Game.main.prmtpak");
        WritePackage(packagePath, ("assets/ui.ttf", "ttf-bytes"), ("assets/x.png", "png"));
        using var package = new PackageAssetSource(packagePath);

        var paths = WebPackageFonts.Extract(package, Path.Combine(_dir.Path, "fonts"));

        paths.Should().ContainSingle();
        File.ReadAllText(paths[0]).Should().Be("ttf-bytes");
    }

    [Fact]
    public void Extract_NullArguments_ShouldThrow()
    {
        var nullPackage = () => WebPackageFonts.Extract(null!, _dir.Path);
        var nullDirectory = () => WebPackageFonts.Extract(new InMemoryAssetSource(), null!);

        nullPackage.Should().Throw<ArgumentNullException>();
        nullDirectory.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Source_ShouldDelegateReadsToPackage()
    {
        var package = new InMemoryAssetSource(("a.txt", "a"), ("dir/b.txt", "b"));
        using var source = new WebPackageAssetSource(package, Path.Combine(_dir.Path, "fonts"));

        source.Exists("a.txt").Should().BeTrue();
        source.Exists("none.txt").Should().BeFalse();
        new StreamReader(source.OpenRead("dir/b.txt")).ReadToEnd().Should().Be("b");
        source.Enumerate("", "*", true).Should().BeEquivalentTo("a.txt", "dir/b.txt");
    }

    [Fact]
    public void Source_TryGetFilePath_ShouldReturnExtractedFontOnly()
    {
        var package = new InMemoryAssetSource(
            ("fonts/ui.ttf", "font"),
            ("fonts/other.ttf", "font2"),
            ("x.png", "png")
        );
        var directory = Path.Combine(_dir.Path, "fonts-out");
        var extracted = WebPackageFonts.Extract(package, directory);
        File.Delete(extracted[0]);
        using var source = new WebPackageAssetSource(package, directory);

        source.TryGetFilePath("fonts/ui.ttf", out var fullPath).Should().BeTrue();
        fullPath.Should().Be(Path.GetFullPath(Path.Combine(directory, "fonts", "ui.ttf")));
        source.TryGetFilePath("./fonts/ui.ttf", out _).Should().BeTrue("論理パスを正規化するはず");
        source.TryGetFilePath("fonts/other.ttf", out _).Should().BeFalse("取り出されていない");
        source.TryGetFilePath("x.png", out _).Should().BeFalse("フォントではない");
        source.TryGetFilePath("fonts/none.ttf", out _).Should().BeFalse("パッケージに無い");
    }

    [Fact]
    public void Source_TryGetFilePath_ShouldIgnoreFilesNotInPackage()
    {
        var directory = Path.Combine(_dir.Path, "fonts-out");
        _dir.CreateFile("fonts-out/stale.ttf", "stale");
        using var source = new WebPackageAssetSource(new InMemoryAssetSource(), directory);

        source.TryGetFilePath("stale.ttf", out _).Should().BeFalse();
    }

    [Fact]
    public void Source_Dispose_ShouldDisposePackageOnce()
    {
        var package = new InMemoryAssetSource();
        var source = new WebPackageAssetSource(package, _dir.Path);

        source.Dispose();
        source.Dispose();

        package.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Source_NullArguments_ShouldThrow()
    {
        var nullPackage = () => new WebPackageAssetSource(null!, _dir.Path);
        var nullDirectory = () => new WebPackageAssetSource(new InMemoryAssetSource(), null!);

        nullPackage.Should().Throw<ArgumentNullException>();
        nullDirectory.Should().Throw<ArgumentNullException>();
    }

    private static void WritePackage(string path, params (string Path, string Content)[] files)
    {
        var writer = new AssetPackageWriter();
        foreach (var (logical, content) in files)
            writer.Add(logical, Encoding.UTF8.GetBytes(content));
        writer.WriteTo(path);
    }
}
