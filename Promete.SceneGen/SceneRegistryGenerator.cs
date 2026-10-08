using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Promete.SceneGen;

/// <summary>
/// コンパイル時に <c>Promete.Scene</c> 派生クラスを列挙し、DI ファクトリつきで
/// <c>Promete.SceneRegistry</c> へ自己登録するコードを生成する。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SceneRegistryGenerator : IIncrementalGenerator
{
    private const string SceneBaseName = "Promete.Scene";
    private const string CoreAssemblyName = "Promete";
    private const string GeneratedNamespace = "Promete.Generated";
    private const string IgnoredAttributeName = "Promete.IgnoredSceneAttribute";

    private static readonly DiagnosticDescriptor _unreachableScene = new(
        "PROMETE0001",
        "シーンを自動登録できない",
        "シーン '{0}' は生成コードから参照できないため自動登録されません。private / protected なネストや file ローカルをやめ、internal 以上の可視性にしてください。",
        "Promete.SceneGen",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor _noPublicConstructor = new(
        "PROMETE0002",
        "シーンに公開コンストラクタが無い",
        "シーン '{0}' には公開コンストラクタが無いため自動登録されません。",
        "Promete.SceneGen",
        DiagnosticSeverity.Warning,
        true
    );

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // シンボルはインクリメンタル性を壊すので即座に値へ射影する。
        var ownScenes = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) =>
                    ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node)
                        is INamedTypeSymbol type
                    && IsRegistrableScene(type)
                        ? Describe(type, IsAccessibleWithinAssembly(type))
                        : (SceneInfo?)null
            )
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value)
            .Collect();

        // Compilation 全体に依存するため別ノードに切る。
        var referencedScenes = context.CompilationProvider.Select(
            static (compilation, _) => CollectFromReferences(compilation)
        );

        // build/Promete.props の CompilerVisibleProperty 経由で届く。
        var outputType = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.OutputType", out var value)
                    ? value
                    : null
        );

        context.RegisterSourceOutput(
            ownScenes.Combine(referencedScenes).Combine(outputType),
            static (spc, data) =>
            {
                var ((own, referenced), output) = data;
                Emit(spc, own, referenced, output);
            }
        );
    }

    /// <summary>
    /// 登録対象のシーンかどうかを判定する。
    /// </summary>
    private static bool IsRegistrableScene(INamedTypeSymbol type)
    {
        if (
            type.IsAbstract
            || type.IsStatic
            || type.IsGenericType
            || type.TypeKind != TypeKind.Class
        )
        {
            return false;
        }

        var isScene = false;
        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            if (b.ToDisplayString() == SceneBaseName)
            {
                isScene = true;
                break;
            }
        }

        if (!isScene)
            return false;

        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == IgnoredAttributeName)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 生成コードから参照できる可視性かどうか。private / protected なネスト型と、
    /// 宣言ファイルの外から参照できない file ローカル型は参照できない。
    /// </summary>
    private static bool IsAccessibleWithinAssembly(INamedTypeSymbol type)
    {
        for (ISymbol? s = type; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            if (s is INamedTypeSymbol { IsFileLocal: true })
                return false;

            switch (s.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    continue;
                default:
                    return false;
            }
        }

        return true;
    }

    private static bool IsExternallyVisible(INamedTypeSymbol type)
    {
        for (ISymbol? s = type; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            if (s.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }

    /// <summary>
    /// シーン型とファクトリ本体を文字列へ落とす。シンボルを持ち回らないことで
    /// インクリメンタル性を保つ。
    /// </summary>
    private static SceneInfo Describe(INamedTypeSymbol type, bool accessible)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var ctors = type
            .InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(c => c.Parameters.Length)
            .Select(c =>
                c.Parameters.Select(p =>
                        p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    )
                    .ToImmutableArray()
            )
            .ToList();

        if (ctors.Count == 0)
            return new SceneInfo(name, string.Empty, false, accessible);

        return new SceneInfo(name, BuildFactoryBody(name, ctors), true, accessible);
    }

    /// <summary>
    /// ファクトリ本体を組み立てる。公開コンストラクタが複数ある場合、MS.DI は引数を
    /// 解決できないコンストラクタを飛ばして次を試すため、その挙動を再現する。
    /// </summary>
    private static string BuildFactoryBody(string name, List<ImmutableArray<string>> ctors)
    {
        // 単一のコンストラクタなら解決失敗時のエラーが具体的な GetRequiredService に任せる。
        if (ctors.Count == 1)
            return $"        return new {name}({Required(ctors[0])});";

        var sb = new StringBuilder();

        for (var i = 0; i < ctors.Count; i++)
        {
            var parameters = ctors[i];
            if (parameters.Length == 0)
                continue;

            var conditions = parameters.Select(
                (t, j) => $"sp.GetService(typeof({t})) is {t} a{i}_{j}"
            );
            var arguments = string.Join(", ", parameters.Select((_, j) => $"a{i}_{j}"));

            sb.AppendLine($"        if ({string.Join("\n            && ", conditions)})");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new {name}({arguments});");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // どれも解決できなかった場合の着地点。引数なしがあればそれ、無ければ引数が
        // 最も多いものを GetRequiredService で呼んで具体的な例外を投げさせる。
        var last = ctors[ctors.Count - 1];
        sb.Append(
            last.Length == 0
                ? $"        return new {name}();"
                : $"        return new {name}({Required(ctors[0])});"
        );

        return sb.ToString();
    }

    private static string Required(ImmutableArray<string> parameters) =>
        string.Join(
            ", ",
            parameters.Select(t =>
                "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions"
                + $".GetRequiredService<{t}>(sp)"
            )
        );

    /// <summary>
    /// 参照アセンブリから Scene 派生を集める。Promete を参照していないアセンブリは
    /// 名前空間を辿らずに落とす。
    /// </summary>
    private static ReferencedResult CollectFromReferences(Compilation compilation)
    {
        var accessible = ImmutableArray.CreateBuilder<SceneInfo>();
        var scanned = 0;
        var pruned = 0;

        foreach (var asm in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!ReferencesCore(asm))
            {
                pruned++;
                continue;
            }

            scanned++;
            foreach (var type in EnumerateTypes(asm.GlobalNamespace))
            {
                if (!IsRegistrableScene(type))
                    continue;

                // 他アセンブリの internal 型は、そのライブラリ自身の生成コードが登録する
                // (ライブラリ側のビルドで可視性の問題は報告される) ため、ここでは無視する。
                if (!IsExternallyVisible(type))
                    continue;

                accessible.Add(Describe(type, true));
            }
        }

        return new ReferencedResult(accessible.ToImmutable(), scanned, pruned);
    }

    private static bool ReferencesCore(IAssemblySymbol asm)
    {
        if (asm.Name == CoreAssemblyName)
            return true;

        foreach (var module in asm.Modules)
        {
            foreach (var id in module.ReferencedAssemblies)
            {
                if (id.Name == CoreAssemblyName)
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in EnumerateNested(type))
                yield return nested;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in EnumerateTypes(child))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNested(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deeper in EnumerateNested(nested))
                yield return deeper;
        }
    }

    private static void Emit(
        SourceProductionContext spc,
        ImmutableArray<SceneInfo> own,
        ReferencedResult referenced,
        string? outputType
    )
    {
        // 実行可能プロジェクトでのみ参照アセンブリ分を集約する。ライブラリは自分の分だけ。
        var isExecutable = outputType is "Exe" or "WinExe";

        var registrable = new List<SceneInfo>();
        foreach (
            var scene in own.Concat(
                isExecutable ? referenced.Accessible : ImmutableArray<SceneInfo>.Empty
            )
        )
        {
            if (!scene.Accessible)
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(_unreachableScene, Location.None, scene.TypeName)
                );
                continue;
            }

            if (!scene.HasPublicConstructor)
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(_noPublicConstructor, Location.None, scene.TypeName)
                );
                continue;
            }

            registrable.Add(scene);
        }

        registrable = registrable.OrderBy(s => s.TypeName, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {GeneratedNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Promete.SceneGen が生成したシーンの登録処理。手で編集しないこと。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"// OutputType = {outputType ?? "(未取得)"} / aggregate = {isExecutable}");
        sb.AppendLine(
            $"// 走査した参照アセンブリ = {referenced.Scanned}, 枝刈り = {referenced.Pruned}"
        );
        sb.AppendLine(
            "[global::System.CodeDom.Compiler.GeneratedCode(\"Promete.SceneGen\", \"1\")]"
        );
        sb.AppendLine("internal static class GeneratedSceneRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// モジュール初期化子。Main より前に実行される。");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void Register()");
        sb.AppendLine("    {");
        for (var i = 0; i < registrable.Count; i++)
        {
            sb.AppendLine(
                $"        global::Promete.SceneRegistry.Add(typeof({registrable[i].TypeName}), "
                    + $"Create{i});"
            );
        }

        if (registrable.Count == 0)
            sb.AppendLine("        // 登録対象のシーンは無い");

        sb.AppendLine("    }");

        for (var i = 0; i < registrable.Count; i++)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"    private static global::Promete.Scene Create{i}"
                    + "(global::System.IServiceProvider sp)"
            );
            sb.AppendLine("    {");
            sb.AppendLine(registrable[i].FactoryBody);
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        spc.AddSource("GeneratedSceneRegistry.g.cs", sb.ToString());
    }

    private readonly record struct SceneInfo(
        string TypeName,
        string FactoryBody,
        bool HasPublicConstructor,
        bool Accessible
    );

    private readonly record struct ReferencedResult(
        ImmutableArray<SceneInfo> Accessible,
        int Scanned,
        int Pruned
    );
}
