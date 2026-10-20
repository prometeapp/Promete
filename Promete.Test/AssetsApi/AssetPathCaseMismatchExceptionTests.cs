using FluentAssertions;
using Promete.Assets;

namespace Promete.Test.AssetsApi;

/// <summary>
/// 大文字・小文字だけが違うときの例外 (<see cref="AssetPathCaseMismatchException"/>) のテスト。
/// </summary>
public class AssetPathCaseMismatchExceptionTests
{
    [Fact]
    public void Constructor_ShouldExposePaths()
    {
        var ex = new AssetPathCaseMismatchException("assets/player.png", "assets/Player.png");

        ex.RequestedPath.Should().Be("assets/player.png");
        ex.ActualPath.Should().Be("assets/Player.png");
        ex.FileName.Should()
            .Be("assets/player.png", "FileNotFoundException.FileName は要求したパスのはず");
    }

    [Fact]
    public void ShouldBeFileNotFoundException()
    {
        var ex = new AssetPathCaseMismatchException("a.png", "A.png");

        ex.Should()
            .BeAssignableTo<FileNotFoundException>("見つからない例外として catch できるはず");
    }

    [Fact]
    public void Message_ShouldFollowSpecifiedFormat()
    {
        var ex = new AssetPathCaseMismatchException("Player.png", "player.png");

        ex.Message.Should()
            .Be(
                "'Player.png' が見つかりません。'player.png' はあります。アセットのパスは大文字・小文字を区別します。"
            );
    }
}
