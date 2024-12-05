using System;
using Promete.Nodes;
using Promete.Windowing;

namespace Promete;

/// <summary>
/// シーンを表す抽象クラスです。
/// </summary>
public abstract class Scene
{
    /// <summary>
    /// このシーンのルートコンテナを取得します。
    /// </summary>
    public Container Root { get; protected init; } = new Container().Name("Root");

    protected PrometeApp App =>
        PrometeApp.Current ?? throw new InvalidOperationException("PrometeApp is not initialized.");

    protected IWindow Window => App.Window ?? throw new InvalidOperationException("Window is not initialized.");

    /// <summary>
    /// シーンが開始したときに呼び出されます。
    /// </summary>
    public virtual void OnStart()
    {
    }

    /// <summary>
    /// アプリケーションのゲームループ毎に呼び出されます。
    /// </summary>
    public virtual void OnUpdate()
    {
    }

    /// <summary>
    /// シーンが破棄されるときに呼び出されます。
    /// </summary>
    public virtual void OnDestroy()
    {
    }

    /// <summary>
    /// シーンが一時停止したときに呼び出されます。
    /// </summary>
    public virtual void OnPause()
    {
    }

    /// <summary>
    /// シーンが再開したときに呼び出されます。
    /// </summary>
    public virtual void OnResume()
    {
    }
}
