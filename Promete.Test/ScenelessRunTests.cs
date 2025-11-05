using FluentAssertions;
using Promete.Headless;
using System.Reflection;

namespace Promete.Test;

public class ScenelessRunTests
{
    /// <summary>
    /// DefaultScene の型を取得するヘルパーメソッド
    /// </summary>
    private static Type GetDefaultSceneType()
    {
        return typeof(PrometeApp).GetNestedType("DefaultScene", BindingFlags.NonPublic)!;
    }

    [Fact]
    public void Run_WithoutScene_ShouldHaveValidRoot()
    {
        // Arrange
        var app = PrometeApp.Create()
            .BuildWithHeadless();

        // OnStartを手動で呼び出してDefaultSceneをロード
        var onStartMethod = typeof(PrometeApp).GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var genericOnStartMethod = onStartMethod.MakeGenericMethod(GetDefaultSceneType());
        genericOnStartMethod.Invoke(app, null);

        // Assert
        app.Root.Should().NotBeNull("Root container should be initialized even without explicit scene");
    }

    [Fact]
    public void GetPlugin_ShouldWork_WhenRunningWithoutScene()
    {
        // Arrange
        var app = PrometeApp.Create()
            .Use<ConsoleLayer>()
            .BuildWithHeadless();

        // Act
        var console = app.GetPlugin<ConsoleLayer>();

        // Assert
        console.Should().NotBeNull("Plugins should be accessible even when running without explicit scene");
    }

    [Fact]
    public void DefaultScene_ShouldBeRegistered()
    {
        // Arrange
        var app = PrometeApp.Create()
            .BuildWithHeadless();

        // Act - DefaultSceneのロードを試みる
        var loadSceneMethod = typeof(PrometeApp).GetMethod("LoadScene", BindingFlags.Public | BindingFlags.Instance, [typeof(Type)])!;
        
        // Assert - 例外がスローされないことを確認
        var act = () => loadSceneMethod.Invoke(app, [GetDefaultSceneType()]);
        act.Should().NotThrow("DefaultScene should be registered and loadable");
    }
}
