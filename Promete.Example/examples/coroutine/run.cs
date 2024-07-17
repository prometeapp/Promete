using System.Collections;
using Promete.Audio;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.coroutine;

[Demo("/coroutine/run.demo", "コルーチンを実行します。")]
public class CoroutineRunExampleScene(Keyboard keyboard, ConsoleLayer console, CoroutineManager coroutine) : Scene
{
	public override void OnStart()
	{
		coroutine.Start(Task());
	}

	public override void OnUpdate()
	{
		if (keyboard.Escape.IsKeyDown)
		{
			App.LoadScene<MainScene>();
		}
	}

	private IEnumerator Task()
	{
		for (var i = 1; i <= 5; i++)
		{
			console.Print(i);
			yield return new WaitForSeconds(1);
		}
		console.Print("Done!");
		yield return new WaitForSeconds(1);
		console.Print("Press [ESC] to return");
	}
}
