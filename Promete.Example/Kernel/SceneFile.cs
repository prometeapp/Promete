namespace Promete.Example.Kernel;

public class SceneFile(string name, string description, Type scene, Folder? parent = null) : IFileSystemElement
{
	public string Name { get; } = name;
	public string Description { get; } = description;
	public Type Scene { get; } = scene;
	public Folder? Parent { get; } = parent;
}
