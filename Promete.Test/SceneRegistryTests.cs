using System.Reflection;
using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

/// <summary>
/// 生成されたシーンレジストリの回帰テスト。
/// </summary>
public class SceneRegistryTests
{
    [Fact]
    public void GetSceneTypes_ShouldContainScenesInThisAssembly()
    {
        var assembly = typeof(SceneRegistryTests).Assembly;

        var types = SceneRegistry.GetSceneTypes(assembly);

        types
            .Should()
            .Contain(typeof(SceneRegistrationTests.ExternalAssemblyScene))
            .And.OnlyContain(t => t.Assembly == assembly);
    }

    [Fact]
    public void GetSceneTypes_ShouldNotContainIgnoredScenes()
    {
        var types = SceneRegistry.GetSceneTypes(typeof(SceneRegistryTests).Assembly);

        types
            .Should()
            .NotContain(
                typeof(IgnoredTestScene),
                "[IgnoredScene] の付いたシーンは登録されないはず"
            );
    }

    [Fact]
    public void GetSceneTypes_ShouldNotContainAbstractScenes()
    {
        var types = SceneRegistry.GetSceneTypes(typeof(SceneRegistryTests).Assembly);

        types.Should().NotContain(typeof(AbstractTestScene), "abstract なシーンは生成できないはず");
    }

    [Fact]
    public void GetSceneTypes_WithNull_ShouldThrow()
    {
        var act = () => SceneRegistry.GetSceneTypes(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteredFactory_ShouldResolveConstructorDependencies()
    {
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<SceneWithDependency>()
            .BuildWithHeadless();

        Exception? thrown = null;
        app.Start += () =>
        {
            try
            {
                app.LoadScene<SceneWithDependency>();
            }
            catch (Exception e)
            {
                thrown = e;
            }

            app.Exit();
        };

        app.Run();

        thrown.Should().BeNull("生成されたファクトリがコンストラクタ注入を解決できるはず");
    }

    /// <summary>
    /// コンストラクタ注入を持つシーン。
    /// </summary>
    internal sealed class SceneWithDependency(PrometeApp app) : Scene
    {
        public PrometeApp InjectedApp { get; } = app;
    }

    /// <summary>
    /// [IgnoredScene] により登録対象外となるシーン。
    /// </summary>
    [IgnoredScene]
    internal sealed class IgnoredTestScene : Scene { }

    /// <summary>
    /// abstract なため登録対象外となるシーン。
    /// </summary>
    internal abstract class AbstractTestScene : Scene { }
}
