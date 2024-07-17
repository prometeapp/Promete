using Promete;
using Promete.Coroutines;
using Promete.GLDesktop;
using Promete.Input;

using Promete.Example;
using Promete.ImGui;

var app = PrometeApp.Create()
	.Use<Keyboard>()
	.Use<Mouse>()
	.Use<Gamepads>()
	.Use<ConsoleLayer>()
	.Use<CoroutineManager>()
	.Use<ImGuiPlugin>()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
