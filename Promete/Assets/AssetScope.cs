namespace Promete.Assets;

/// <summary>
/// <see cref="AssetManager"/> で読み込んだアセットを、いつ解放するかを表します。 (v2.4~)
/// </summary>
public enum AssetScope
{
    /// <summary>
    /// アプリの終了まで残します。<see cref="AssetManager.Unload(string, AssetScope)"/> などで明示的に解放したときだけ解放します。
    /// </summary>
    Global,

    /// <summary>
    /// 読み込んだシーンが使われなくなったとき (<c>LoadScene</c> や <c>PopScene</c> で別のシーンに移ったとき) に解放します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// シーンのコンストラクタ (フィールド初期化子を含む) で読み込んだものは、生成中のそのシーンに帰属します。
    /// それ以外では、呼び出した時点の現在のシーンに帰属します。<c>PushScene</c> で下に積まれたシーンのものは残ります。
    /// </para>
    /// <para>
    /// メインスレッドからしか指定できません。シーンが 1 つも無いとき (アプリの組み立て中や、プラグインの <c>OnStart</c> の中など) にも指定できません。
    /// </para>
    /// </remarks>
    Scene,
}
