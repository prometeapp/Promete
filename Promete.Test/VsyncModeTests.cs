using FluentAssertions;
using Promete.Headless;
using Promete.Windowing;

namespace Promete.Test;

public class VsyncModeTests
{
    [Fact]
    public void IsVsyncMode_ShouldReflectWindowOptions()
    {
        using var app = PrometeApp
            .Create()
            .BuildWithHeadless(WindowOptions.Default with { IsVsyncMode = true });

        app.Time.IsVsyncMode.Should().BeTrue("起動時の WindowOptions の値が反映されるはず");
    }

    [Fact]
    public void IsVsyncMode_ShouldBeChangeableAtRuntime()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        app.Time.IsVsyncMode = true;

        app.Time.IsVsyncMode.Should().BeTrue();
    }

#pragma warning disable CS0618
    [Fact]
    public void CompatibleWindow_IsVsyncMode_ShouldForwardToTimeProvider()
    {
        using var app = PrometeApp.Create().BuildWithHeadless();

        app.Window.IsVsyncMode = true;

        app.Time.IsVsyncMode.Should().BeTrue("IWindow.IsVsyncMode は Time に転送されるはず");
        app.Window.IsVsyncMode.Should().BeTrue();
    }
#pragma warning restore CS0618
}
