using System;
using System.IO;
using System.Reflection;

namespace Promete;

/// <summary>
/// 埋め込みリソースを取得するためのヘルパー
/// </summary>
public static class EmbeddedResource
{
    private static readonly Assembly CurrentAssembly = typeof(EmbeddedResource).Assembly;

    /// <summary>
    /// 埋め込みリソースを文字列として取得します。
    /// </summary>
    /// <param name="name">リソースの名前</param>
    /// <returns>リソースの内容を表す文字列</returns>
    public static string GetResourceAsString(string name)
    {
        using var reader = new StreamReader(GetResourceAsStream(name));
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 埋め込みリソースをバイト配列として取得します。
    /// </summary>
    /// <param name="name">リソースの名前</param>
    /// <returns>リソースの内容を表すバイト配列</returns>
    public static byte[] GetResourceAsBytes(string name)
    {
        using var stream = GetResourceAsStream(name);
        using var reader = new BinaryReader(stream);
        return reader.ReadBytes((int)stream.Length);
    }

    /// <summary>
    /// 埋め込みリソースをストリームとして取得します。
    /// </summary>
    /// <param name="name">リソースの名前</param>
    /// <returns>リソースの内容を表すストリーム</returns>
    /// <exception cref="InvalidOperationException">リソースが見つからない場合にスローされます</exception>
    public static Stream GetResourceAsStream(string name)
    {
        var stream = CurrentAssembly.GetManifestResourceStream(name);
        if (stream == null)
            throw new InvalidOperationException($"Resource {name} not found");
        return stream;
    }
}
