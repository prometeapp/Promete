namespace Promete.Example.Kernel;

public class Folder(string name, Folder? parent = null) : IFileSystemElement
{
    public List<IFileSystemElement> Files { get; } = [];

    public int Count => Files.Count;
    public string Name { get; } = name;

    public Folder? Parent { get; } = parent;

    public string GetFullPath()
    {
        return Parent == null ? Name : $"{Parent.GetFullPath()}/{Name}";
    }
}
