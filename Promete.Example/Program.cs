using Promete;
using Promete.Coroutines;
using Promete.Example;
using Promete.GLDesktop;
using Promete.ImGui;
using Promete.Input;
using Promete.Windowing;

var app = PrometeApp
    .Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<Gamepads>()
    .Use<ConsoleLayer>()
    .Use<CoroutineManager>()
    .Use<ImGuiPlugin>()
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
