using Promete;
using Promete.Coroutines;
using Promete.GLDesktop;
using Promete.Input;
using Promete.Example;
using Promete.ImGui;
using Promete.VulkanDesktop;

var builder = PrometeApp.Create()
	.Use<Keyboard>()
	.Use<Mouse>()
	.Use<Gamepads>()
	.Use<ConsoleLayer>()
	.Use<CoroutineManager>()
	.Use<ImGuiPlugin>();

PrometeApp app;
var arg = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
if (arg == "--vulkan")
	app = builder.BuildWithVulkanDesktop();
else
	app = builder.BuildWithOpenGLDesktop();

app.Run<MainScene>();
