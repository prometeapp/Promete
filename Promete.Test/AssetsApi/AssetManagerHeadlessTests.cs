using FluentAssertions;
using Promete.Assets;
using Promete.Headless;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 本物のヘッドレスのバックエンド (<c>BuildWithHeadless()</c>) と <see cref="FolderAssetSource"/> で、型付きの読み込みが動くことのテスト。
/// </summary>
/// <remarks>
/// 1 つのテストで <see cref="PrometeApp"/> を 2 つ作ると、後から破棄した方が <c>PrometeApp.Current</c> を参照して失敗するため、フィクスチャを持たないクラスに分けている。
/// </remarks>
public sealed class AssetManagerHeadlessTests
{
    [Fact]
    public void LoadTexture_OnRealHeadlessBackend_FromFolder_ShouldWork()
    {
        using var dir = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(dir.Path, "img"));
        File.WriteAllBytes(Path.Combine(dir.Path, "img", "a.png"), TestAssetData.Png(4, 2));
        using var app = PrometeApp
            .Create()
            .UseAssets(
                typeof(AssetManagerHeadlessTests).Assembly,
                o => o.MountFolder(dir.Path, "assets")
            )
            .BuildWithHeadless();

        var texture = app.GetPlugin<AssetManager>()
            .LoadTexture("assets/img/a.png", AssetScope.Global);

        texture.Size.Should().Be(new VectorInt(4, 2));
    }
}
