using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// MSBuild の統合テスト用に、一時ディレクトリへ小さなプロジェクトを生成し、<c>dotnet</c> を子プロセスで実行する。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WorkspaceKind.Repository"/> では、リポジトリの <c>Directory.Build.props</c> / <c>.targets</c> を取り込む
/// (リポジトリ内のプロジェクトと同じく、<c>Promete/build/Promete.targets</c> が効く)。
/// Promete などの参照先は、テストを実行している構成 (<see cref="Configuration"/>) で既にビルドされている前提で、
/// <c>BuildProjectReferences=false</c> でビルドし直さない (Promete.Test が参照しているので、テストの前にビルドされる)。
/// </para>
/// <para>
/// <see cref="WorkspaceKind.Isolated"/> では、空の <c>Directory.Build.props</c> / <c>.targets</c> を置き、外の設定を遮る (NuGet 経由のテスト用)。
/// </para>
/// </remarks>
internal sealed class MSBuildWorkspace : IDisposable
{
    /// <summary>
    /// 1 回の <c>dotnet</c> の実行の上限。
    /// </summary>
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 子プロセスに引き継がない環境変数。テストホストや Visual Studio の MSBuild の設定が、子の <c>dotnet</c> に混ざらないようにする。
    /// </summary>
    private static readonly string[] RemovedVariables =
    [
        "MSBuildExtensionsPath",
        "MSBUILD_EXE_PATH",
        "MSBuildSDKsPath",
        "MSBuildLoadMicrosoftTargetsReadOnly",
        "MSBUILDNOINPROCNODE",
        "VSTEST_HOST_DEBUG",
        "VSTEST_RUNNER_DEBUG",
    ];

    public MSBuildWorkspace(WorkspaceKind kind = WorkspaceKind.Repository)
    {
        Root = Path.Combine(
            Path.GetTempPath(),
            "promete-msbuild-test",
            Guid.NewGuid().ToString("N")[..12]
        );
        Directory.CreateDirectory(Root);

        if (kind == WorkspaceKind.Repository)
        {
            Write(
                "Directory.Build.props",
                $"""<Project><Import Project="{RepoRoot}/Directory.Build.props" /></Project>"""
            );
            Write(
                "Directory.Build.targets",
                $"""<Project><Import Project="{RepoRoot}/Directory.Build.targets" /></Project>"""
            );
        }
        else
        {
            Write("Directory.Build.props", "<Project />");
            Write("Directory.Build.targets", "<Project />");
        }
    }

    /// <summary>
    /// 生成するプロジェクトの種類。
    /// </summary>
    public enum WorkspaceKind
    {
        /// <summary>リポジトリの Directory.Build.* を取り込む。</summary>
        Repository,

        /// <summary>外の設定を取り込まない。</summary>
        Isolated,
    }

