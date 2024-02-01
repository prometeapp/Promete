using Promete.Example.Kernel;
using Promete.Input;
using Promete.Windowing;

namespace Promete.Example.examples;


[Demo("/sample3", "ドラッグアンドドロップの例")]
public class Sample3ExampleScene(PrometeApp app, IWindow window, ConsoleLayer console, Keyboard keyboard) : Scene
{
	public override void OnStart()
	{
		window.FileDropped += OnFileDrop;
		console.Print("Drop some files");
		console.Print("Press [ESC] to return");
	}

	public override void OnUpdate()
	{
		if (keyboard.Escape.IsKeyUp)
			app.LoadScene<MainScene>();
	}

	public override void OnDestroy()
	{
		window.FileDropped -= OnFileDrop!;
	}

	private void OnFileDrop(FileDroppedEventArgs e)
	{
		foreach (var path in e.Pathes)
		{
			console.Print($"Dropped file is {path}");
		}
	}
}
