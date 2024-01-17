using Promete;
using Promete.GLDesktop;
using Promete.Input;

using Promete.Example;

var app = PrometeApp.Create()
	.Use<Keyboard>()
	.Use<Mouse>()
	.Use<ConsoleLayer>()
	.BuildWithOpenGLDesktop();

app.Run<MainScene>();
