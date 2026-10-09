using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Promete.SceneGen;

namespace Promete.Test;

public class SceneUsageAnalyzerTests
{
    private const string Usings = """
        using Promete;
        using System.Reflection;

        """;

    private const string LibSource = """
        namespace ContentLib;

        public class LibScene : Promete.Scene { }

        public class OtherLibScene : Promete.Scene { }
        """;

    private static readonly ImmutableArray<MetadataReference> BaseReferences =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            // 自身はシーンを含むので、参照先のシーンとして数えられないよう除く
            .Where(p => Path.GetFileName(p) != "Promete.Test.dll")
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)),
    ];

    [Fact]
    public async Task LoadingLibraryScene_WithoutUseScenesFrom_ReportsWarning()
    {
        var diagnostics = await AnalyzeAsync(
            """
            var builder = PrometeApp.Create();
            PrometeApp app = null!;
            app.LoadScene<ContentLib.LibScene>();
            """
        );

        diagnostics.Select(d => d.Id).Should().Equal("PROMETE0003");
        diagnostics[0].GetMessage().Should().Contain("ContentLib.LibScene");
    }

    [Theory]
    [InlineData("PrometeApp.Create().UseScenesFrom<ContentLib.OtherLibScene>();")]
    [InlineData("PrometeApp.Create().UseScenesFrom(typeof(ContentLib.LibScene).Assembly);")]
    public async Task LoadingLibraryScene_WithUseScenesFrom_ReportsNothing(string create)
    {
        var diagnostics = await AnalyzeAsync(
            $$"""
            {{create}}
            PrometeApp app = null!;
            app.PushScene(typeof(ContentLib.LibScene));
            """
        );

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UnresolvableUseScenesFrom_SuppressesAssemblyDiagnostics()
    {
        var diagnostics = await AnalyzeAsync(
            """
            Assembly asm = null!;
            PrometeApp.Create().UseScenesFrom(asm);
            PrometeApp app = null!;
            app.LoadScene<ContentLib.LibScene>();
            """
        );

        diagnostics.Should().BeEmpty("指定先を静的に決められないときは誤検知を避けて黙るはず");
    }

    [Fact]
    public async Task Library_DoesNotReportAssemblyDiagnostics()
    {
        var diagnostics = await AnalyzeAsync(
            """
            public static class Navigator
            {
                public static void Go(PrometeApp app)
                {
                    PrometeApp.Create();
                    app.LoadScene<ContentLib.LibScene>();
                }
            }
            """,
            OutputKind.DynamicallyLinkedLibrary
        );

        diagnostics.Should().BeEmpty("UseScenesFrom はエントリアセンブリ側で指定するため");
    }

    [Theory]
    [InlineData("public class LibScene : Promete.Scene { }")]
    [InlineData("internal class LibScene : Promete.Scene { }")]
    public async Task ReferencedScenes_WithoutUseScenesFrom_ReportsInfo(string libSource)
    {
        var diagnostics = await AnalyzeAsync(
            "PrometeApp.Create();",
            libSource: $"namespace ContentLib;\n{libSource}"
        );

        diagnostics.Should().ContainSingle();
        diagnostics[0].Id.Should().Be("PROMETE0004");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
    }

    [Fact]
    public async Task ReferencedScenes_WithoutCreate_ReportsNothing()
    {
        var diagnostics = await AnalyzeAsync("System.Console.WriteLine();");

        diagnostics.Should().BeEmpty("アプリを組み立てていないプロジェクトでは判断できないため");
    }

    [Theory]
    [InlineData("[IgnoredScene] public class Target : Scene { }", "[IgnoredScene]")]
    [InlineData("public abstract class Target : Scene { }", "抽象クラス")]
    [InlineData("public class Target : Scene { private Target() { } }", "公開コンストラクタ")]
    [InlineData("public class Target { }", "Scene を継承していません")]
    public async Task LoadingUnregistrableScene_ReportsWarning(string declaration, string reason)
    {
        var diagnostics = await AnalyzeAsync(
            $$"""
            PrometeApp.Create().UseScenesFrom<ContentLib.LibScene>();
            PrometeApp app = null!;
            app.PushScene<Target>();

            {{declaration}}
            """
        );

        diagnostics.Select(d => d.Id).Should().Equal("PROMETE0005");
        diagnostics[0].GetMessage().Should().Contain(reason);
    }

    [Fact]
    public async Task LoadingPrivateNestedScene_ReportsWarning()
    {
        var diagnostics = await AnalyzeAsync(
            """
            PrometeApp.Create().UseScenesFrom<ContentLib.LibScene>();

            public class Outer
            {
                public void Go(PrometeApp app) => app.LoadScene<Inner>();

                private class Inner : Scene { }
            }
            """
        );

        diagnostics.Select(d => d.Id).Should().Equal("PROMETE0005");
    }

    [Fact]
    public async Task LoadingIgnoredScene_RegisteredWithUse_ReportsNothing()
    {
        var diagnostics = await AnalyzeAsync(
            """
            PrometeApp.Create().UseScenesFrom<ContentLib.LibScene>().Use<Target>();
            PrometeApp app = null!;
            app.LoadScene<Target>();

            [IgnoredScene] public class Target : Scene { }
            """
        );

        diagnostics.Should().BeEmpty("Use で手動登録したシーンは読み込めるため");
    }

    [Fact]
    public async Task LoadingOwnScene_ReportsNothing()
    {
        var diagnostics = await AnalyzeAsync(
            """
            PrometeApp.Create().UseScenesFrom<ContentLib.LibScene>();
            PrometeApp app = null!;
            app.Run<Target>();

            public class Target : Scene { }
            """
        );

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// シーンを含むライブラリ ContentLib を参照するコンパイルにアナライザーをかける。
    /// </summary>
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        OutputKind outputKind = OutputKind.ConsoleApplication,
        string libSource = LibSource
    )
    {
        var lib = Compile("ContentLib", libSource, OutputKind.DynamicallyLinkedLibrary, []);
        using var image = new MemoryStream();
        var emitted = lib.Emit(image);
        emitted.Success.Should().BeTrue(string.Join("\n", emitted.Diagnostics));

        var app = Compile(
            "App",
            Usings + source,
            outputKind,
            [MetadataReference.CreateFromImage(image.ToArray())]
        );
        app.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

        return await app.WithAnalyzers([new SceneUsageAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }

    private static CSharpCompilation Compile(
        string name,
        string source,
        OutputKind outputKind,
        IEnumerable<MetadataReference> references
    ) =>
        CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            BaseReferences.Concat(references),
            new CSharpCompilationOptions(
                outputKind,
                nullableContextOptions: NullableContextOptions.Enable
            )
        );
}
