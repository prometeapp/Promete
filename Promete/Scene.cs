using Promete.Elements;

namespace Promete;

/// <summary>
/// シーンを表す抽象クラスです。
/// </summary>
public abstract class Scene
{
	/// <summary>
	/// このシーンのルートコンテナを取得します。
	/// </summary>
	public Container Root { get; }

	protected Scene()
	{
		Root = Setup();
	}

	protected virtual Container Setup()
	{
		return [];
	}

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
