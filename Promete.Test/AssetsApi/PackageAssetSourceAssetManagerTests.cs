using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;
using Promete.Audio;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// パッケージを <see cref="AssetManager"/> にマウントして読むテスト。<see cref="AssetsOptions.MountPackage"/> と <see cref="AssetsOptions.MountEmbeddedPackage"/> も含む。
/// </summary>
public sealed class PackageAssetSourceAssetManagerTests : IDisposable
{
    private static readonly System.Reflection.Assembly TestAssembly =
        typeof(PackageAssetSourceAssetManagerTests).Assembly;

    private static readonly byte[] Wav = TestAssetData.Wav(
        channels: 2,
        sampleRate: 22050,
        frames: 64
    );

    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void AssetManager_ShouldLoadTextureAudioAndCustomFromPackage()
    {
        var writer = SampleWriter();
        using var app = AssetsTestApp.Build(new PackageAssetSource(WriteToFile(writer)));
        var assets = app.Assets();

        var texture = assets.LoadTexture("assets/player.png", AssetScope.Global);
        var audio = assets.LoadAudio("assets/se.wav", AssetScope.Global);
        var text = assets.Load(
            "assets/data/stage.json",
            "text",
            stream => new StreamReader(stream).ReadToEnd(),
            AssetScope.Global
        );

        texture.Size.Should().Be(new VectorInt(8, 8));
        audio.Should().BeOfType<WaveAudioSource>();
        audio.SampleRate.Should().Be(22050);
        audio.Frames.Should().Be(64);
        text.Should().Be(StageJson);
    }

    [Theory]
    [InlineData(AssetCompression.Auto)]
    [InlineData(AssetCompression.Always)]
    public void LoadAudio_Ogg_ShouldDecodeFullyEvenIfUnmountedRightAway(
        AssetCompression compression
    )
    {
        // Vorbis は戻った後も別スレッドでストリームを読み続ける。Unmount しても最後まで読めること
        var ogg = TestAssetData.File("amaebi.ogg");
        var writer = new AssetPackageWriter();
        writer.Add("bgm/amaebi.ogg", ogg, compression);
        var source = new PackageAssetSource(WriteToFile(writer));
        using var app = AssetsTestApp.Build(source);

        var audio = (VorbisAudioSource)app.Assets().LoadAudio("bgm/amaebi.ogg", AssetScope.Global);
        app.Assets().FileSystem.Unmount(source).Should().BeTrue();

        SpinWait
            .SpinUntil(() => audio.IsLoadingFinished, TimeSpan.FromSeconds(30))
            .Should()
            .BeTrue();
        using var expected = new VorbisAudioSource(new MemoryStream(ogg));
        SpinWait
            .SpinUntil(() => expected.IsLoadingFinished, TimeSpan.FromSeconds(30))
            .Should()
            .BeTrue();
        audio.LoadedSize.Should().Be(expected.LoadedSize);
        audio.Frames.Should().Be(expected.Frames);
        var actualSamples = new float[expected.LoadedSize];
        var expectedSamples = new float[expected.LoadedSize];
        audio.FillSamples(actualSamples, 0);
        expected.FillSamples(expectedSamples, 0);
        actualSamples.Should().Equal(expectedSamples);
    }

    [Fact]
    public void Package_ShouldBeOverriddenByHigherPriorityFolder()
    {
        var writer = new AssetPackageWriter();
        writer.Add("assets/a.txt", Encoding.UTF8.GetBytes("package"));
        writer.Add("assets/b.txt", Encoding.UTF8.GetBytes("package"));
        var package = WriteToFile(writer);
        _dir.CreateFile("patch/a.txt", "patch");
        using var app = PrometeApp
            .Create()
            .UseAssets(
                TestAssembly,
                o =>
                    o.MountPackage(package)
                        .MountFolder(Path.Combine(_dir.Path, "patch"), "assets", 10)
            )
            .BuildWithHeadless();
        var fs = app.GetPlugin<AssetManager>().FileSystem;

        using (var a = fs.OpenRead("assets/a.txt"))
            a.ReadAllText().Should().Be("patch");
        using (var b = fs.OpenRead("assets/b.txt"))
            b.ReadAllText().Should().Be("package");
        fs.Enumerate("assets").Should().Equal("assets/a.txt", "assets/b.txt");
    }