    /// <summary>
    /// リポジトリのルート (<c>Promete.slnx</c> のあるディレクトリ)。区切りは <c>/</c>。
    /// </summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>
    /// テストを実行している構成。参照先はこの構成でビルドされている。
    /// </summary>
    public static string Configuration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    /// <summary>
    /// NuGet のグローバルパッケージフォルダ。
    /// </summary>
    public static string GlobalPackagesFolder =>
        Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } path
            ? path
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages"
            );

    /// <summary>
    /// 一時ディレクトリのルート。
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// アセットを読んで結果を出力する、実行プロジェクトの Program.cs。
    /// 引数: 1 つ目は <c>entry</c> (引数なしの <c>UseAssets()</c>) かアセンブリ名 (<c>UseAssets(Assembly.Load(名前))</c>)、2 つ目以降は読む論理パス。
    /// 出力: <c>SOURCES 型名,...</c>、論理パスごとに <c>READ 番号 中身</c> か <c>ERROR 番号 例外の型名</c>、起動に失敗したら <c>STARTUP 例外の型名 メッセージ</c>。
    /// 番号は引数の位置 (1 から) で、ASCII 以外のパスを出力しないためのもの。
    /// </summary>
    public const string ProbeProgram = """
        using System;
        using System.IO;
        using System.Linq;
        using System.Reflection;
        using Promete;
        using Promete.Assets;
        using Promete.Headless;

        public static class Program
        {
            public static void Main(string[] args)
            {
                try
                {
                    var builder = PrometeApp.Create();
                    builder = args[0] == "entry" ? builder.UseAssets() : builder.UseAssets(Assembly.Load(args[0]));
                    using var app = builder.BuildWithHeadless();
                    var fs = app.GetPlugin<AssetManager>().FileSystem;
                    Console.WriteLine("SOURCES " + string.Join(",", fs.Sources.Select(s => s.GetType().Name)));
                    for (var i = 1; i < args.Length; i++)
                    {
                        try
                        {
                            using var reader = new StreamReader(fs.OpenRead(args[i]));
                            Console.WriteLine($"READ {i} {reader.ReadToEnd().Trim()}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"ERROR {i} {e.GetType().Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"STARTUP {e.GetType().Name} {e.Message.ReplaceLineEndings(" ")}");
                }
            }
        }
        """;

    /// <summary>
    /// ルートからの相対パス (<c>/</c> 区切り) にファイルを書く。親ディレクトリも作る。
    /// </summary>
    /// <returns>書いたファイルのフルパス。</returns>
    public string Write(string relativePath, string content)
    {
        var path = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    /// <summary>
    /// ルートからの相対パス (<c>/</c> 区切り) をフルパスにする。
    /// </summary>
    public string FullPath(string relativePath) =>
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Promete を参照する、デスクトップの実行プロジェクト (Program.cs は <see cref="ProbeProgram"/>) を作る。
    /// </summary>
    /// <param name="name">プロジェクト名 (= ディレクトリ名、アセンブリ名)。</param>
    /// <param name="properties">PropertyGroup に足す XML。</param>
    /// <param name="items">ItemGroup に足す XML。</param>
    /// <param name="outputType">OutputType。<c>Library</c> のときは Program.cs を作らない。</param>
    /// <returns>csproj のフルパス。</returns>
    public string CreateDesktopProject(
        string name,
        string properties = "",
        string items = "",
        string outputType = "Exe"
    )
    {
        if (outputType != "Library")
            Write($"{name}/Program.cs", ProbeProgram);
        else
            Write($"{name}/Library.cs", $"namespace {name}; public static class Marker {{ }}");

        return Write(
            $"{name}/{name}.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>{outputType}</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
                {properties}
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{RepoRoot}/Promete/Promete.csproj" />
                {items}
              </ItemGroup>
            </Project>
            """
        );
    }

    /// <summary>
    /// <c>Microsoft.NET.Sdk.WebAssembly</c> の実行プロジェクトを作る。
    /// </summary>
    /// <param name="name">プロジェクト名。</param>
    /// <param name="withPrometeWeb">
    /// <see langword="true"/> なら、Web Example と同じく Promete.Web の props / targets を取り込み、Promete.Web を参照する (契約に対応した Promete.Web)。
    /// <see langword="false"/> なら、Promete.Web を使わず Promete だけを参照する (契約のプロパティを立てない、古い Promete.Web の代わり)。
    /// </param>
    /// <param name="properties">PropertyGroup に足す XML。</param>
    /// <param name="items">ItemGroup に足す XML。</param>
    /// <returns>csproj のフルパス。</returns>
    /// <remarks>
    /// ネイティブのリンク (emscripten) と AOT は時間がかかり、wasm-tools のワークロードも要るので止める。アセットの処理には関係しない。
    /// RunAOTCompilation は false にすると「既定から変えた」とみなされネイティブのリンクを要求されるので、空に戻す
    /// (Promete.Web.props が Release で true にするのを打ち消す)。
    /// </remarks>
    public string CreateWebProject(
        string name,
        bool withPrometeWeb,
        string properties = "",
        string items = ""
    )
    {
        Write(
            $"{name}/Program.cs",
            "public static class Program { public static void Main() { } }"
        );
        var reference = withPrometeWeb
            ? $"""<ProjectReference Include="{RepoRoot}/Promete.Web/Promete.Web.csproj" />"""
            : $"""<ProjectReference Include="{RepoRoot}/Promete/Promete.csproj" />""";
        var props = withPrometeWeb
            ? $"""<Import Project="{RepoRoot}/Promete.Web/build/Promete.Web.props" />"""
            : string.Empty;
        var targets = withPrometeWeb
            ? $"""<Import Project="{RepoRoot}/Promete.Web/build/Promete.Web.targets" />"""
            : string.Empty;

        return Write(
            $"{name}/{name}.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk.WebAssembly">
              {props}
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <WasmBuildNative>false</WasmBuildNative>
                <RunAOTCompilation></RunAOTCompilation>
                <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
                <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
                {properties}
              </PropertyGroup>
              <ItemGroup>
                {reference}
                {items}
              </ItemGroup>
              {targets}
            </Project>
            """
        );
    }

    /// <summary>
    /// <c>dotnet build</c> を実行する。参照先はビルドし直さない。
    /// </summary>
    public ProcessResult Build(string project, params string[] arguments) =>
        Dotnet(
            Root,
            ["build", project, "-c", Configuration, "-p:BuildProjectReferences=false", .. arguments]
        );

    /// <summary>
    /// <c>dotnet publish</c> を実行する。参照先はビルドし直さない。
    /// </summary>
    public ProcessResult Publish(
        string project,
        string outputDirectory,
        params string[] arguments
    ) =>
        Dotnet(
            Root,
            [
                "publish",
                project,
                "-c",
                Configuration,
                "-o",
                outputDirectory,
                "-p:BuildProjectReferences=false",
                .. arguments,
            ]
        );

    /// <summary>
    /// プロジェクトを評価して、item を取得する (<c>dotnet msbuild -getItem</c>。ビルドはしない)。
    /// </summary>
    /// <returns>item の Identity と、メタデータの辞書。</returns>
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> GetItems(
        string project,
        string itemType,
        params string[] arguments
    )
    {
        var result = Dotnet(
            Path.GetDirectoryName(project)!,
            ["msbuild", project, $"-getItem:{itemType}", .. arguments]
        );
        result.EnsureSucceeded();
        using var json = JsonDocument.Parse(ExtractJson(result.Output));
        if (!json.RootElement.GetProperty("Items").TryGetProperty(itemType, out var items))
            return [];

        return items
            .EnumerateArray()
            .Select(item =>
                (IReadOnlyDictionary<string, string>)
                    item.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty)
            )
            .ToArray();
    }

    /// <summary>
    /// プロジェクトを評価して、プロパティの値を取得する (<c>dotnet msbuild -getProperty</c>。ビルドはしない)。
    /// </summary>
    public static string GetProperty(string project, string property, params string[] arguments)
    {
        var result = Dotnet(
            Path.GetDirectoryName(project)!,
            ["msbuild", project, $"-getProperty:{property}", .. arguments]
        );
        result.EnsureSucceeded();
        return result.Output.Trim();
    }

    /// <summary>
    /// ビルドした実行プロジェクトを <c>dotnet &lt;dll&gt;</c> で実行する。作業ディレクトリは dll のあるディレクトリ。
    /// </summary>
    public static ProcessResult RunApp(string dll, params string[] arguments) =>
        Dotnet(Path.GetDirectoryName(dll)!, [dll, .. arguments]);

    /// <summary>
    /// プロジェクトの出力先 (<c>bin/&lt;構成&gt;/net10.0/</c>) を返す。
    /// </summary>
    public string OutputDirectory(string projectName, string? runtimeIdentifier = null) =>
        runtimeIdentifier is null
            ? FullPath($"{projectName}/bin/{Configuration}/net10.0")
            : FullPath($"{projectName}/bin/{Configuration}/net10.0/{runtimeIdentifier}");

    /// <summary>
    /// プロジェクトの中間出力先 (<c>obj/&lt;構成&gt;/net10.0/</c>) を返す。
    /// </summary>
    public string IntermediateDirectory(string projectName, string? runtimeIdentifier = null) =>
        runtimeIdentifier is null
            ? FullPath($"{projectName}/obj/{Configuration}/net10.0")
            : FullPath($"{projectName}/obj/{Configuration}/net10.0/{runtimeIdentifier}");

    /// <summary>
    /// コンパイラに渡された、生成された AssemblyInfo.cs の中身を返す (属性の確認用)。無ければ空文字列。
    /// </summary>
    public string ReadAssemblyInfo(string projectName, string? runtimeIdentifier = null)
    {
        var path = Path.Combine(
            IntermediateDirectory(projectName, runtimeIdentifier),
            $"{projectName}.AssemblyInfo.cs"
        );
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// <c>dotnet</c> を子プロセスで実行し、標準出力と標準エラーをまとめて返す。
    /// </summary>
    public static ProcessResult Dotnet(string workingDirectory, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(
            Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host
                ? host
                : "dotnet"
        )
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        if (start.ArgumentList[0] is "build" or "publish" or "msbuild" or "pack" or "restore")
        {
            start.ArgumentList.Add("-nodeReuse:false");
            start.ArgumentList.Add("-nologo");
        }

        foreach (var name in RemovedVariables)
            start.Environment.Remove(name);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        using var process = Process.Start(start)!;
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (output)
                    output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (output)
                    output.AppendLine(e.Data);
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (!process.WaitForExit(ProcessTimeout))
        {
            process.Kill(true);
            throw new TimeoutException(
                $"dotnet {string.Join(' ', start.ArgumentList)} が {ProcessTimeout} で終わりませんでした。\n{output}"
            );
        }

        process.WaitForExit();
        lock (output)
            return new ProcessResult(
                process.ExitCode,
                output.ToString(),
                $"dotnet {string.Join(' ', start.ArgumentList)}"
            );
    }

    public void Dispose()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(Root))
                    Directory.Delete(Root, true);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // 終了直後のプロセスがファイルを掴んでいることがあるので、少し待って再試行する
                Thread.Sleep(500);
            }
        }
    }

    /// <summary>
    /// 出力から、最初の <c>{</c> から最後の <c>}</c> までを取り出す (警告などが前後に混ざることがあるため)。
    /// </summary>
    private static string ExtractJson(string output)
    {
        var start = output.IndexOf('{');
        var end = output.LastIndexOf('}');
        return start >= 0 && end > start ? output[start..(end + 1)] : output;
    }

    /// <summary>
    /// テストのアセンブリの場所から上にたどり、<c>Promete.slnx</c> のあるディレクトリを探す。
    /// </summary>
    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null && !File.Exists(Path.Combine(directory.FullName, "Promete.slnx"))
        )
            directory = directory.Parent;

        return directory?.FullName.Replace('\\', '/')
            ?? throw new InvalidOperationException(
                "リポジトリのルート (Promete.slnx) が見つかりません。"
            );
    }
}

