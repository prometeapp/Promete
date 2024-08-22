using Promete.Windowing.VulkanDesktop;

namespace Promete.VulkanDesktop;

public static class VulkanDesktopAppExtension
{
	public static PrometeApp BuildWithVulkanDesktop(this PrometeApp.PrometeAppBuilder builder)
	{
		return builder
			// TODO: レンダラー等を実装し次第登録する
			.Build<VulkanDesktopWindow>();
	}
}
