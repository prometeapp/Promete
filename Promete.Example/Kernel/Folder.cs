namespace Promete.Example.Kernel;

public class Folder(string name, Folder? parent = null) : IFileSystemElement
{
	public string Name { get; } = name;

	public List<IFileSystemElement> Files { get; } = [];

	public Folder? Parent { get; } = parent;

	public int Count => Files.Count;

	public string GetFullPath()
	{
		return Parent == null ? Name : $"{Parent.GetFullPath()}/{Name}";
	}
}
