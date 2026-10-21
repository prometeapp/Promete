using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// <see cref="AssetPackageWriter"/> で書いたパッケージを <see cref="PackageAssetSource"/> で読むラウンドトリップのテスト。
/// </summary>
public sealed class AssetPackageRoundTripTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    public static TheoryData<string> Kinds => new() { "stream", "file", "embedded" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void RoundTrip_ShouldReadEveryEntryAsWritten(string kind)
    {
        var files = SampleFiles();
        var writer = new AssetPackageWriter();
        foreach (var (path, content, compression) in files)
            writer.Add(path, content, compression);

        using var source = Open(writer, kind);

        foreach (var (path, content, _) in files)
        {
            using var stream = source.OpenRead(path);
            stream.CanSeek.Should().BeTrue(path);
            PackageAssetSourceTests.ReadAllBytes(stream).Should().Equal(content, path);
        }

        source
            .Enumerate("", "*", true)
            .Should()
            .BeEquivalentTo(files.Select(f => f.Path))
            .And.BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void RoundTrip_AddFile_ShouldReadFileContents()
    {
        var png = TestAssetData.Png(8, 8);
        var pngPath = Path.Combine(_dir.Path, "player.png");
        File.WriteAllBytes(pngPath, png);
        var jsonPath = _dir.CreateFile(
            "data/stage.json",
            string.Concat(Enumerable.Repeat("{\"a\":1}", 300))
        );
        var writer = new AssetPackageWriter();
        writer.AddFile("assets/player.png", pngPath);
        writer.AddFile("assets/data/stage.json", jsonPath);

        using var source = Open(writer, "file");

        using (var stream = source.OpenRead("assets/player.png"))
            PackageAssetSourceTests.ReadAllBytes(stream).Should().Equal(png);
        using (var stream = source.OpenRead("assets/data/stage.json"))
            PackageAssetSourceTests
                .ReadAllBytes(stream)
                .Should()
                .Equal(File.ReadAllBytes(jsonPath));
    }

    [Fact]
    public void RoundTrip_NfdPathWrittenAndRead_ShouldMatchEitherForm()
    {
        var writer = new AssetPackageWriter();
        writer.Add("フォルダ/\u304B\u3099.txt", Encoding.UTF8.GetBytes("nfd"));

        using var source = Open(writer, "stream");

        source.Exists("フォルダ/が.txt").Should().BeTrue();
        source.Exists("フォルダ/\u304B\u3099.txt").Should().BeTrue();
        source.Enumerate("フォルダ", "*", false).Should().Equal("フォルダ/が.txt");
    }

    [Fact]
    public void RoundTrip_CaseMismatch_ShouldGiveHint()
    {
        var writer = new AssetPackageWriter();
        writer.Add("Assets/Player.png", [1]);

        using var source = Open(writer, "stream");

        var act = () => source.OpenRead("assets/player.png");
        act.Should()
            .Throw<AssetPathCaseMismatchException>()
            .Which.ActualPath.Should()
            .Be("Assets/Player.png");
    }

    [Fact]
    public void RoundTrip_EmptyPackage_ShouldOpen()
    {
        using var source = Open(new AssetPackageWriter(), "file");

        source.Enumerate("", "*", true).Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_WrittenAfterPrefix_ShouldOpenFromThatPosition()
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", Encoding.UTF8.GetBytes("A"));
        using var output = new MemoryStream();
        output.Write(new byte[123]);
        writer.WriteTo(output);
        output.Position = 123;

        using var source = new PackageAssetSource(output, leaveOpen: true);

        using var stream = source.OpenRead("a.txt");
        stream.ReadAllText().Should().Be("A");
    }

    private static List<(
        string Path,
        byte[] Content,
        AssetCompression Compression
    )> SampleFiles() =>
        [
            ("assets/player.png", TestAssetData.Png(16, 16), AssetCompression.Auto),
            ("assets/se.wav", TestAssetData.Wav(frames: 256), AssetCompression.Auto),
            ("assets/bgm.ogg", TestAssetData.File("amaebi.ogg"), AssetCompression.Auto),
            (
                "assets/data/stage.json",
                RawPackageLayout.CompressibleText("{\"tile\": 1}"),
                AssetCompression.Auto
            ),
            ("assets/data/random.bin", RawPackageLayout.RandomBytes(10000), AssetCompression.Auto),
            ("assets/data/forced.png", TestAssetData.Png(4, 4), AssetCompression.Always),
            (
                "assets/data/raw.txt",
                RawPackageLayout.CompressibleText("raw"),
                AssetCompression.Never
            ),
            ("assets/empty.txt", [], AssetCompression.Auto),
            ("assets/empty-forced.txt", [], AssetCompression.Always),
            ("日本語/テキスト.txt", Encoding.UTF8.GetBytes("こんにちは"), AssetCompression.Auto),
            ("Case/A.txt", Encoding.UTF8.GetBytes("upper"), AssetCompression.Auto),
            ("Case/a.txt", Encoding.UTF8.GetBytes("lower"), AssetCompression.Auto),
        ];

    private PackageAssetSource Open(AssetPackageWriter writer, string kind)
    {
        switch (kind)
        {
            case "file":
            {
                var path = Path.Combine(_dir.Path, Guid.NewGuid().ToString("N") + ".pak");
                writer.WriteTo(path);
                return new PackageAssetSource(path);
            }
            case "embedded":
            {
                using var output = new MemoryStream();
                writer.WriteTo(output);
                var assembly = FakeResourceAssembly.WithBinary(("game.main.pak", output.ToArray()));
                return PackageAssetSource.FromEmbeddedResource(assembly, "game.main.pak");
            }
            default:
            {
                var output = new MemoryStream();
                writer.WriteTo(output);
                output.Position = 0;
                return new PackageAssetSource(output);
            }
        }
    }
}
