using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// インスタンスとして登録したプラグインの解決テスト。
/// </summary>
public class PluginInstanceRegistrationTests
{
    [Fact]
    public void Use_WithInstance_ShouldResolveTheSameInstance()
    {
        var instance = new TestPlugin();

        using var app = PrometeApp.Create().Use<ITestPlugin>(instance).BuildWithHeadless();

        app.TryGetPlugin<ITestPlugin>(out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(instance, "インスタンス登録はそのインスタンスを返すはず");
    }

    [Fact]
    public void Use_WithNullInstance_ShouldThrow()
    {
        var act = () => PrometeApp.Create().Use<ITestPlugin>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private interface ITestPlugin;

    private sealed class TestPlugin : ITestPlugin;
}
