using System.Text;
using FluentAssertions;
using Promete.AssetPacker;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// ゲームがパッケージを開いている間に、ビルド (MSBuild 統合の Package モード) がパッケージを作り直せることのテスト。
/// </summary>
/// <remarks>
/// ライターは一時ファイルに書いてから置き換える (<see cref="AssetPackageWriter.WriteTo(string)"/>)。Windows では、開いているファイルを
/// 置き換えるには、開いた側が削除の共有 (<see cref="FileShare.Delete"/>) を許している必要がある。Linux / macOS では常に置き換えられる。
/// </remarks>
public sealed class PackageAssetSourceReplaceTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Writer_ShouldReplacePackageWhileSourceIsOpen()
    {
        var path = Path.Combine(_dir.Path, "Game.main.prmtpak");
        Write(path, "old");
        using var source = new PackageAssetSource(path);

        var act = () => Write(path, "new content");

        act.Should().NotThrow("開いているパッケージを、ビルドで作り直せるはず");
        using var reopened = new PackageAssetSource(path);
        Read(reopened).Should().Be("new content");
    }

    [Fact]
    public void OpenSource_ShouldKeepReadingOldContentAfterReplacement()
    {
        var path = Path.Combine(_dir.Path, "Game.main.prmtpak");
        Write(path, "old");
        using var source = new PackageAssetSource(path);

        Write(path, "new content");

        Read(source).Should().Be("old", "開いたときのファイルを読み続けるはず");
    }

    private static void Write(string path, string content)
    {
        var writer = new AssetPackageWriter();
        writer.Add("a.txt", Encoding.UTF8.GetBytes(content), AssetCompression.Never);
        writer.WriteTo(path);
    }

    private static string Read(IAssetSource source)
    {
        using var reader = new StreamReader(source.OpenRead("a.txt"));
        return reader.ReadToEnd();
    }
}
