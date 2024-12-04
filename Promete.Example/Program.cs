using Promete;
using Promete.Coroutines;
using Promete.Example;
using Promete.GLDesktop;
using Promete.ImGui;
using Promete.Input;
using Promete.Windowing;

var app = PrometeApp.Create()
    .Use<Keyboard>()
    .Use<Mouse>()
    .Use<Gamepads>()
    .Use<ConsoleLayer>()
    .Use<CoroutineManager>()
    .Use<ImGuiPlugin>()
    .BuildWithOpenGLDesktop();

return app.Run<MainScene>(WindowOptions.Default with
{
    Title = "Promete Demo",
    Mode = WindowMode.Resizable,
});
