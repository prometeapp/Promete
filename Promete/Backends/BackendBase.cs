using Promete.Backends.SilkNetCommon;
using Promete.Graphics;
using Promete.Windowing;
using Silk.NET.Input;

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
    public abstract ITimeProvider SetupTimeProvider();

    /// <summary>
    /// ゲーム画面にアクセスする <see cref="IGameView" /> の実装を初期化し、エンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    public abstract IGameView SetupGameView();

    /// <summary>
    /// </summary>
    public abstract InputProvider SetupInputProvider();

    /// <summary>
    /// テクスチャの初期化に用いる、 <see cref="TextureFactoryBase"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    public abstract TextureFactoryBase SetupTextureFactory();

    /// <summary>
    /// RenderTexture 機能を提供する <see cref="IRenderTextureProvider"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    public abstract IRenderTextureProvider SetupRenderTextureProvider();

    /// <summary>
    /// シェーダーAPIを提供する <see cref="IShaderFactory"/> をエンジンに提供します。
    /// ゲームを初期化する際に1度だけ呼び出されます。
    /// </summary>
    public abstract IShaderFactory SetupShaderFactory();

    /// <summary>
    /// ゲームを起動するよう要求された場合の処理を定義します。
    /// </summary>
    public abstract void OnStart(PrometeApp app);

    /// <summary>
    /// ゲームを終了するよう要求された場合の処理を定義します。
    /// </summary>
    public abstract void OnExit(PrometeApp app);
}
