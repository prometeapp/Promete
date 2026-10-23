using System.Text.RegularExpressions;
using FluentAssertions;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// Promete.Example と Promete.Web.Example を <c>PrometeAsset</c> と AssetManager に移行したことの検査。
/// </summary>
/// <remarks>
/// ソースに書かれたアセットのパス (<c>"assets/..."</c>) がすべて <c>PrometeAsset</c> で宣言されていること (Package モードで読めなくならないこと) と、
/// パスを直接ファイルの API に渡していないことを、ソースの文字列とプロジェクトの評価 (ビルドはしない) で確かめる。
/// 実際に動かす確認 (GL / ブラウザ) は手動で行う。
/// </remarks>
[Collection(MSBuildCollection.Name)]
[Trait("Category", MSBuildCollection.Category)]
public sealed class ExampleAssetsMigrationTests
{
    /// <summary>
    /// ソースの中のアセットのパスの文字列リテラル。<c>"./assets/x.png"</c> や <c>"/assets/x.png"</c> も拾う。
    /// </summary>
    private static readonly Regex AssetLiteral = new(
        @"""(?:\./|/)?(assets/[^""]+\.(?:png|jpg|ttf|otf|wav|ogg))""",
        RegexOptions.Compiled
    );

    /// <summary>
    /// アセットのパスを直接ファイルとして開く API。AssetManager に移したら、アセットのパスと同じ行には現れないはず。
    /// </summary>
    private static readonly string[] DirectLoadPatterns =
    [
        "TextureFactory.",
        "textureFactory.",
        "factory.Load",
        "Font.FromFile(",
        "AudioSource(",
        "= new(",
        "GetStreamAsync(",
        "File.Open",
        "File.Read",
    ];

    private static string ExampleProject =>
        $"{MSBuildWorkspace.RepoRoot}/Promete.Example/Promete.Example.csproj";

    private static string WebExampleProject =>
        $"{MSBuildWorkspace.RepoRoot}/Promete.Web.Example/Promete.Web.Example.csproj";

    [Fact]
    public void Example_ShouldDeclareEveryAssetItsSourceReads()
    {
        var declared = DeclaredLogicalPaths(ExampleProject);

        LiteralsIn("Promete.Example").Select(l => l.Path).Distinct().Should().BeSubsetOf(declared);
    }

    [Fact]
    public void Example_ShouldNotDeclareEditorFiles()
    {
        DeclaredLogicalPaths(ExampleProject).Should().NotContain(p => p.EndsWith(".aseprite"));
    }

    [Fact]
    public void Example_ShouldNotCopyAssetsWithNoneItems()
    {
        MSBuildWorkspace
            .GetItems(ExampleProject, "None")
            .Where(i => i["Identity"].Replace('\\', '/').StartsWith("assets/"))
            .Should()
            .AllSatisfy(i =>
                i.GetValueOrDefault("CopyToOutputDirectory", string.Empty).Should().BeEmpty()
            );
    }

    [Fact]
    public void Example_ShouldUseAttributeBasedMounting()
    {
        var program = File.ReadAllText($"{MSBuildWorkspace.RepoRoot}/Promete.Example/Program.cs");

        program.Should().Contain(".UseAssets()");
        program
            .Should()
            .NotContain(
                "MountFolder(",
                "PrometeAsset の属性でマウントするので、フォルダの明示的なマウントは要らない"
            );
    }

    [Fact]
    public void WebExample_ShouldDeclareEveryAssetTheDemosRead()
    {
        var declared = DeclaredLogicalPaths(WebExampleProject);
        var literals = LiteralsIn("Promete.Example").Concat(LiteralsIn("Promete.Web.Example"));

        literals.Select(l => l.Path).Distinct().Should().BeSubsetOf(declared);
    }

    [Fact]
    public void WebExample_ShouldUseAssetsOfItsOwnAssemblyWithoutMountFolder()
    {
        var program = File.ReadAllText(
            $"{MSBuildWorkspace.RepoRoot}/Promete.Web.Example/Program.cs"
        );

        program.Should().Contain("UseAssets(typeof(Program).Assembly)");
        program
            .Should()
            .NotContain(
                "MountFolder(",
                "Package モードではばらのファイルを仮想ファイルシステムに置かないので、/assets は無い"
            );
    }

    [Fact]
    public void Examples_ShouldNotPassAssetPathsToFileApisDirectly()
    {
        var offenders = LiteralsIn("Promete.Example")
            .Concat(LiteralsIn("Promete.Web.Example"))
            .Where(l => DirectLoadPatterns.Any(p => l.Line.Contains(p, StringComparison.Ordinal)))
            .Select(l => $"{l.File}: {l.Line.Trim()}")
            .ToArray();

        offenders
            .Should()
            .BeEmpty(
                "アセットは AssetManager から読む (Package モードではファイルとして存在しない)"
            );
    }

    /// <summary>
    /// プロジェクトを評価し、PrometeAsset の論理パス (TargetPath、無ければ Include のパス) を求める。
    /// </summary>
    private static string[] DeclaredLogicalPaths(string project) =>
        MSBuildWorkspace
            .GetItems(project, "PrometeAsset")
            .Select(i =>
                i.GetValueOrDefault("TargetPath") is { Length: > 0 } targetPath
                    ? targetPath
                    : i["Identity"]
            )
            .Select(p => p.Replace('\\', '/').TrimStart('/'))
            .ToArray();

    /// <summary>
    /// プロジェクトのディレクトリにある C# のソースから、アセットのパスの文字列リテラルを集める (bin / obj を除く)。
    /// </summary>
    private static IEnumerable<(string File, string Line, string Path)> LiteralsIn(
        string projectDirectory
    )
    {
        var root = $"{MSBuildWorkspace.RepoRoot}/{projectDirectory}";
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.StartsWith("bin/") || relative.StartsWith("obj/"))
                continue;

            foreach (var line in File.ReadLines(file))
            {
                foreach (Match match in AssetLiteral.Matches(line))
                    yield return (relative, line, match.Groups[1].Value);
            }
        }
    }
}
