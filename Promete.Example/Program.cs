using Promete;
using Promete.GLDesktop;
using Promete.Input;

using Promete.Example;
using Promete.ImGui;

var app = PrometeApp.Create()
	.Use<Keyboard>()
	.Use<Mouse>()
	.Use<ConsoleLayer>()
	.Use<Gamepads>()
	.UseImGui()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
