using Promete.Audio;
using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Graphics.Fonts;
using Promete.Graphics.Fonts.FreeType;
using Promete.Windowing;

namespace Promete.Backends;

/// <summary>
/// Promete バックエンド
/// </summary>
public abstract class BackendBase
{
    /// <summary>
    /// このバックエンドを初期化します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    public abstract void OnInitialize(PrometeApp app, WindowOptions windowOptions);

    /// <summary>
    /// 時間情報を提供する <see cref="ITimeProvider"/> の実装を初期化し、エンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns></returns>
    public abstract ITimeProvider SetupTimeProvider();

    /// <summary>
    /// ゲーム画面にアクセスする <see cref="IGameView" /> の実装を初期化し、エンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns></returns>
    public abstract IGameView SetupGameView();

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public abstract InputProvider SetupInputProvider();

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public abstract IScreenBlitter SetupScreenBlitter();

    /// <summary>
    /// テクスチャの初期化に用いる、 <see cref="TextureFactoryBase"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns></returns>
    public abstract TextureFactoryBase SetupTextureFactory();

    /// <summary>
    /// RenderTexture 機能を提供する <see cref="IRenderTextureProvider"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns></returns>
    public abstract IRenderTextureProvider SetupRenderTextureProvider();

    /// <summary>
    /// シェーダーAPIを提供する <see cref="IShaderFactory"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns></returns>
    public abstract IShaderFactory SetupShaderFactory();

    /// <summary>
    /// <see cref="Font"/> の生成に用いる <see cref="IFontProvider"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns>既定では、FreeType とシステムフォントを用いる実装を返します。</returns>
    public virtual IFontProvider SetupFontProvider() => FreeTypeFontProvider.Shared;

    /// <summary>
    /// <see cref="AudioPlayer"/> が音声の出力に用いる <see cref="IAudioProvider"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    /// <returns>既定では、OpenAL を用いる実装を返します。</returns>
    public virtual IAudioProvider SetupAudioProvider() => OpenALAudioProvider.Shared;

    /// <summary>
    /// ゲームを起動するよう要求された場合の処理を定義します。
    /// </summary>
    /// <remarks>
    /// 実装は、<see cref="PrometeApp.OnStart"/> を 1 度だけ呼び出し、それが完了してから
    /// <see cref="PrometeApp.OnUpdate"/> と <see cref="PrometeApp.OnRender"/> をフレームごとに呼び出す必要があります。
    /// <para>
    /// このメソッドは、ゲームループが終わるまでブロックしても、ゲームループを外部
    /// (ブラウザの <c>requestAnimationFrame</c> など) に委ねてすぐに戻ってもかまいません。
    /// すぐに戻る場合、<see cref="PrometeApp.Run()"/> はゲームの実行中に制御を返します。
    /// </para>
    /// </remarks>
    public abstract void OnStart(PrometeApp app);

    /// <summary>
    /// ゲームを終了するよう要求された場合の処理を定義します。
    /// </summary>
    public abstract void OnExit(PrometeApp app);
}
