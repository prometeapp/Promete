using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Promete.Audio;

namespace Promete.Assets;

/// <summary>
/// <see cref="PrometeApp.PrometeAppBuilder"/> に Assets API を登録する拡張メソッドを提供します。
/// </summary>
public static class AssetsAppBuilderExtension
{
    /// <summary>
    /// <see cref="AssetManager"/> を登録します。将来の MSBuild 統合 (<c>PrometeAsset</c>) で、アセンブリに書かれる属性に従ってマウントする予定です。
    /// 現在のバージョンでは属性は無いので、明示的なマウント (<paramref name="configure"/>) だけが効きます。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 現在のバージョンには属性が無いため、<paramref name="configure"/> を渡さなければ何もマウントしません。テストホストのように、エントリアセンブリがアプリではない環境でも例外にはなりません。
    /// </para>
    /// <para>
    /// エントリアセンブリを取得できない環境 (WebAssembly) では、<paramref name="configure"/> の有無に関係なく例外をスローします。
    /// その場合は <see cref="UseAssets(PrometeApp.PrometeAppBuilder, Assembly, Action{AssetsOptions}?)"/> を使ってください。
    /// </para>
    /// </remarks>
    /// <param name="builder">登録先のビルダー。</param>
    /// <param name="configure">明示的なマウントの設定。属性によるマウントより高い優先度で重なります。</param>
    /// <returns>メソッドチェーン用に、同じビルダーを返します。</returns>
    /// <exception cref="InvalidOperationException">エントリアセンブリを取得できない場合。または、既に <c>UseAssets</c> を呼んでいる場合。</exception>
    public static PrometeApp.PrometeAppBuilder UseAssets(
        this PrometeApp.PrometeAppBuilder builder,
        Action<AssetsOptions>? configure = null
    )
    {
        return builder.UseAssetsWithEntryAssembly(Assembly.GetEntryAssembly, configure);
    }

    /// <summary>
    /// <see cref="AssetManager"/> を登録します。将来の MSBuild 統合 (<c>PrometeAsset</c>) で、アセンブリに書かれる属性に従ってマウントする予定です。
    /// 現在のバージョンでは属性は無いので、明示的なマウント (<paramref name="configure"/>) だけが効きます。
    /// </summary>
    /// <param name="builder">登録先のビルダー。</param>
    /// <param name="assembly">マウントの対象となるアセンブリ。将来、Assets の属性 (MSBuild 統合で追加予定) を読むために使います。現在のバージョンでは属性は無いので、明示的なマウントだけが効きます。</param>
    /// <param name="configure">明示的なマウントの設定。属性によるマウントより高い優先度で重なります。</param>
    /// <returns>メソッドチェーン用に、同じビルダーを返します。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> が <see langword="null"/> の場合。</exception>
    /// <exception cref="InvalidOperationException">既に <c>UseAssets</c> を呼んでいる場合。</exception>
    public static PrometeApp.PrometeAppBuilder UseAssets(
        this PrometeApp.PrometeAppBuilder builder,
        Assembly assembly,
        Action<AssetsOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return Register(builder, assembly, configure);
    }

    /// <summary>
    /// エントリアセンブリの取得方法を差し替えて <see cref="UseAssets(PrometeApp.PrometeAppBuilder, Action{AssetsOptions}?)"/> を行います。テスト用です。
    /// </summary>
    internal static PrometeApp.PrometeAppBuilder UseAssetsWithEntryAssembly(
        this PrometeApp.PrometeAppBuilder builder,
        Func<Assembly?> getEntryAssembly,
        Action<AssetsOptions>? configure
    )
    {
        // アセンブリを総当たりで探すと、別の実行プロジェクトの属性を拾ってしまうため、エントリアセンブリだけを見る
        var entryAssembly = getEntryAssembly();
        if (entryAssembly is null)
        {
            throw new InvalidOperationException(
                "エントリアセンブリを取得できません。UseAssets(Assembly) で、Assets の属性を持つアセンブリを指定してください。"
            );
        }

        return Register(builder, entryAssembly, configure);
    }

    /// <summary>
    /// <see cref="AssetManager"/> をビルダーに登録します。2 回目の呼び出しは例外にします。
    /// </summary>
    private static PrometeApp.PrometeAppBuilder Register(
        PrometeApp.PrometeAppBuilder builder,
        Assembly assembly,
        Action<AssetsOptions>? configure
    )
    {
        if (builder.IsRegistered<AssetManager>())
        {
            throw new InvalidOperationException(
                "UseAssets は 1 つのアプリに対して 1 回だけ呼び出せます。"
            );
        }

        var options = new AssetsOptions();
        configure?.Invoke(options);

        return builder.UseFactory(sp => new AssetManager(
            assembly,
            options,
            sp.GetRequiredService<PrometeApp>(),
            sp.GetServices<IAudioDecoder>().ToArray()
        ));
    }
}
