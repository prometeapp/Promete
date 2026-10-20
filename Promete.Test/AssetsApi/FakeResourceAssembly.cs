using System.Reflection;
using System.Text;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 埋め込みリソースの名前と中身を自由に決められる、テスト用の <see cref="Assembly"/>。
/// 実際の csproj では作りにくい名前 (<c>\</c> を含むもの、NFD、正規化すると衝突するもの) を試すために使う。
/// </summary>
internal sealed class FakeResourceAssembly : Assembly
{
    private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);

    public FakeResourceAssembly(params (string Name, string Content)[] resources)
    {
        foreach (var (name, content) in resources)
            _resources[name] = Encoding.UTF8.GetBytes(content);
    }

    public override string[] GetManifestResourceNames() => [.. _resources.Keys];

    public override Stream? GetManifestResourceStream(string name) =>
        _resources.TryGetValue(name, out var bytes) ? new MemoryStream(bytes, false) : null;
}
