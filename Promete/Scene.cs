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
    /// Called when the scene starts.
    /// </summary>
    public virtual void OnStart()
    {
    }

    /// <summary>
    /// Called when updating frame of the scene.
    /// </summary>
    public virtual void OnUpdate()
    {
    }

    /// <summary>
    /// Called when the scene is disposed.
    /// </summary>
    public virtual void OnDestroy()
    {
    }
}
