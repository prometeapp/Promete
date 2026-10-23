namespace Promete.Test.AssetsBuild.Integration;

/// <summary>
/// MSBuild の統合テストをまとめるコレクション。子プロセスで <c>dotnet build</c> などを実行するので遅く、並列に走らせない。
/// </summary>
/// <remarks>
/// これらのテストには <c>[Trait("Category", "MSBuild")]</c> を付ける。開発中に速いテストだけを回すときは
/// <c>dotnet test Promete.Test/Promete.Test.csproj --filter "Category!=MSBuild"</c>、統合テストだけを回すときは
/// <c>--filter "Category=MSBuild"</c> を使う。
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MSBuildCollection
{
    public const string Name = "MSBuild";

    public const string Category = "MSBuild";
}
