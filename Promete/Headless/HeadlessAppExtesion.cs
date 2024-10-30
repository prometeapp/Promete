using Promete.Windowing.Headless;

namespace Promete.Headless;

public static class HeadlessAppExtesion
{
	public static PrometeApp BuildWithHeadless(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder.Build<HeadlessWindow>();
	}
}
