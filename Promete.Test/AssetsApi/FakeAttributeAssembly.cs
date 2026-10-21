using System.Reflection;

namespace Promete.Test.AssetsApi;

/// <summary>
/// アセンブリの属性 (と埋め込みリソース) を自由に決められる、テスト用の <see cref="Assembly"/>。
/// MSBuild が書き込む <c>PrometeAssetSourceAttribute</c> を、ビルドせずに試すために使う。
/// </summary>
internal sealed class FakeAttributeAssembly : Assembly
{
    private readonly string _name;
    private readonly Attribute[] _attributes;
    private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);

    public FakeAttributeAssembly(string name, params Attribute[] attributes)
    {
        _name = name;
        _attributes = attributes;
    }

    /// <summary>
    /// 埋め込みリソースを足す。
    /// </summary>
    public FakeAttributeAssembly WithResource(string name, byte[] content)
    {
        _resources[name] = content;
        return this;
    }

    public override AssemblyName GetName() => new(_name);

    public override string? FullName => _name;

    public override object[] GetCustomAttributes(bool inherit) => [.. _attributes];

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        var matched = _attributes.Where(attributeType.IsInstanceOfType).ToArray();
        var result = Array.CreateInstance(attributeType, matched.Length);
        Array.Copy(matched, result, matched.Length);
        return (object[])result;
    }

    public override bool IsDefined(Type attributeType, bool inherit) =>
        _attributes.Any(attributeType.IsInstanceOfType);

    public override string[] GetManifestResourceNames() => [.. _resources.Keys];

    public override Stream? GetManifestResourceStream(string name) =>
        _resources.TryGetValue(name, out var bytes) ? new MemoryStream(bytes, false) : null;
}
