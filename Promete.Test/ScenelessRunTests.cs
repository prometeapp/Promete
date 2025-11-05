using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

public class ScenelessRunTests
{
    [Fact]
    public void Run_WithoutScene_ShouldHaveValidRoot()
    {
        // Arrange
        var app = PrometeApp.Create()
            .BuildWithHeadless();

        // DefaultSceneの型を取得する
        var defaultSceneType = typeof(PrometeApp).GetNestedType("DefaultScene", System.Reflection.BindingFlags.NonPublic)!;

        // OnStartを手動で呼び出してDefaultSceneをロード
        var onStartMethod = typeof(PrometeApp).GetMethod("OnStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var genericOnStartMethod = onStartMethod.MakeGenericMethod(defaultSceneType);
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

        // DefaultSceneの型を取得する
        var defaultSceneType = typeof(PrometeApp).GetNestedType("DefaultScene", System.Reflection.BindingFlags.NonPublic)!;

        // Act - DefaultSceneのロードを試みる
        var loadSceneMethod = typeof(PrometeApp).GetMethod("LoadScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, [typeof(Type)])!;
        
        // Assert - 例外がスローされないことを確認
        var act = () => loadSceneMethod.Invoke(app, [defaultSceneType]);
        act.Should().NotThrow("DefaultScene should be registered and loadable");
    }
}
