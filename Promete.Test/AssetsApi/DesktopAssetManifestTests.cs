using System.Text;
using FluentAssertions;
using Promete.Assets.Infrastructure;

namespace Promete.Test.AssetsApi;

/// <summary>
/// デスクトップ形式のマニフェスト (<see cref="DesktopAssetManifest"/>) の読み込みのテスト。
/// </summary>
public sealed class DesktopAssetManifestTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Constants_ShouldMatchTheBuildContract()
    {
        DesktopAssetManifest.FormatVersion.Should().Be(1);
        DesktopAssetManifest.FileNameSuffix.Should().Be(".promete-assets.json");
    }

    [Fact]
    public void Parse_Valid_ShouldReturnFiles()
    {
        var files = Parse(
            """
            {
              "version": 1,
              "files": {
                "assets/a.png": "C:\\game\\assets\\a.png",
                "assets/b.txt": "/home/user/game/assets/b.txt"
              }
            }
            """
        );

        files.Should().HaveCount(2);
        files["assets/a.png"].Should().Be("C:\\game\\assets\\a.png");
        files["assets/b.txt"].Should().Be("/home/user/game/assets/b.txt");
    }

    [Fact]
    public void Parse_KeysShouldBeComparedOrdinally()
    {
        var files = Parse("""{ "version": 1, "files": { "a.txt": "/a", "A.txt": "/A" } }""");

        files.Should().HaveCount(2);
        files.ContainsKey("a.txt").Should().BeTrue();
        files.ContainsKey("A.TXT").Should().BeFalse();
    }

    [Fact]
    public void Parse_WithUtf8Bom_ShouldParse()
    {
        var json = Encoding.UTF8.GetBytes("""{ "version": 1, "files": { "a": "/a" } }""");
        byte[] withBom = [0xEF, 0xBB, 0xBF, .. json];

        DesktopAssetManifest.Parse(withBom, "'bom'").Should().ContainKey("a");
    }

    [Fact]
    public void Parse_UnknownProperties_ShouldBeIgnored()
    {
        var files = Parse(
            """
            {
              "generator": { "name": "x", "list": [1, 2, { "deep": true }] },
              "version": 1,
              "extra": [ "a", "b" ],
              "files": { "a": "/a" },
              "after": null
            }
            """
        );

        files.Should().ContainSingle().Which.Key.Should().Be("a");
    }

    [Fact]
    public void Parse_EmptyFiles_ShouldReturnEmpty()
    {
        Parse("""{ "version": 1, "files": {} }""").Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Parse_UnsupportedVersion_ShouldThrowNotSupported(int version)
    {
        var act = () => Parse($$"""{ "version": {{version}}, "files": {} }""");

        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("""{ "files": {} }""")]
    [InlineData("""{ "version": 1 }""")]
    [InlineData("""{ "version": "1", "files": {} }""")]
    [InlineData("""{ "version": 1.5, "files": {} }""")]
    [InlineData("""{ "version": 1, "files": [] }""")]
    [InlineData("""{ "version": 1, "files": { "a": 1 } }""")]
    [InlineData("""{ "version": 1, "files": { "a": null } }""")]
    [InlineData("""{ "version": 1, "files": { "a": "/a", "a": "/b" } }""")]
    [InlineData("""[ 1 ]""")]
    [InlineData("""{ "version": 1, "files": { "a": "/a" """)]
    [InlineData("not json")]
    [InlineData("")]
    public void Parse_Invalid_ShouldThrowInvalidDataWithDescription(string json)
    {
        var act = () => DesktopAssetManifest.Parse(Encoding.UTF8.GetBytes(json), "'my.json'");

        act.Should().Throw<InvalidDataException>().Where(e => e.Message.Contains("'my.json'"));
    }

    [Fact]
    public void Read_ShouldReadFile()
    {
        var path = _dir.CreateFile("m.json", """{ "version": 1, "files": { "a": "/a" } }""");

        DesktopAssetManifest.Read(path).Should().ContainKey("a");
    }

    [Fact]
    public void Read_InvalidFile_ShouldIncludePathInMessage()
    {
        var path = _dir.CreateFile("broken.json", "{");

        var act = () => DesktopAssetManifest.Read(path);

        act.Should().Throw<InvalidDataException>().Where(e => e.Message.Contains(path));
    }

    [Fact]
    public void Read_MissingFile_ShouldThrowFileNotFound()
    {
        var act = () => DesktopAssetManifest.Read(Path.Combine(_dir.Path, "none", "m.json"));

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Read_Null_ShouldThrowArgumentNull()
    {
        var act = () => DesktopAssetManifest.Read(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static IReadOnlyDictionary<string, string> Parse(string json) =>
        DesktopAssetManifest.Parse(Encoding.UTF8.GetBytes(json), "'test'");
}
