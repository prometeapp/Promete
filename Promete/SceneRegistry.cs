using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Promete;

/// <summary>
/// エンジンに読み込まれたシーンを管理します。シーンは通常、自動的に読み込まれるので明示的な登録は不要です。
/// シーンのメタ情報を取得するのに使用できますが、非公開APIのため互換性が保たれない場合があります。
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SceneRegistry
{
    private static readonly Lock _gate = new();

    private static readonly Dictionary<Type, Func<IServiceProvider, Scene>> _factories = new();

    /// <summary>
    /// シーンとその生成処理を登録します。生成コードから呼ばれます。
    /// </summary>
    /// <param name="sceneType">シーンの型。</param>
    /// <param name="factory">シーンを生成するファクトリ。</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Add(Type sceneType, Func<IServiceProvider, Scene> factory)
    {
        ArgumentNullException.ThrowIfNull(sceneType);
        ArgumentNullException.ThrowIfNull(factory);

        lock (_gate)
        {
            _factories[sceneType] = factory;
        }
    }

    /// <summary>
    /// 指定したアセンブリに含まれる、登録済みシーンの型を列挙します。
    /// </summary>
    /// <param name="assembly">対象のアセンブリ。</param>
    /// <returns>シーンの型。</returns>
    public static IReadOnlyList<Type> GetSceneTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var result = new List<Type>();
        lock (_gate)
        {
            foreach (var type in _factories.Keys)
            {
                if (type.Assembly == assembly)
                    result.Add(type);
            }
        }

        return result;
    }

    /// <summary>
    /// 指定したアセンブリの登録処理が実行済みであることを保証します。
    /// </summary>
    /// <param name="assembly">対象のアセンブリ。</param>
    internal static void EnsureRegistered(Assembly assembly)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (NotSupportedException)
        {
            // 明示的に走らせられない環境では通常の読み込み時の実行に委ねる。
        }
    }

    /// <summary>
    /// 指定したアセンブリに含まれる、登録済みシーンとそのファクトリを列挙します。
    /// </summary>
    /// <param name="assembly">対象のアセンブリ。</param>
    /// <returns>シーンの型とファクトリの組。</returns>
    internal static List<(Type Type, Func<IServiceProvider, Scene> Factory)> GetScenesIn(
        Assembly assembly
    )
    {
        var result = new List<(Type, Func<IServiceProvider, Scene>)>();
        lock (_gate)
        {
            foreach (var pair in _factories)
            {
                if (pair.Key.Assembly == assembly)
                    result.Add((pair.Key, pair.Value));
            }
        }

        return result;
    }
}
