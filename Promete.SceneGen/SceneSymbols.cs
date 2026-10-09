using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Promete.SceneGen;

/// <summary>
/// シーンの登録可否をシンボルから判定する処理。ジェネレータとアナライザーで共有する。
/// </summary>
internal static class SceneSymbols
{
    public const string SceneBaseName = "Promete.Scene";
    public const string CoreAssemblyName = "Promete";
    public const string IgnoredAttributeName = "Promete.IgnoredSceneAttribute";

    /// <summary>
    /// 登録対象のシーンかどうかを判定する。
    /// </summary>
    public static bool IsRegistrableScene(INamedTypeSymbol type) =>
        IsInstantiableClass(type) && IsScene(type) && !IsIgnored(type);

    public static bool IsInstantiableClass(INamedTypeSymbol type) =>
        !type.IsAbstract
        && !type.IsStatic
        && !type.IsGenericType
        && type.TypeKind == TypeKind.Class;

    public static bool IsScene(INamedTypeSymbol type)
    {
        for (var b = type.BaseType; b is not null; b = b.BaseType)
        {
            if (b.ToDisplayString() == SceneBaseName)
                return true;
        }

        return false;
    }

    public static bool IsIgnored(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == IgnoredAttributeName)
                return true;
        }

        return false;
    }

    public static bool HasPublicConstructor(INamedTypeSymbol type)
    {
        foreach (var ctor in type.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Public)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 生成コードから参照できる可視性かどうか。private / protected なネスト型と、
    /// 宣言ファイルの外から参照できない file ローカル型は参照できない。
    /// </summary>
    public static bool IsAccessibleWithinAssembly(INamedTypeSymbol type)
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

    public static bool IsExternallyVisible(INamedTypeSymbol type)
    {
        for (ISymbol? s = type; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            if (s.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Promete 本体か、Promete を参照しているアセンブリかどうか。
    /// </summary>
    public static bool ReferencesCore(IAssemblySymbol asm)
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

    /// <summary>
    /// 名前空間以下の型を、ネスト型も含めて列挙する。
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol ns)
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
}
