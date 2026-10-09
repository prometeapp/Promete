using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Promete.SceneGen;

/// <summary>
/// シーンの読み込みと <c>UseScenesFrom</c> の呼び出しを調べ、実行時に
/// 「シーンが登録されていない」と失敗する使い方をビルド時に知らせる。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SceneUsageAnalyzer : DiagnosticAnalyzer
{
    private const string AppTypeName = "Promete.PrometeApp";
    private const string BuilderTypeName = "Promete.PrometeApp.PrometeAppBuilder";

    private static readonly DiagnosticDescriptor _sceneAssemblyNotUsed = new(
        "PROMETE0003",
        "UseScenesFrom で指定していないアセンブリのシーンを読み込んでいる",
        "シーン '{0}' のあるアセンブリ '{1}' が UseScenesFrom で指定されていないため、読み込むと実行時に失敗します。ビルダーで UseScenesFrom<{2}>() を呼んでください。",
        "Promete.SceneGen",
        DiagnosticSeverity.Warning,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    private static readonly DiagnosticDescriptor _referencedScenesNotUsed = new(
        "PROMETE0004",
        "参照先のシーンが UseScenesFrom で指定されていない",
        "参照先のアセンブリ '{0}' にはシーンがありますが、UseScenesFrom で指定されていないため登録されません。そのシーンを使う場合は、ビルダーで UseScenesFrom を呼んでください。",
        "Promete.SceneGen",
        DiagnosticSeverity.Info,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    private static readonly DiagnosticDescriptor _unregistrableSceneLoaded = new(
        "PROMETE0005",
        "自動登録されないシーンを読み込んでいる",
        "シーン '{0}' は自動登録されないため、読み込むと実行時に失敗します ({1})。",
        "Promete.SceneGen",
        DiagnosticSeverity.Warning,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            _sceneAssemblyNotUsed,
            _referencedScenesNotUsed,
            _unregistrableSceneLoaded
        );

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            var state = new CompilationState();
            start.RegisterOperationAction(state.OnInvocation, OperationKind.Invocation);
            start.RegisterCompilationEndAction(state.OnCompilationEnd);
        });
    }

    /// <summary>
    /// 自動登録されない理由を返す。登録されるなら null。
    /// </summary>
    private static string? GetUnregistrableReason(INamedTypeSymbol type)
    {
        if (!SceneSymbols.IsScene(type))
            return "Scene を継承していません";
        if (SceneSymbols.IsIgnored(type))
            return "[IgnoredScene] が付いています";
        if (type.IsAbstract)
            return "抽象クラスです";
        if (type.IsGenericType)
            return "ジェネリック型です";
        if (!SceneSymbols.IsAccessibleWithinAssembly(type))
            return "生成コードから参照できない可視性です";
        if (!SceneSymbols.HasPublicConstructor(type))
            return "公開コンストラクタがありません";

        return null;
    }

    /// <summary>
    /// <c>LoadScene&lt;T&gt;()</c> や <c>LoadScene(typeof(T))</c> から、読み込むシーンの型を取り出す。
    /// 型パラメータなど静的に決まらないものは null。
    /// </summary>
    private static INamedTypeSymbol? GetSceneArgument(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        var type =
            method.TypeArguments.Length == 1 ? method.TypeArguments[0]
            : invocation.Arguments.Length == 1
            && invocation.Arguments[0].Value is ITypeOfOperation typeOf
                ? typeOf.TypeOperand
            : null;

        return type is INamedTypeSymbol { TypeKind: not TypeKind.Error } named ? named : null;
    }

    /// <summary>
    /// <c>UseScenesFrom</c> が指すアセンブリを解決する。
    /// <c>UseScenesFrom&lt;T&gt;()</c> と <c>UseScenesFrom(typeof(T).Assembly)</c> 以外は解決できない。
    /// </summary>
    private static IAssemblySymbol? ResolveSceneAssembly(IInvocationOperation invocation)
    {
        var method = invocation.TargetMethod;
        if (method.TypeArguments.Length == 1)
            return method.TypeArguments[0] is INamedTypeSymbol t ? t.ContainingAssembly : null;

        if (
            invocation.Arguments.Length == 1
            && invocation.Arguments[0].Value
                is IPropertyReferenceOperation
                {
                    Property.Name: "Assembly",
                    Instance: ITypeOfOperation { TypeOperand: INamedTypeSymbol operand },
                }
        )
        {
            return operand.ContainingAssembly;
        }

        return null;
    }

    private static bool IsExecutable(Compilation compilation) =>
        compilation.Options.OutputKind
            is OutputKind.ConsoleApplication
                or OutputKind.WindowsApplication
                or OutputKind.WindowsRuntimeApplication;

    private readonly record struct SceneLoad(INamedTypeSymbol Scene, Location Location);

    /// <summary>
    /// 1 回のコンパイルで集めた呼び出しの情報。操作単位のコールバックは並列に呼ばれる。
    /// </summary>
    private sealed class CompilationState
    {
        private readonly ConcurrentBag<SceneLoad> _loads = new();
        private readonly ConcurrentBag<Location> _creates = new();
        private readonly ConcurrentDictionary<ISymbol, byte> _sceneAssemblies = new(
            SymbolEqualityComparer.Default
        );
        private readonly ConcurrentDictionary<ISymbol, byte> _manuallyRegistered = new(
            SymbolEqualityComparer.Default
        );
        private volatile bool _hasUnresolvedSceneAssembly;

        public void OnInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;

            switch (method.ContainingType?.ToDisplayString())
            {
                case AppTypeName:
                    if (method.Name == "Create" && method.IsStatic)
                    {
                        _creates.Add(invocation.Syntax.GetLocation());
                    }
                    else if (
                        method.Name is "LoadScene" or "PushScene" or "Run"
                        && GetSceneArgument(invocation) is { } scene
                    )
                    {
                        _loads.Add(new SceneLoad(scene, invocation.Syntax.GetLocation()));
                    }

                    break;

                case BuilderTypeName:
                    if (method.Name == "UseScenesFrom")
                    {
                        if (ResolveSceneAssembly(invocation) is { } asm)
                            _sceneAssemblies.TryAdd(asm, 0);
                        else
                            _hasUnresolvedSceneAssembly = true;
                    }
                    else if (method.Name == "Use" && method.TypeArguments.Length > 0)
                    {
                        // Use<T>() で手動登録されたシーンは、自動登録されなくても読み込める。
                        _manuallyRegistered.TryAdd(method.TypeArguments[0], 0);
                    }

                    break;
            }
        }

        public void OnCompilationEnd(CompilationAnalysisContext context)
        {
            var compilation = context.Compilation;
            var own = compilation.Assembly;

            // UseScenesFrom の指定はエントリアセンブリ側で行うため、実行可能プロジェクトでのみ調べる。
            // アプリを組み立てていない、または指定先を静的に決められない場合は誤検知を避けて黙る。
            var canTrack =
                IsExecutable(compilation) && !_creates.IsEmpty && !_hasUnresolvedSceneAssembly;

            var reported = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            foreach (var load in _loads)
            {
                var scene = load.Scene;
                var asm = scene.ContainingAssembly;

                // 本体のシーン (DefaultScene) は手動で登録している。
                if (asm is null || asm.Name == SceneSymbols.CoreAssemblyName)
                    continue;
                if (_manuallyRegistered.ContainsKey(scene))
                    continue;

                if (GetUnregistrableReason(scene) is { } reason)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            _unregistrableSceneLoaded,
                            load.Location,
                            scene.ToDisplayString(),
                            reason
                        )
                    );
                    continue;
                }

                if (
                    !canTrack
                    || SymbolEqualityComparer.Default.Equals(asm, own)
                    || _sceneAssemblies.ContainsKey(asm)
                )
                {
                    continue;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        _sceneAssemblyNotUsed,
                        load.Location,
                        scene.ToDisplayString(),
                        asm.Name,
                        scene.ToDisplayString()
                    )
                );
                reported.Add(asm);
            }

            if (!canTrack)
                return;

            // 位置は決定的にしたいので、ソース上で最初の Create() に出す。
            var createLocation = _creates
                .OrderBy(l => l.SourceTree?.FilePath, System.StringComparer.Ordinal)
                .ThenBy(l => l.SourceSpan.Start)
                .First();

            foreach (var asm in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                if (
                    asm.Name == SceneSymbols.CoreAssemblyName
                    || reported.Contains(asm)
                    || _sceneAssemblies.ContainsKey(asm)
                    || !SceneSymbols.ReferencesCore(asm)
                    || !ContainsRegistrableScene(asm)
                )
                {
                    continue;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(_referencedScenesNotUsed, createLocation, asm.Name)
                );
            }
        }

        private static bool ContainsRegistrableScene(IAssemblySymbol asm) =>
            SceneSymbols
                .EnumerateTypes(asm.GlobalNamespace)
                .Any(t =>
                    SceneSymbols.IsRegistrableScene(t)
                    && SceneSymbols.IsAccessibleWithinAssembly(t)
                    && SceneSymbols.HasPublicConstructor(t)
                );
    }
}
