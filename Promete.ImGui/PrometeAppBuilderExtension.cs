namespace Promete.ImGui;

public static class PrometeAppBuilderExtension
{
	public static PrometeApp.PrometeAppBuilder UseImGui(this PrometeApp.PrometeAppBuilder builder)
	{
		builder.UseRenderer<ImGuiHost, ImGuiHost.ImGuiHostRenderer>();
		return builder;
	}
}
