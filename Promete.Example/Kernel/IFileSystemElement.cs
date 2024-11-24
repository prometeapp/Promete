namespace Promete.Example.Kernel;

public interface IFileSystemElement
{
    public string Name { get; }
    public Folder? Parent { get; }
}
