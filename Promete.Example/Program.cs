using Promete;
using Promete.Coroutines;
using Promete.Example;
using Promete.GLDesktop;
using Promete.ImGui;
using Promete.Input;
using Promete.VulkanDesktop;
using Promete.Windowing;

// --vulkan フラグで実験的な Vulkan バックエンドを使用する
var useVulkan = args.Contains("--vulkan");

var builder = PrometeApp
    .Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<Gamepads>()
    .Use<ConsoleLayer>()
    .Use<CoroutineManager>()
    .Use<ImGuiPlugin>();

var options = WindowOptions.Default with
{
    Title = useVulkan ? "Promete Demo (Vulkan)" : "Promete Demo",
    Mode = WindowMode.Resizable,
    TargetFps = 0,
    TargetUps = 0,
    IsVsyncMode = false,
};

var app = useVulkan
    ? builder.BuildWithVulkanDesktop(options)
    : builder.BuildWithOpenGLDesktop(options);

return app.Run<MainScene>();
