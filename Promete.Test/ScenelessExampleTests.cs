using System.Reflection;
using Promete.Headless;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Test;

/// <summary>
/// Issue で示されたコード例と同様のテスト。
/// シーンを使わずに PrometeApp を実行できることを確認する。
/// </summary>
public class ScenelessExampleTests
{
    [Fact]
    public void Example_ScenelessHelloWorld_ShouldHaveRequiredMethods()
    {
        // Arrange - Issue で示されたコード例と同様のパターン
        using var app = PrometeApp.Create().Use<Keyboard>().Use<ConsoleLayer>().BuildWithHeadless();

        var keyboard = app.GetPlugin<Keyboard>();
        var console = app.GetPlugin<ConsoleLayer>();

        // Window イベントにハンドラを登録できることを確認
        app.Start += () =>
        {
            console.Print("Hello, world!");
        };

        app.Update += () => {
            // Update logic would go here
        };

        // Run() メソッドが存在することを確認
        var runMethods = typeof(PrometeApp)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Run" && m.GetParameters().Length == 0 && !m.IsGenericMethod);
        Assert.Single(runMethods);

        var runMethodsWithOpts = typeof(PrometeApp)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m =>
                m.Name == "Run"
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(WindowOptions)
                && !m.IsGenericMethod
            );
        Assert.Single(runMethodsWithOpts);

        // プラグインが正しく取得できることを確認
        Assert.NotNull(keyboard);
        Assert.NotNull(console);
    }
}
