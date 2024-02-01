using System.Reflection;

namespace Promete.Example.Kernel;

public class DemoFileSystem
{
	public Folder Root { get; } = new Folder("");

	public DemoFileSystem()
	{
		Initialize();
	}

	private void Initialize()
	{
		var scenes = GetType().Assembly.GetTypes()
			.Select(type => (type, attribute: type.GetCustomAttribute<DemoAttribute>()))
			.Where(t => t.attribute is not null) as IEnumerable<(Type, DemoAttribute)>;

		foreach (var (type, attribute) in scenes)
		{
			var path = attribute.Path;
			if (path.IndexOf('/') < 0) path = "/" + path;
			var a = path.LastIndexOf('/');
			var folderPath = path.Remove(a);
			var fileName = path[(a + 1)..];
			var folder = CreateOrGetFolder(folderPath);

			var file = new SceneFile(fileName, attribute.Description, type, folder);

			folder.Files.Add(file);
			folder.Files.Sort((f1, f2) => string.Compare(f1.Name, f2.Name, StringComparison.Ordinal));
		}
	}

	private Folder CreateOrGetFolder(string path)
	{
		path = path.ToLowerInvariant();
		var nest = path.Split('/').Where(s => !string.IsNullOrEmpty(s));
		var current = Root;
		foreach (var name in nest)
		{
			var el = current.Files.FirstOrDefault(f => f.Name == name);
			switch (el)
			{
				case null:
				{
					var folder = new Folder(name, current);
					current.Files.Add(folder);
					current = folder;
					break;
				}
				case Folder f:
					current = f;
					break;
				default:
					// el is other non-null type
					throw new Exception($"'{path}' already exists");
			}
		}
		return current;
	}
}