/// <summary>
/// 子プロセスの <c>dotnet</c> の結果。
/// </summary>
/// <param name="ExitCode">終了コード。</param>
/// <param name="Output">標準出力と標準エラー。</param>
/// <param name="CommandLine">実行したコマンド (失敗したときのメッセージ用)。</param>
internal sealed record ProcessResult(int ExitCode, string Output, string CommandLine)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// 出力の行のうち、指定した接頭辞で始まるもの (アプリの出力を読むときに使う)。
    /// </summary>
    public IEnumerable<string> Lines(string prefix) =>
        Output
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>
    /// 指定したコードの警告かエラー (<c>warning PROMETE1012:</c> など) が出たかどうか。
    /// </summary>
    public bool HasDiagnostic(string code) =>
        Output.Contains($"warning {code}:", StringComparison.Ordinal)
        || Output.Contains($"error {code}:", StringComparison.Ordinal);

    /// <summary>
    /// Promete の Assets の警告・エラー (<c>PROMETE1xxx</c>) が 1 つでも出たかどうか。
    /// </summary>
    public bool HasAnyAssetsDiagnostic() =>
        System.Text.RegularExpressions.Regex.IsMatch(Output, @"(warning|error) PROMETE1\d{3}:");

    /// <summary>
    /// 成功していなければ、出力を含めて例外にする。
    /// </summary>
    public ProcessResult EnsureSucceeded()
    {
        if (!Succeeded)
            throw new Xunit.Sdk.XunitException(
                $"{CommandLine} が失敗しました (終了コード {ExitCode})。\n{Output}"
            );

        return this;
    }

    public override string ToString() => $"{CommandLine} (終了コード {ExitCode})\n{Output}";
}
