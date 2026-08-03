using FluentAssertions;
using Promete.Headless;

namespace Promete.Test;

public class SceneRegistrationTests
{
    [Fact]
    public void LoadScene_WithoutUseScenesFrom_ShouldThrow()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        // テストアセンブリはエントリアセンブリではないため、既定では登録されない
        var act = () => app.LoadScene<ExternalAssemblyScene>();

        act.Should().Throw<ArgumentException>("エントリアセンブリ以外のシーンは既定では登録されないはず");
    }

    [Fact]
    public void UseScenesFrom_ShouldRegisterScenesInSpecifiedAssembly()
    {
        using var app = PrometeApp
            .Create()
            .UseScenesFrom(typeof(ExternalAssemblyScene).Assembly)
            .BuildWithHeadless();

        var loaded = false;
        app.Start += () =>
        {
            app.LoadScene<ExternalAssemblyScene>();
            loaded = ExternalAssemblyScene.HasStarted;
            app.Exit();
        };

        ExternalAssemblyScene.HasStarted = false;
        app.Run();

        loaded.Should().BeTrue("UseScenesFrom で指定したアセンブリのシーンは読み込めるはず");
    }

    [Fact]
    public void UseScenesFromGeneric_ShouldRegisterScenesInAssemblyOfType()
    {
        using var app = PrometeApp
            .Create()
            .UseScenesFrom<ExternalAssemblyScene>()
            .BuildWithHeadless();

        Exception? thrown = null;
        app.Start += () =>
        {
            try
            {
                app.LoadScene<ExternalAssemblyScene>();
            }
            catch (Exception e)
            {
                thrown = e;
            }

            app.Exit();
        };

        app.Run();

        thrown.Should().BeNull("型指定のオーバーロードでも同じアセンブリが登録されるはず");
    }

    [Fact]
    public void UseScenesFrom_WithSameAssemblyTwice_ShouldNotThrow()
    {
        var assembly = typeof(ExternalAssemblyScene).Assembly;

        var act = () =>
        {
            using var app = PrometeApp
                .Create()
                .UseScenesFrom(assembly)
                .UseScenesFrom(assembly)
                .BuildWithHeadless();
        };

        act.Should().NotThrow("同じアセンブリを重複して指定しても問題ないはず");
    }

    [Fact]
    public void UseScenesFrom_WithNull_ShouldThrow()
    {
        var act = () => PrometeApp.Create().UseScenesFrom(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// エントリアセンブリの外側に置かれたシーンを模したもの。
    /// </summary>
    private sealed class ExternalAssemblyScene : Scene
    {
        public static bool HasStarted { get; set; }

        public override void OnStart()
        {
            HasStarted = true;
        }
    }
}