    [Fact]
    public void MountPackage_ShouldMountWithMountPointAndPriority()
    {
        var low = new AssetPackageWriter();
        low.Add("x.txt", Encoding.UTF8.GetBytes("low"));
        var high = new AssetPackageWriter();
        high.Add("x.txt", Encoding.UTF8.GetBytes("high"));
        var lowPath = WriteToFile(low);
        var highPath = WriteToFile(high);
        using var app = PrometeApp
            .Create()
            .UseAssets(
                TestAssembly,
                o => o.MountPackage(highPath, "dlc", 5).MountPackage(lowPath, "dlc")
            )
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Sources.Should().HaveCount(2).And.AllBeOfType<PackageAssetSource>();
        using var stream = fs.OpenRead("dlc/x.txt");
        stream.ReadAllText().Should().Be("high");
    }

    [Fact]
    public void MountPackage_RelativePath_ShouldResolveFromBaseDirectory()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountPackage("assets/package-format/golden-v1.0.pak"))
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        fs.Exists("hello.txt").Should().BeTrue();
    }

    [Fact]
    public void MountPackage_Missing_ShouldThrowFileNotFoundWhenMounted()
    {
        var missing = Path.Combine(_dir.Path, "missing.pak");
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountPackage(missing))
            .BuildWithHeadless();

        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void MountPackage_Corrupt_ShouldThrowInvalidDataWhenMounted()
    {
        var broken = _dir.CreateFile(
            "broken.pak",
            "not a package at all, but long enough to read a header.........."
        );
        using var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountPackage(broken))
            .BuildWithHeadless();

        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void MountPackage_InvalidArguments_ShouldThrowAtConfiguration()
    {
        var options = new AssetsOptions();

        ((Action)(() => options.MountPackage(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => options.MountPackage("a.pak", null!)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => options.MountPackage("a.pak", "a\\b"))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MountEmbeddedPackage_ShouldMount()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(
                TestAssembly,
                o =>
                    o.MountEmbeddedPackage(TestAssembly, "package-format/golden-v1.0.pak", "golden")
            )
            .BuildWithHeadless();

        var fs = app.GetPlugin<AssetManager>().FileSystem;

        using var stream = fs.OpenRead("golden/hello.txt");
        stream.ReadAllText().Should().Be("Hello, Promete!\n");
    }

    [Fact]
    public void MountEmbeddedPackage_Missing_ShouldThrowFileNotFoundWhenMounted()
    {
        using var app = PrometeApp
            .Create()
            .UseAssets(
                TestAssembly,
                o => o.MountEmbeddedPackage(TestAssembly, "packages/missing.pak")
            )
            .BuildWithHeadless();

        var act = () => app.GetPlugin<AssetManager>();

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void MountEmbeddedPackage_InvalidArguments_ShouldThrowAtConfiguration()
    {
        var options = new AssetsOptions();

        ((Action)(() => options.MountEmbeddedPackage(null!, "a.pak")))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => options.MountEmbeddedPackage(TestAssembly, null!)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => options.MountEmbeddedPackage(TestAssembly, "a.pak", null!)))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => options.MountEmbeddedPackage(TestAssembly, "a.pak", "a\\b")))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void AppDispose_ShouldReleasePackageFile()
    {
        var path = WriteToFile(SampleWriter());
        var app = PrometeApp
            .Create()
            .UseAssets(TestAssembly, o => o.MountPackage(path))
            .BuildWithHeadless();
        using (var stream = app.GetPlugin<AssetManager>().FileSystem.OpenRead("assets/player.png"))
            stream.Length.Should().BeGreaterThan(0);

        app.Dispose();

        PackageAssetSourceOpenTests.AssertFileIsNotOpen(path);
    }

    private static string StageJson => string.Concat(Enumerable.Repeat("{\"stage\":1}", 100));

    private static AssetPackageWriter SampleWriter()
    {
        var writer = new AssetPackageWriter();
        writer.Add("assets/player.png", TestAssetData.Png(8, 8));
        writer.Add("assets/se.wav", Wav);
        writer.Add("assets/data/stage.json", Encoding.UTF8.GetBytes(StageJson));
        return writer;
    }

    private string WriteToFile(AssetPackageWriter writer)
    {
        var path = Path.Combine(_dir.Path, Guid.NewGuid().ToString("N") + ".pak");
        writer.WriteTo(path);
        return path;
    }
}
