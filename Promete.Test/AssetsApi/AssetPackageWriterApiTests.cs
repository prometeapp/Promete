using FluentAssertions;
using Promete.AssetPacker;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetPackageWriter"/> のエントリの追加 (<c>Add</c> / <c>AddFile</c>) と、拡張子による圧縮の判定のテスト。
/// </summary>
public sealed class AssetPackageWriterApiTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void FormatVersion_ShouldBe1_0()
    {
        AssetPackageWriter.FormatMajorVersion.Should().Be(1);
        AssetPackageWriter.FormatMinorVersion.Should().Be(0);
    }

    [Fact]
    public void Count_ShouldReflectAddedEntries()
    {
        var writer = new AssetPackageWriter();
        writer.Count.Should().Be(0);

        writer.Add("a.txt", [1]);
        writer.AddFile("b.txt", _dir.CreateFile("b.txt", "b"));

        writer.Count.Should().Be(2);
    }

    [Theory]
    [InlineData("a.txt", "/a.txt")]
    [InlineData("dir/a.txt", "dir/./x/../a.txt")]
    [InlineData("が.txt", "\u304B\u3099.txt")]
    public void Add_DuplicateAfterNormalization_ShouldThrow(string first, string second)
    {
        var writer = new AssetPackageWriter();
        writer.Add(first, [1]);

        var act = () => writer.Add(second, [2]);

        act.Should().Throw<ArgumentException>();
        writer.Count.Should().Be(1);
    }

    [Fact]
    public void AddFile_DuplicateOfAdd_ShouldThrow()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", [1]);

        var act = () => writer.AddFile("/a.txt", _dir.CreateFile("a.txt", "a"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_PathsDifferingOnlyInCase_ShouldBeSeparateEntries()
    {
        var writer = new AssetPackageWriter();

        writer.Add("Player.png", [1]);
        writer.Add("player.png", [2]);

        writer.Count.Should().Be(2);
    }

    [Theory]
    [InlineData("a\\b.txt")]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("a/..")]
    public void Add_InvalidPath_ShouldThrow(string path)
    {
        var writer = new AssetPackageWriter();

        var act = () => writer.Add(path, [1]);

        act.Should().Throw<ArgumentException>();
        writer.Count.Should().Be(0);
    }

    [Fact]
    public void Add_TooLongPath_ShouldThrow()
    {
        var writer = new AssetPackageWriter();

        // 3 バイトの文字 × 21846 = 65538 バイト > 65535
        var act = () => writer.Add(new string('あ', 21846), [1]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_LongestAllowedPath_ShouldBeAccepted()
    {
        var writer = new AssetPackageWriter();

        writer.Add(new string('a', 65535), [1]);

        writer.Count.Should().Be(1);
    }

    [Fact]
    public void Add_NullArguments_ShouldThrowArgumentNull()
    {
        var writer = new AssetPackageWriter();

        ((Action)(() => writer.Add(null!, [1]))).Should().Throw<ArgumentNullException>();
        ((Action)(() => writer.Add("a.txt", null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => writer.AddFile(null!, "x"))).Should().Throw<ArgumentNullException>();
        ((Action)(() => writer.AddFile("a.txt", null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Add_UndefinedCompression_ShouldThrow()
    {
        var writer = new AssetPackageWriter();

        var act = () => writer.Add("a.txt", [1], (AssetCompression)99);

        act.Should().Throw<ArgumentException>();
        writer.Count.Should().Be(0);
    }

    [Fact]
    public void AddFile_MissingFile_ShouldThrowFileNotFound()
    {
        var writer = new AssetPackageWriter();

        var act = () => writer.AddFile("a.txt", Path.Combine(_dir.Path, "missing.txt"));

        act.Should().Throw<FileNotFoundException>();
        writer.Count.Should().Be(0);
    }

    [Theory]
    [InlineData("a.png")]
    [InlineData("A.PNG")]
    [InlineData("dir/a.jpg")]
    [InlineData("a.jpeg")]
    [InlineData("a.gif")]
    [InlineData("a.webp")]
    [InlineData("a.avif")]
    [InlineData("bgm.ogg")]
    [InlineData("a.oga")]
    [InlineData("a.opus")]
    [InlineData("a.mp3")]
    [InlineData("a.m4a")]
    [InlineData("a.aac")]
    [InlineData("a.flac")]
    [InlineData("se.wav")]
    [InlineData("se.WAV")]
    [InlineData("a.mp4")]
    [InlineData("a.webm")]
    [InlineData("a.woff")]
    [InlineData("a.woff2")]
    [InlineData("a.zip")]
    [InlineData("a.gz")]
    [InlineData("a.7z")]
    [InlineData("dir.txt/a.png")]
    public void IsAutoCompressed_KnownCompressedOrSeekableFormats_ShouldBeFalse(string path)
    {
        AssetPackageWriter.IsAutoCompressed(path).Should().BeFalse();
    }

    [Theory]
    [InlineData("a.txt")]
    [InlineData("a.json")]
    [InlineData("a.ttf")]
    [InlineData("a.otf")]
    [InlineData("a.bin")]
    [InlineData("a.mid")]
    [InlineData("noextension")]
    [InlineData("dir.png/readme")]
    [InlineData("a.png.txt")]
    [InlineData(".png.bak")]
    public void IsAutoCompressed_OtherFormats_ShouldBeTrue(string path)
    {
        AssetPackageWriter.IsAutoCompressed(path).Should().BeTrue();
    }

    [Fact]
    public void IsAutoCompressed_Null_ShouldThrowArgumentNull()
    {
        var act = () => AssetPackageWriter.IsAutoCompressed(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
