using Promete.Elements;

namespace Promete;

/// <summary>
/// Abstract scene class.
/// </summary>
public abstract class Scene
{

	/// <summary>
	/// Get a root container of this scene.
	/// </summary>
	public Container Root { get; }
	public Scene()
	{
		Root = Setup();
	}

	public virtual Container Setup()
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
