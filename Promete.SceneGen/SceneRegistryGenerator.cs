using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Promete.SceneGen;

/// <summary>
/// コンパイル時に <c>Promete.Scene</c> 派生クラスを列挙し、DI ファクトリつきの
/// レジストリを生成する。実行時のリフレクション (Assembly.GetTypes) を不要にし、
/// トリムおよび NativeAOT で動作させるためのもの。
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SceneRegistryGenerator : IIncrementalGenerator
{
    private const string SceneBaseName = "Promete.Scene";
    private const string CoreAssemblyName = "Promete";
    private const string GeneratedNamespace = "Promete.Generated";
    private const string IgnoredAttributeName = "Promete.IgnoredSceneAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 自アセンブリのシーン。シンボルはインクリメンタル性を壊すので即座に値へ射影する。
        var ownScenes = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                static (ctx, _) =>
                    ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node)
                        is INamedTypeSymbol type
                    && IsConcreteScene(type)
                    && IsAccessibleWithinAssembly(type)
                        ? Describe(type)
                        : null
            )
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value)
            .Collect();

        // 参照アセンブリのシーン。Compilation 全体に依存するため別ノードに切る。
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

    private static bool IsConcreteScene(INamedTypeSymbol type)
    {
        if (
            type.IsAbstract
            || type.IsStatic
            || type.IsGenericType
            || type.TypeKind != TypeKind.Class
        )
            return false;

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

        // RegisterScenesIn と同じ意味論: [IgnoredScene] の付いた型は登録しない。
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == IgnoredAttributeName)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 生成コードは同一アセンブリ内のトップレベル internal クラスに置かれる。
    /// private / protected なネスト型はそこから参照できない。
    /// </summary>
    private static bool IsAccessibleWithinAssembly(INamedTypeSymbol type)
    {
        for (ISymbol? s = type; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
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

    /// <summary>
    /// シーン型とその DI コンストラクタを文字列に落とす。MS.DI と同じく
    /// 公開コンストラクタのうち引数が最も多いものを選ぶ。
    /// </summary>
    private static SceneInfo? Describe(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var ctor = type
            .InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (ctor is null)
            return new SceneInfo(name, ImmutableArray<string>.Empty, false);

        var parameters = ctor
            .Parameters.Select(p =>
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            )
            .ToImmutableArray();

        return new SceneInfo(name, parameters, true);
    }

    /// <summary>
    /// 参照アセンブリから Scene 派生を集める。Promete を参照していないアセンブリは
    /// 名前空間を辿らずに落とす。ビルド時間への影響は実測では無視できる範囲だったが、
    /// このノードは Compilation の変化ごとに再実行されるため IDE 応答性のために残す。
    /// </summary>
    private static ReferencedResult CollectFromReferences(Compilation compilation)
    {
        var accessible = ImmutableArray.CreateBuilder<SceneInfo>();
        var inaccessible = ImmutableArray.CreateBuilder<string>();
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
                if (!IsConcreteScene(type))
                    continue;

                // 生成コードは他アセンブリの internal 型を参照できない。
                if (!IsExternallyVisible(type))
                {
                    inaccessible.Add(
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                    continue;
                }

                if (Describe(type) is { } info)
                    accessible.Add(info);
            }
        }

        return new ReferencedResult(
            accessible.ToImmutable(),
            inaccessible.ToImmutable(),
            scanned,
            pruned
        );
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

    private static bool IsExternallyVisible(INamedTypeSymbol type)
    {
        for (ISymbol? s = type; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            if (s.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
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
        // 実行可能プロジェクトでのみ参照アセンブリ分を集約する。
        // ライブラリは自分の分だけを出し、二重登録を避ける。
        var isExecutable = outputType is "Exe" or "WinExe";

        var scenes = own.ToList();
        if (isExecutable)
            scenes.AddRange(referenced.Accessible);
        scenes = scenes.OrderBy(s => s.TypeName, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {GeneratedNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Promete.SceneGen が生成したシーンのレジストリ。手で編集しないこと。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"// OutputType = {outputType ?? "(未取得)"} / aggregate = {isExecutable}");
        sb.AppendLine(
            $"// 走査した参照アセンブリ = {referenced.Scanned}, 枝刈り = {referenced.Pruned}"
        );
        foreach (var name in referenced.Inaccessible)
            sb.AppendLine($"// 外部から参照不可のため別パスが必要: {name}");
        sb.AppendLine(
            "[global::System.CodeDom.Compiler.GeneratedCode(\"Promete.SceneGen\", \"1\")]"
        );
        sb.AppendLine("internal static class GeneratedSceneRegistry");
        sb.AppendLine("{");
        sb.AppendLine(
            "    internal static readonly (global::System.Type Type, "
                + "global::System.Func<global::System.IServiceProvider, global::Promete.Scene> Factory)[] Scenes ="
        );
        sb.AppendLine("    {");
        foreach (var scene in scenes)
        {
            if (!scene.HasPublicConstructor)
            {
                sb.AppendLine($"        // 公開コンストラクタが無いため除外: {scene.TypeName}");
                continue;
            }

            var args = string.Join(
                ", ",
                scene.ParameterTypes.Select(t =>
                    "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions"
                    + $".GetRequiredService<{t}>(sp)"
                )
            );
            sb.AppendLine(
                $"        (typeof({scene.TypeName}), static sp => new {scene.TypeName}({args})),"
            );
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        spc.AddSource("GeneratedSceneRegistry.g.cs", sb.ToString());
    }

    private readonly record struct SceneInfo(
        string TypeName,
        ImmutableArray<string> ParameterTypes,
        bool HasPublicConstructor
    );

    private readonly record struct ReferencedResult(
        ImmutableArray<SceneInfo> Accessible,
        ImmutableArray<string> Inaccessible,
        int Scanned,
        int Pruned
    );
}
