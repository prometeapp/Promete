using Promete.Example.Kernel;

namespace Promete.Example.examples.window;

[Demo("window/time.demo", "時間情報を表示する")]
public class time(ConsoleLayer console) : Scene
{
	public override void OnUpdate()
	{
		console.Clear();
		console.Print($"Time: {Window.TotalTime}");
		console.Print($"DeltaTime: {Window.DeltaTime}");
		console.Print($"FPS: {Window.FramePerSeconds}");
		console.Print($"UPS: {Window.UpdatePerSeconds}");
	}
}
