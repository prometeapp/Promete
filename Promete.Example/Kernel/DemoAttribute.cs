namespace Promete.Example.Kernel;

[AttributeUsage(AttributeTargets.Class)]
public class DemoAttribute(string path, string description) : Attribute
{
    public string Path { get; } = path;

    public string Description { get; } = description;
}
