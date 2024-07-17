using System.Collections;
using Promete.Coroutines;
using Promete.Example.Kernel;
using Promete.Input;

namespace Promete.Example.examples.coroutine;

[Demo("/coroutine/keep-alive.demo", "シーンを切り替えても動き続けるコルーチンを定義します。")]
public class CoroutineKeepAliveDemoScene(Keyboard keyboard, ConsoleLayer console, CoroutineManager coroutine) : Scene
{
	public override void OnStart()
	{
		coroutine.Start(Task())
			.KeepAlive();
		console.Print("Press [ESC] to return");
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
		for (var i = 1; i <= 10; i++)
		{
			Window.Title = $"Count: " + i;
			yield return new WaitForSeconds(1);
		}
	}

	public override void OnDestroy()
	{
	}
}
