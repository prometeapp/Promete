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

    [Fact]
    public void GetSceneTypes_ShouldNotContainFileLocalScenes()
    {
        var types = SceneRegistry.GetSceneTypes(typeof(SceneRegistryTests).Assembly);

        types
            .Should()
            .NotContain(
                typeof(FileLocalTestScene),
                "file ローカルなシーンは別ファイルの生成コードから参照できないはず"
            );
    }

    [Fact]
    public void RegisteredFactory_ShouldSkipConstructorsWithUnresolvableParameters()
    {
        Load<SceneWithUnresolvableConstructor>()
            .Should()
            .Be(
                "()",
                "MS.DI は引数を解決できないコンストラクタを飛ばすので、生成側もそれに倣うはず"
            );
    }

    [Fact]
    public void RegisteredFactory_ShouldPreferTheLongestResolvableConstructor()
    {
        Load<SceneWithResolvableConstructor>()
            .Should()
            .Be("(app)", "引数が解決できる場合は引数の多いコンストラクタを選ぶはず");
    }

    /// <summary>
    /// シーンを DI 経由で生成し、選ばれたコンストラクタの印を返す。
    /// </summary>
    private static string Load<TScene>()
        where TScene : Scene
    {
        using var app = PrometeApp.Create().UseScenesFrom<TScene>().BuildWithHeadless();

        var picked = string.Empty;
        app.Start += () =>
        {
            // 例外で Exit を取りこぼすと Run が返らないので必ず抜ける。
            try
            {
                app.LoadScene<TScene>();
                picked = PickedConstructor;
            }
            finally
            {
                app.Exit();
            }
        };

        app.Run();

        return picked;
    }

    private static string PickedConstructor { get; set; } = string.Empty;

    /// <summary>
    /// DI に登録されていない型を取るコンストラクタを持つシーン。
    /// </summary>
    internal sealed class SceneWithUnresolvableConstructor : Scene
    {
        public SceneWithUnresolvableConstructor()
        {
            PickedConstructor = "()";
        }

        public SceneWithUnresolvableConstructor(UnregisteredDependency dependency)
        {
            PickedConstructor = "(dependency)";
        }
    }

    /// <summary>
    /// DI に登録されている型を取るコンストラクタを持つシーン。
    /// </summary>
    internal sealed class SceneWithResolvableConstructor : Scene
    {
        public SceneWithResolvableConstructor()
        {
            PickedConstructor = "()";
        }

        public SceneWithResolvableConstructor(PrometeApp app)
        {
            PickedConstructor = "(app)";
        }
    }

    /// <summary>
    /// DI に登録しない依存。
    /// </summary>
    internal sealed class UnregisteredDependency;
}

/// <summary>
/// file ローカルなため登録対象外となるシーン。宣言ファイルの外からは参照できない。
/// </summary>
file sealed class FileLocalTestScene : Scene;
