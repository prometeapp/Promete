// Promete.Web は browser 専用のアセンブリだが、ここではブラウザに依存しない部分だけをデスクトップで試す
#pragma warning disable CA1416

using System.Text;
using FluentAssertions;
using Promete.Web.Assets;

namespace Promete.Test.AssetsWeb;

/// <summary>
/// Web 形式のマニフェスト (<see cref="WebAssetManifest"/>) の読み込みのテスト。
/// </summary>
public sealed class WebAssetManifestTests
{
    [Fact]
    public void Constants_ShouldMatchTheContract()
    {
        WebAssetManifest.FormatVersion.Should().Be(1);
        WebAssetManifest.KindFile.Should().Be("file");
        WebAssetManifest.KindFont.Should().Be("font");
        WebAssetManifest.KindPackage.Should().Be("package");
        WebAssetManifest.KindEmbeddedPackage.Should().Be("embedded-package");
    }

    [Fact]
    public void Parse_ShouldReadAllKindsInOrder()
    {
        var entries = Parse(
            """
            {
              "version": 1,
              "entries": [
                { "kind": "file", "path": "/assets/x.png", "url": "assets/x.png", "logicalPath": "assets/x.png" },
                { "kind": "font", "path": "/assets/a.ttf", "url": "assets/a.ttf", "logicalPath": "assets/a.ttf" },
                { "kind": "package", "path": "/Game.main.prmtpak", "url": "Game.main.prmtpak", "package": "main" },
                { "kind": "embedded-package", "assembly": "Game", "resource": "Game.extra.prmtpak", "package": "extra" }
              ]
            }
            """
        );

        entries
            .Should()
            .Equal(
                new WebAssetManifestEntry(
                    "file",
                    "/assets/x.png",
                    "assets/x.png",
                    null,
                    null,
                    null
                ),
                new WebAssetManifestEntry(
                    "font",
                    "/assets/a.ttf",
                    "assets/a.ttf",
                    null,
                    null,
                    null
                ),
                new WebAssetManifestEntry(
                    "package",
                    "/Game.main.prmtpak",
                    null,
                    "main",
                    null,
                    null
                ),
                new WebAssetManifestEntry(
                    "embedded-package",
                    null,
                    null,
                    "extra",
                    "Game",
                    "Game.extra.prmtpak"
                )
            );
    }

    [Fact]
    public void Parse_UnknownPropertiesAndKinds_ShouldBeKeptOrIgnored()
    {
        var entries = Parse(
            """
            {
              "meta": { "a": [1, { "b": null }] },
              "version": 1,
              "entries": [
                { "kind": "bundle", "path": "/x", "hash": { "sha256": "..." }, "size": 10 }
              ]
            }
            """
        );

        entries
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new WebAssetManifestEntry("bundle", "/x", null, null, null, null));
    }

    [Fact]
    public void Parse_WithBom_ShouldParse()
    {
        byte[] json =
        [
            0xEF,
            0xBB,
            0xBF,
            .. Encoding.UTF8.GetBytes("""{ "version": 1, "entries": [] }"""),
        ];

        WebAssetManifest.Parse(json, "'bom'").Should().BeEmpty();
    }

    [Fact]
    public void Parse_UnsupportedVersion_ShouldThrowNotSupported()
    {
        var act = () => Parse("""{ "version": 2, "entries": [] }""");

        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("""[]""")]
    [InlineData("""{ "entries": [] }""")]
    [InlineData("""{ "version": 1 }""")]
    [InlineData("""{ "version": 1, "entries": {} }""")]
    [InlineData("""{ "version": 1, "entries": [ 1 ] }""")]
    [InlineData("""{ "version": 1, "entries": [ { "path": "/x" } ] }""")]
    [InlineData("""{ "version": 1, "entries": [ { "kind": 1 } ] }""")]
    [InlineData("""{ "version": 1, "entries": [ { "kind": "file", "logicalPath": 3 } ] }""")]
    [InlineData("""{ "version": 1, "entries": [ """)]
    [InlineData("not json")]
    public void Parse_Invalid_ShouldThrowInvalidDataWithDescription(string json)
    {
        var act = () => WebAssetManifest.Parse(Encoding.UTF8.GetBytes(json), "'web.json'");

        act.Should().Throw<InvalidDataException>().Where(e => e.Message.Contains("'web.json'"));
    }

    private static IReadOnlyList<WebAssetManifestEntry> Parse(string json) =>
        WebAssetManifest.Parse(Encoding.UTF8.GetBytes(json), "'test'");
}
