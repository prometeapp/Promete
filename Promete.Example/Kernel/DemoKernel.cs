namespace Promete.Example.Kernel;

public static class DemoKernel
{
    public static DemoFileSystem FileSystem { get; } = new();

    public static Folder CurrentFolder { get; set; } = FileSystem.Root;

    public static int CurrentIndex { get; set; }

    public static bool UseVulkan { get; set; }
}
