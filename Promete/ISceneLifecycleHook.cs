namespace Promete;

/// <summary>
/// シーンの生成と破棄を、コアの内部の機能 (Assets のシーンのスコープなど) に知らせるフックです。
/// <see cref="PrometeApp.AddSceneLifecycleHook"/> で登録します。
/// </summary>
/// <remarks>
/// <para>
/// すべてのメソッドは、シーンの遷移を行ったスレッド (メインスレッド) から呼ばれます。
/// </para>
/// <para>
/// 呼ばれる順序は次のとおりです。
/// </para>
/// <list type="bullet">
/// <item>シーンを生成するとき (<c>LoadScene</c> / <c>PushScene</c>): <see cref="OnSceneConstructing"/> の後、成功すれば <see cref="OnSceneConstructed"/>、例外なら <see cref="OnSceneConstructionFailed"/> が呼ばれます。どちらも <c>SceneWillChange</c> より前です。</item>
/// <item>シーンが使われなくなったとき: そのシーンの <c>OnDestroy</c> より後に <see cref="OnSceneReleased"/> が呼ばれます。
/// <c>LoadScene</c> では新しいシーンの <c>OnStart</c> の後、<c>PopScene</c> では下のシーンの <c>OnResume</c> の後です (<c>OnStart</c> / <c>OnResume</c> が例外を投げても呼ばれます)。
/// <c>LoadScene</c> が生成の失敗で終わった場合は、<see cref="OnSceneConstructionFailed"/> の直後です。
/// <c>ClearSceneStack</c> とアプリの終了では、各シーンの <c>OnDestroy</c> の直後です。</item>
/// </list>
/// </remarks>
internal interface ISceneLifecycleHook
{
    /// <summary>
    /// シーンを生成する直前に呼ばれます。生成はシーンのコンストラクタ (フィールド初期化子を含む) と、DI による依存の解決を含みます。
    /// </summary>
    /// <remarks>
    /// 遷移が入れ子になると、<see cref="OnSceneConstructed"/> / <see cref="OnSceneConstructionFailed"/> より前にもう一度呼ばれることがあります。
    /// 対応は後入れ先出しです。
    /// </remarks>
    public void OnSceneConstructing();

    /// <summary>
    /// シーンの生成に成功した直後に呼ばれます。直前の <see cref="OnSceneConstructing"/> と対になります。
    /// </summary>
    /// <param name="scene">生成されたシーン。</param>
    public void OnSceneConstructed(Scene scene);

    /// <summary>
    /// シーンの生成が例外で失敗した直後に呼ばれます。直前の <see cref="OnSceneConstructing"/> と対になります。
    /// </summary>
    public void OnSceneConstructionFailed();

    /// <summary>
    /// シーンが使われなくなり、それに結び付いたものを解放してよくなったときに呼ばれます。
    /// </summary>
    /// <param name="scene">使われなくなったシーン。<c>OnDestroy</c> は呼び出し済みです。</param>
    public void OnSceneReleased(Scene scene);
}
