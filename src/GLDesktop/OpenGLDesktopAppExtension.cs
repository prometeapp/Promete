using Promete.Windowing.GLDesktop;

namespace Promete.GLDesktop;

public static class OpenGLDesktopAppExtension
{
	public static PrometeApp BuildWithOpenGLDesktop(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder.Build<PrOpenGLDesktopWindow>();
	}
}
