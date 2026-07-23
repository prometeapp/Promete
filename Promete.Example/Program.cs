using Promete;
using Promete.Coroutines;
using Promete.Example;
using Promete.VulkanDesktop;
using Promete.ImGui;
using Promete.Input;
using Promete.Windowing;
using Promete.GLDesktop;

var app = PrometeApp
    .Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<Gamepads>()
    .Use<ConsoleLayer>()
    .Use<CoroutineManager>()
    // .Use<ImGuiPlugin>()
    .BuildWithOpenGLDesktop(
        WindowOptions.Default with
        {
            Title = "Promete Demo",
            Mode = WindowMode.Resizable,
            TargetFps = 0,
            TargetUps = 0,
            IsVsyncMode = false,
        }
    );

return app.Run<MainScene>();
