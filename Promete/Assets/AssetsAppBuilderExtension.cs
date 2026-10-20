using System;
using System.Reflection;

namespace Promete.Assets;

/// <summary>
/// <see cref="PrometeApp.PrometeAppBuilder"/> に Assets API を登録する拡張メソッドを提供します。
/// </summary>
public static class AssetsAppBuilderExtension
{
    /// <summary>
    /// <see cref="AssetManager"/> を登録します。エントリアセンブリに Assets の属性があれば、それに従ってマウントします。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 属性が無ければ何もマウントしません。テストホストのように、エントリアセンブリがアプリではない環境でも例外にはなりません。
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="AssetManager"/> を登録します。指定したアセンブリに Assets の属性があれば、それに従ってマウントします。
    /// </summary>
    /// <param name="builder">登録先のビルダー。</param>
    /// <param name="assembly">Assets の属性を持つアセンブリ。属性が無ければ、明示的なマウントだけが効きます。</param>
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
