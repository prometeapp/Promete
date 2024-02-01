namespace Promete.Example.Kernel;

public interface IFileSystemElement
{
	string Name { get; }
	Folder? Parent { get; }
}
