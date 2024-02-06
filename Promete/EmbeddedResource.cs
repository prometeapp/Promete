using System;
using System.IO;
using System.Reflection;

namespace Promete;

/// <summary>
/// 埋め込みリソースを取得するためのヘルパー
/// </summary>
public static class EmbeddedResource
{
	private static readonly Assembly currentAssembly = typeof(EmbeddedResource).Assembly;

	public static string GetResourceAsString(string name)
	{
		using var reader = new StreamReader(GetResourceAsStream(name));
		return reader.ReadToEnd();
	}

	public static byte[] GetResourceAsBytes(string name)
	{
		using var stream = GetResourceAsStream(name);
		using var reader = new BinaryReader(stream);
		return reader.ReadBytes((int)stream.Length);
	}

	public static Stream GetResourceAsStream(string name)
	{
		var stream = currentAssembly.GetManifestResourceStream(name);
		if (stream == null)
			throw new InvalidOperationException($"Resource {name} not found");
		return stream;
	}
}
